/* =============================================================
   Serie A Admin - matches.js
   Veri: sunucunun (API'den) bastığı satırlardan okunur.
   Filtre + ekle/düzenle/sil; kayıt işlemleri /Admin/Matches/* üzerinden API'ye gider.
   ============================================================= */
(function () {
  'use strict';

  const STATUS = { NotStarted: 'Başlamadı', Live: 'Canlı', Finished: 'Tamamlandı' };
  const STATUS_CODE = { NotStarted: 0, Live: 1, Finished: 2 };  // API MatchStatus

  /* Takımlar: Index.cshtml'in ev sahibi seçimine bastığı option'lardan okunur */
  const TEAMS = {};
  document.querySelectorAll('[data-team-select] option').forEach((o) => {
    if (!o.value) return;
    TEAMS[o.value] = {
      name: o.textContent.trim(),
      stadium: o.dataset.stadium || '',
      logo: o.dataset.logo || '',
      a: o.dataset.colorA || '#1E5BD8',
      b: o.dataset.colorB || '#FFFFFF'
    };
  });
  const team = (id) => TEAMS[id] || { name: 'Bilinmeyen takım', stadium: '', logo: '', a: '#8A97AD', b: '#FFFFFF' };

  /* Maçlar: sunucunun (API'den) bastığı satırların data-* değerlerinden okunur */
  let matches = Array.from(document.querySelectorAll('[data-match-rows] tr[data-match-id]'))
    .map((tr) => {
      const d = tr.dataset;
      return {
        id: Number(d.matchId),
        week: Number(d.week),
        date: d.date,
        time: d.time,
        home: d.home,
        away: d.away,
        homeScore: Number(d.homeScore),
        awayScore: Number(d.awayScore),
        status: d.status || 'NotStarted',
        stadium: d.stadium || ''
      };
    });

  /* ---------- Yardımcılar ---------- */
  const $ = (sel, root = document) => root.querySelector(sel);
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const fmtDate = (iso) => iso.split('-').reverse().join('.');
  const norm = (s) => s.toLocaleLowerCase('tr-TR').normalize('NFD').replace(/[̀-ͯ]/g, '');
  const hasScore = (m) => m.status === 'Live' || m.status === 'Finished';
  const icon = (id) => `<svg class="icon" aria-hidden="true"><use href="#i-${id}"/></svg>`;
  const isUrl = (s) => /^(https?:)?\/\//.test(s) || s.startsWith('/');
  const teamCell = (id) => {
    const t = team(id);
    const crest = isUrl(t.logo)
      ? `<img class="team-crest" src="${esc(t.logo)}" alt="" aria-hidden="true">`
      : `<span class="team-crest" style="--crest-a:${esc(t.a)};--crest-b:${esc(t.b)}" aria-hidden="true"></span>`;
    return `<span class="team">${crest}${esc(t.name)}</span>`;
  };

  const els = {
    rows: $('[data-match-rows]'),
    count: $('[data-result-count]'),
    filter: $('[data-filter-form]'),
    modal: $('#match-modal'),
    form: $('[data-match-form]'),
    title: $('#match-modal-title'),
    alert: $('[data-form-alert]'),
    scoreFields: $('[data-score-fields]'),
    del: $('#delete-modal'),
    delText: $('[data-delete-text]'),
    toasts: $('[data-toasts]'),
    submit: $('[data-submit]')
  };
  let filters = { team: '', week: '', status: '' };
  const ALERT_DEFAULT = els.alert.textContent;

  /* ---------- API iletişimi (WebUI controller üzerinden) ---------- */
  async function sendMatch(url, payload) {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: payload ? JSON.stringify(payload) : undefined
    });
    let data = {};
    try { data = await res.json(); } catch (_) { /* gövde yok */ }
    if (!res.ok) throw new Error(data.message || 'İşlem başarısız oldu.');
    return data;
  }
  function reloadWithToast(message) {
    try { sessionStorage.setItem('matchesToast', message); } catch (_) { /* depolama kapalı */ }
    location.reload();
  }

  /* ---------- Tablo ---------- */
  function applyFilters(list) {
    const q = norm(filters.team.trim());
    return list.filter((m) =>
      (!q || norm(team(m.home).name).includes(q) || norm(team(m.away).name).includes(q)) &&
      (!filters.week || String(m.week) === filters.week) &&
      (!filters.status || m.status === filters.status));
  }

  function renderTable() {
    const list = applyFilters(matches)
      .sort((a, b) => a.week - b.week || (a.date + a.time).localeCompare(b.date + b.time));
    if (!list.length) {
      els.rows.innerHTML = `<tr class="table-empty"><td colspan="9"><strong>Kayıt bulunamadı</strong>Filtreleri değiştirin ya da Temizle ile tüm maçları gösterin.</td></tr>`;
    } else {
      els.rows.innerHTML = list.map((m) => `
        <tr data-id="${m.id}">
          <td class="col-id"><span>${m.id}</span></td>
          <td class="week-cell">${m.week}. Hafta</td>
          <td>${esc(fmtDate(m.date))}</td>
          <td>${esc(m.time)}</td>
          <td>${teamCell(m.home)}</td>
          <td class="col-score">${hasScore(m)
            ? `<span class="score">${m.homeScore} - ${m.awayScore}</span>`
            : '<span class="score score--empty">-</span>'}</td>
          <td>${teamCell(m.away)}</td>
          <td><span class="badge-a badge-a--${m.status}">${STATUS[m.status]}</span></td>
          <td class="col-actions">
            <div class="row-actions">
              <a class="act-btn" href="/Admin/Matches/Detail/${m.id}" title="Gol, kart ve değişiklikler" aria-label="Detay: ${esc(team(m.home).name)} - ${esc(team(m.away).name)}">${icon('eye')}</a>
              <button type="button" class="act-btn" data-edit="${m.id}" title="Düzenle" aria-label="Düzenle: ${esc(team(m.home).name)} - ${esc(team(m.away).name)}">${icon('edit')}</button>
              <button type="button" class="act-btn act-btn--danger" data-delete="${m.id}" title="Sil" aria-label="Sil: ${esc(team(m.home).name)} - ${esc(team(m.away).name)}">${icon('trash')}</button>
            </div>
          </td>
        </tr>`).join('');
    }
    els.count.textContent = `${matches.length} kayıttan ${list.length} tanesi gösteriliyor`;
  }

  els.filter.addEventListener('submit', (e) => {
    e.preventDefault();
    const fd = new FormData(els.filter);
    filters = { team: fd.get('team') || '', week: fd.get('week') || '', status: fd.get('status') || '' };
    renderTable();
  });
  $('[data-filter-reset]').addEventListener('click', () => {
    els.filter.reset();
    filters = { team: '', week: '', status: '' };
    renderTable();
  });

  /* ---------- Form seçeneklerini doldur ---------- */
  const f = els.form.elements;
  f.week.innerHTML = '<option value="">Hafta seçin</option>' +
    Array.from({ length: 38 }, (_, i) => `<option value="${i + 1}">${i + 1}. Hafta</option>`).join('');
  f.away.innerHTML = f.home.innerHTML;  // ev sahibi seçenekleri sunucudan gelir

  let stadiumTouched = false;
  f.stadium.addEventListener('input', () => { stadiumTouched = f.stadium.value.trim() !== ''; });
  f.home.addEventListener('change', () => {
    if (!stadiumTouched && TEAMS[f.home.value]) f.stadium.value = TEAMS[f.home.value].stadium;
    clearError('home');
  });
  f.status.addEventListener('change', toggleScoreFields);
  function toggleScoreFields() {
    const show = f.status.value === 'Live' || f.status.value === 'Finished';
    els.scoreFields.hidden = !show;
    if (!show) { clearError('homeScore'); clearError('awayScore'); }
  }
  ['week', 'away', 'date', 'time', 'homeScore', 'awayScore'].forEach((n) =>
    f[n].addEventListener('input', () => clearError(n)));

  /* ---------- Doğrulama ---------- */
  function setError(name, msg) {
    const field = els.form.querySelector(`[data-field="${name}"]`);
    field.classList.add('has-error');
    field.querySelector('.field-error').textContent = msg;
    f[name].setAttribute('aria-invalid', 'true');
  }
  function clearError(name) {
    const field = els.form.querySelector(`[data-field="${name}"]`);
    if (!field) return;
    field.classList.remove('has-error');
    f[name].removeAttribute('aria-invalid');
  }
  function clearAllErrors() {
    els.form.querySelectorAll('.has-error').forEach((el) => el.classList.remove('has-error'));
    els.form.querySelectorAll('[aria-invalid]').forEach((el) => el.removeAttribute('aria-invalid'));
    els.alert.classList.remove('is-visible');
    els.alert.textContent = ALERT_DEFAULT;
  }

  function validate(data) {
    const errors = [];
    if (!data.week) errors.push(['week', 'Lütfen bir hafta seçin.']);
    if (!data.home) errors.push(['home', 'Lütfen ev sahibi takımı seçin.']);
    if (!data.away) errors.push(['away', 'Lütfen deplasman takımını seçin.']);
    else if (data.home && data.home === data.away) errors.push(['away', 'Ev sahibi ve deplasman takımı aynı olamaz.']);
    if (!data.date) errors.push(['date', 'Lütfen maç tarihini seçin.']);
    if (!data.time) errors.push(['time', 'Lütfen maç saatini seçin.']);
    if (data.status === 'Live' || data.status === 'Finished') {
      ['homeScore', 'awayScore'].forEach((k) => {
        const v = data[k];
        if (v === '' || !Number.isInteger(Number(v)) || Number(v) < 0)
          errors.push([k, 'Geçerli bir skor girin (0 veya üzeri).']);
      });
    }
    return errors;
  }

  /* ---------- Modal aç / kapat ---------- */
  let lastFocus = null;
  function openModal(mode, match) {
    lastFocus = document.activeElement;
    els.form.reset();
    clearAllErrors();
    els.submit.disabled = false;
    stadiumTouched = false;
    f.id.value = '';
    f.status.value = 'NotStarted';
    if (mode === 'edit' && match) {
      els.title.textContent = 'Maçı Düzenle';
      f.id.value = match.id;
      f.week.value = match.week;
      f.home.value = match.home;
      f.away.value = match.away;
      f.date.value = match.date;
      f.time.value = match.time;
      f.stadium.value = match.stadium || '';
      f.status.value = match.status;
      f.homeScore.value = match.status === 'NotStarted' ? '' : match.homeScore;
      f.awayScore.value = match.status === 'NotStarted' ? '' : match.awayScore;
      stadiumTouched = true;
    } else {
      els.title.textContent = 'Yeni Maç Ekle';
    }
    toggleScoreFields();
    els.modal.showModal();
    f.week.focus();
  }
  function closeModal() { els.modal.close(); }
  els.modal.addEventListener('close', () => { if (lastFocus) lastFocus.focus(); });
  els.modal.addEventListener('click', (e) => { if (e.target === els.modal) closeModal(); });
  document.querySelectorAll('[data-modal-close]').forEach((b) => b.addEventListener('click', closeModal));
  $('[data-open-create]').addEventListener('click', () => openModal('create'));

  /* ---------- Kaydet (ekle / güncelle) ---------- */
  els.form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearAllErrors();
    const data = Object.fromEntries(new FormData(els.form).entries());
    const errors = validate(data);
    if (errors.length) {
      errors.forEach(([name, msg]) => setError(name, msg));
      els.alert.classList.add('is-visible');
      f[errors[0][0]].focus();
      return;
    }

    const scored = data.status === 'Live' || data.status === 'Finished';
    const payload = {
      homeTeamId: Number(data.home),
      awayTeamId: Number(data.away),
      week: Number(data.week),
      matchDate: `${data.date}T${data.time}:00`,
      homeScore: scored ? Number(data.homeScore) : 0,
      awayScore: scored ? Number(data.awayScore) : 0,
      status: STATUS_CODE[data.status],
      stadium: data.stadium.trim() || team(data.home).stadium
    };

    els.submit.disabled = true;
    try {
      if (data.id) {
        await sendMatch('/Admin/Matches/Update', { ...payload, id: Number(data.id) });
        reloadWithToast('Maç başarıyla güncellendi.');
      } else {
        await sendMatch('/Admin/Matches/Create', payload);
        reloadWithToast('Maç başarıyla eklendi.');
      }
    } catch (err) {
      els.alert.textContent = err.message;
      els.alert.classList.add('is-visible');
      els.submit.disabled = false;
    }
  });

  /* ---------- Düzenle / Sil ---------- */
  let pendingDelete = null;
  els.rows.addEventListener('click', (e) => {
    const edit = e.target.closest('[data-edit]');
    const del = e.target.closest('[data-delete]');
    if (edit) openModal('edit', matches.find((m) => m.id === Number(edit.dataset.edit)));
    if (del) {
      const m = matches.find((x) => x.id === Number(del.dataset.delete));
      pendingDelete = m.id;
      els.delText.innerHTML = `<strong>${esc(team(m.home).name)} - ${esc(team(m.away).name)}</strong> (ID ${m.id}, ${m.week}. Hafta) kalıcı olarak silinecek. Bu işlem geri alınamaz.`;
      els.del.returnValue = '';
      els.del.showModal();
    }
  });
  els.del.addEventListener('close', async () => {
    const id = pendingDelete;
    pendingDelete = null;
    if (els.del.returnValue !== 'confirm' || id === null) return;
    try {
      await sendMatch('/Admin/Matches/Delete?id=' + id);
      reloadWithToast('Maç silindi.');
    } catch (err) {
      toast(err.message, 'info');
    }
  });

  /* ---------- Toast ---------- */
  function toast(message, type = 'success') {
    if (!els.toasts) return;
    const el = document.createElement('div');
    el.className = `toast-a toast-a--${type}`;
    el.setAttribute('role', 'status');
    el.innerHTML = `${icon(type === 'success' ? 'check' : 'info')}<span>${esc(message)}</span>`;
    els.toasts.appendChild(el);
    setTimeout(() => el.remove(), 3500);
  }

  /* Haftalar sayfasından ?week=N ile gelindiyse o haftayı filtrele */
  const weekParam = new URLSearchParams(location.search).get('week');
  if (weekParam && els.filter.elements.week.querySelector(`option[value="${CSS.escape(weekParam)}"]`)) {
    els.filter.elements.week.value = weekParam;
    filters.week = weekParam;
  }

  renderTable();

  /* Sayfa yenilendikten sonra önceki işlemin bildirimini göster */
  try {
    const pending = sessionStorage.getItem('matchesToast');
    if (pending) {
      sessionStorage.removeItem('matchesToast');
      toast(pending);
    }
  } catch (_) { /* depolama kapalı */ }
})();
