/* =============================================================
   Serie A Admin - match-events.js
   Maç detayında gol / kart / oyuncu değişikliği ekle, düzenle, sil.
   Veri: Detail.cshtml'in API'den bastığı satırların data-* değerleri.
   Kayıt işlemleri /Admin/Matches/Save* ve Delete* üzerinden API'ye gider.
   ============================================================= */
// (function () { ... })(): dosyadaki değişkenler global olmasın ve diğer script'lerle
// çakışmasın diye her şey hemen çalışan bir fonksiyonun içinde (IIFE).
(function () {
  'use strict';

  const $ = (sel, root = document) => root.querySelector(sel);
  // esc: veriyi innerHTML'e koymadan önce < > & " ' karakterlerini zararsız hale getirir (XSS koruması).
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const icon = (id) => `<svg class="icon" aria-hidden="true"><use href="#i-${id}"/></svg>`;
  const minuteLabel = (m) => (m > 90 ? `90+${m - 90}'` : `${m}'`);

  const summary = $('[data-match-id]');
  if (!summary) return;

  const match = {
    id: Number(summary.dataset.matchId),
    teams: {
      [summary.dataset.homeId]: summary.dataset.homeName,
      [summary.dataset.awayId]: summary.dataset.awayName
    }
  };

  /* Tür başına ayarlar: başlık, API uç noktası, silme metni */
  const KINDS = {
    goal: {
      title: 'Gol', save: '/Admin/Matches/SaveGoal', del: '/Admin/Matches/DeleteGoal',
      playerLabel: 'Gol atan oyuncu',
      describe: (r) => `${r.player} golü (${minuteLabel(r.minute)})`
    },
    card: {
      title: 'Kart', save: '/Admin/Matches/SaveCard', del: '/Admin/Matches/DeleteCard',
      playerLabel: 'Kart gören oyuncu',
      describe: (r) => `${r.player} ${r.cardType === 'Red' ? 'kırmızı' : 'sarı'} kartı (${minuteLabel(r.minute)})`
    },
    sub: {
      title: 'Oyuncu Değişikliği', save: '/Admin/Matches/SaveSubstitution', del: '/Admin/Matches/DeleteSubstitution',
      playerLabel: '',
      describe: (r) => `${r.playerIn} ↔ ${r.playerOut} değişikliği (${minuteLabel(r.minute)})`
    }
  };

  const els = {
    modal: $('#event-modal'),
    form: $('[data-event-form]'),
    title: $('#event-modal-title'),
    alert: $('[data-form-alert]'),
    submit: $('[data-submit]'),
    playerLabel: $('[data-player-label]'),
    del: $('#event-delete-modal'),
    delText: $('[data-delete-text]'),
    toasts: $('[data-toasts]')
  };
  const f = els.form.elements;
  const ALERT_DEFAULT = els.alert.textContent;

  /* Satırdaki data-* değerlerinden kayıt nesnesi */
  // dataset: HTML'deki data-team-id="4" özniteliği JS'te tr.dataset.teamId olarak okunur
  // (tireli ad camelCase'e döner). Değerler her zaman metin gelir, sayı için Number(...) gerekir.
  function readRow(tr) {
    const d = tr.dataset;
    return {
      kind: d.eventRow,
      id: Number(d.id),
      teamId: d.teamId,
      minute: Number(d.minute),
      player: d.player || '',
      cardType: d.cardType || 'Yellow',
      playerIn: d.playerIn || '',
      playerOut: d.playerOut || ''
    };
  }

  /* ---------- API iletişimi (WebUI controller üzerinden) ---------- */
  async function send(url, payload) {
    // fetch: sayfayı yenilemeden sunucuya istek atar; await cevabı bekler.
    // res.ok, durum kodu 200-299 ise true olur; değilse controller'ın gönderdiği message gösterilir.
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
    // sessionStorage: sekme kapanana kadar duran küçük tarayıcı deposu. Sayfa yenilenince
    // bildirimi gösterebilmek için mesajı buraya bırakıp sayfayı yeniliyoruz.
    try { sessionStorage.setItem('eventsToast', message); } catch (_) { /* depolama kapalı */ }
    location.reload();
  }

  /* ---------- Doğrulama ---------- */
  function fieldOf(name) { return els.form.querySelector(`[data-field="${name}"]`); }
  function setError(name, msg) {
    const field = fieldOf(name);
    field.classList.add('has-error');
    field.querySelector('.field-error').textContent = msg;
    f[name].setAttribute('aria-invalid', 'true');
  }
  function clearError(name) {
    const field = fieldOf(name);
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
  ['teamId', 'minute', 'playerName', 'cardType', 'playerIn', 'playerOut'].forEach((n) =>
    f[n].addEventListener('input', () => clearError(n)));

  function validate(kind, d) {
    const errors = [];
    if (!d.teamId) errors.push(['teamId', 'Lütfen takımı seçin.']);
    const min = Number(d.minute);
    if (d.minute === '' || !Number.isInteger(min) || min < 0 || min > 130)
      errors.push(['minute', 'Dakika 0 ile 130 arasında bir tam sayı olmalı.']);
    if (kind === 'sub') {
      if (!d.playerIn.trim()) errors.push(['playerIn', 'Oyuna giren oyuncuyu yazın.']);
      if (!d.playerOut.trim()) errors.push(['playerOut', 'Oyundan çıkan oyuncuyu yazın.']);
      else if (d.playerIn.trim().toLocaleLowerCase('tr-TR') === d.playerOut.trim().toLocaleLowerCase('tr-TR'))
        errors.push(['playerOut', 'Giren ve çıkan oyuncu aynı olamaz.']);
    } else if (!d.playerName.trim()) {
      errors.push(['playerName', 'Oyuncu adını yazın.']);
    }
    return errors;
  }

  /* ---------- Modal ---------- */
  let lastFocus = null;

  // Türüne göre sadece ilgili alanları göster
  function showFieldsFor(kind) {
    els.form.querySelectorAll('[data-kinds]').forEach((field) => {
      field.hidden = !field.dataset.kinds.split(' ').includes(kind);
    });
    if (KINDS[kind].playerLabel) {
      els.playerLabel.innerHTML = `${esc(KINDS[kind].playerLabel)} <span class="req">*</span>`;
    }
  }

  function openModal(kind, record) {
    lastFocus = document.activeElement;
    els.form.reset();
    clearAllErrors();
    els.submit.disabled = false;
    f.kind.value = kind;
    f.id.value = record ? record.id : '';
    showFieldsFor(kind);
    els.title.textContent = `${KINDS[kind].title} ${record ? 'Düzenle' : 'Ekle'}`;

    if (record) {
      f.teamId.value = record.teamId;
      f.minute.value = record.minute;
      f.playerName.value = record.player;
      f.cardType.value = record.cardType;
      f.playerIn.value = record.playerIn;
      f.playerOut.value = record.playerOut;
    }

    els.modal.showModal();
    f.teamId.focus();
  }
  const closeModal = () => els.modal.close();
  els.modal.addEventListener('close', () => { if (lastFocus) lastFocus.focus(); });
  els.modal.addEventListener('click', (e) => { if (e.target === els.modal) closeModal(); });
  document.querySelectorAll('[data-modal-close]').forEach((b) => b.addEventListener('click', closeModal));

  document.querySelectorAll('[data-event-add]').forEach((btn) =>
    btn.addEventListener('click', () => openModal(btn.dataset.eventAdd, null)));

  /* ---------- Kaydet ---------- */
  els.form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearAllErrors();
    const d = Object.fromEntries(new FormData(els.form).entries());
    const kind = d.kind;
    const errors = validate(kind, d);
    if (errors.length) {
      errors.forEach(([n, msg]) => setError(n, msg));
      els.alert.classList.add('is-visible');
      f[errors[0][0]].focus();
      return;
    }

    const payload = {
      id: d.id ? Number(d.id) : 0,
      matchId: match.id,
      teamId: Number(d.teamId),
      minute: Number(d.minute)
    };
    if (kind === 'sub') {
      payload.playerIn = d.playerIn.trim();
      payload.playerOut = d.playerOut.trim();
    } else {
      payload.playerName = d.playerName.trim();
    }
    if (kind === 'card') payload.cardType = d.cardType;

    els.submit.disabled = true;
    try {
      await send(KINDS[kind].save, payload);
      reloadWithToast(`${KINDS[kind].title} ${d.id ? 'güncellendi' : 'eklendi'}.`);
    } catch (err) {
      els.alert.textContent = err.message;
      els.alert.classList.add('is-visible');
      els.submit.disabled = false;
    }
  });

  /* ---------- Düzenle / Sil ---------- */
  let pendingDelete = null;
  document.addEventListener('click', (e) => {
    const edit = e.target.closest('[data-event-edit]');
    const del = e.target.closest('[data-event-delete]');
    const tr = (edit || del)?.closest('[data-event-row]');
    if (!tr) return;

    const record = readRow(tr);
    if (edit) openModal(record.kind, record);
    if (del) {
      pendingDelete = record;
      const team = match.teams[record.teamId] || '';
      els.delText.innerHTML = `<strong>${esc(KINDS[record.kind].describe(record))}</strong>${team ? ' (' + esc(team) + ')' : ''} kalıcı olarak silinecek. Bu işlem geri alınamaz.`;
      els.del.returnValue = '';
      els.del.showModal();
    }
  });
  // <dialog>: tarayıcının hazır modal penceresi. showModal() açar. İçindeki <form method="dialog">
  // butonuna basılınca pencere kapanır ve butonun value'su (confirm/cancel) returnValue'ya yazılır.
  els.del.addEventListener('close', async () => {
    const record = pendingDelete;
    pendingDelete = null;
    if (els.del.returnValue !== 'confirm' || !record) return;
    try {
      await send(`${KINDS[record.kind].del}?id=${record.id}`);
      reloadWithToast(`${KINDS[record.kind].title} silindi.`);
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

  /* Sayfa yenilendikten sonra önceki işlemin bildirimini göster */
  try {
    const pending = sessionStorage.getItem('eventsToast');
    if (pending) {
      sessionStorage.removeItem('eventsToast');
      toast(pending);
    }
  } catch (_) { /* depolama kapalı */ }
})();
