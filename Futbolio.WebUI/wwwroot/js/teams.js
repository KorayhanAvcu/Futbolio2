/* =============================================================
   Serie A Admin - teams.js
   Veri: sunucunun (API'den) bastığı tablo satırlarından okunur.
   Filtre + sıralama + ekle/düzenle/sil arayüzü.
   Ekle/düzenle/sil şimdilik sadece tarayıcıda çalışır (API'ye
   gitmez); API uç noktaları yorumlarda belirtilmiştir.
   ============================================================= */
(function () {
    'use strict';

    const MAX_ACTIVE = 20;
    const THIS_YEAR = new Date().getFullYear();
    const REGION = { north: 'Kuzey İtalya', center: 'Orta İtalya', south: 'Güney İtalya', island: 'Adalar' };

    /* Başlangıç verisi: Index.cshtml'in bastığı satırların data-* değerlerinden okunur */
    let teams = Array.from(document.querySelectorAll('[data-team-rows] tr[data-team-id]'))
        .map((tr) => {
            const d = tr.dataset;
            return {
                id: Number(d.id),
                name: d.name || '',
                code: d.code || '',
                city: d.city || '',
                region: d.region || '',
                stadium: d.stadium || '',
                capacity: Number(d.capacity) || 0,
                founded: Number(d.founded) || 0,
                colorA: d.colorA || '#1E5BD8',
                colorB: d.colorB || '#FFFFFF',
                status: d.status || 'inactive'
            };
        });

    /* ---------- Yardımcılar ---------- */
    const $ = (sel, root = document) => root.querySelector(sel);
    const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const norm = (s) => String(s).toLocaleLowerCase('tr-TR').normalize('NFD').replace(/[̀-ͯ]/g, '');
    const num = (n) => new Intl.NumberFormat('tr-TR').format(n);
    const icon = (id) => `<svg class="icon" aria-hidden="true"><use href="#i-${id}"/></svg>`;
    const activeCount = () => teams.filter((t) => t.status === 'active').length;

    /* Arma yazısı: ana rengin açıklığına göre koyu/açık */
    function inkFor(hex) {
        const h = hex.replace('#', '');
        const [r, g, b] = [0, 2, 4].map((i) => parseInt(h.slice(i, i + 2), 16));
        return (0.299 * r + 0.587 * g + 0.114 * b) > 170 ? '#10224A' : '#FFFFFF';
    }
    const crestStyle = (a, b) => `--crest-a:${a};--crest-b:${b};--crest-ink:${inkFor(a)}`;

    const els = {
        rows: $('[data-team-rows]'),
        count: $('[data-result-count]'),
        strip: $('[data-league-strip]'),
        filter: $('[data-filter-form]'),
        modal: $('#team-modal'),
        form: $('[data-team-form]'),
        title: $('#team-modal-title'),
        alert: $('[data-form-alert]'),
        del: $('#delete-modal'),
        delText: $('[data-delete-text]'),
        toasts: $('[data-toasts]')
    };
    let filters = { q: '', region: '', status: '' };
    let sort = { key: 'id', dir: 1 };
    const ALERT_DEFAULT = els.alert.textContent;

    /* ---------- API iletişimi (WebUI controller üzerinden) ---------- */
    async function sendTeam(url, payload) {
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
        try { sessionStorage.setItem('teamsToast', message); } catch (_) { /* depolama kapalı */ }
        location.reload();
    }

    /* ---------- Lig şeridi ---------- */
    function renderStrip() {
        const active = teams.filter((t) => t.status === 'active');
        const stadiums = new Map(active.map((t) => [t.stadium, t.capacity]));  // ortak stadyumlar bir kez sayılır
        const capacity = Array.from(stadiums.values()).reduce((s, c) => s + c, 0);
        const cities = new Set(active.map((t) => t.city)).size;
        const oldest = active.filter((t) => t.founded > 0).sort((a, b) => a.founded - b.founded)[0];
        els.strip.innerHTML = `
      <div class="strip-item strip-item--green"><span class="strip-icon">${icon('shield')}</span><div><span class="strip-label">Aktif takım</span><span class="strip-value">${active.length} <small>/ ${MAX_ACTIVE}</small></span></div></div>
      <div class="strip-item strip-item--blue"><span class="strip-icon">${icon('pin')}</span><div><span class="strip-label">Temsil edilen şehir</span><span class="strip-value">${cities}</span></div></div>
      <div class="strip-item strip-item--navy"><span class="strip-icon">${icon('stadium')}</span><div><span class="strip-label">Toplam stadyum kapasitesi</span><span class="strip-value">${num(capacity)}</span></div></div>
      <div class="strip-item strip-item--red"><span class="strip-icon">${icon('clock')}</span><div><span class="strip-label">En köklü kulüp</span><span class="strip-value">${oldest ? `${esc(oldest.name)} <small>${oldest.founded}</small>` : '-'}</span></div></div>`;
    }

    /* ---------- Tablo ---------- */
    function visibleTeams() {
        const q = norm(filters.q.trim());
        return teams
            .filter((t) =>
                (!q || [t.name, t.city, t.stadium, t.code].some((v) => norm(v).includes(q))) &&
                (!filters.region || t.region === filters.region) &&
                (!filters.status || t.status === filters.status))
            .sort((a, b) => {
                const x = a[sort.key], y = b[sort.key];
                return (typeof x === 'string' ? x.localeCompare(y, 'tr') : x - y) * sort.dir;
            });
    }

    function renderTable(highlightId) {
        const list = visibleTeams();
        if (!list.length) {
            const msg = teams.length
                ? '<strong>Takım bulunamadı</strong>Filtreleri değiştirin ya da Temizle ile tüm takımları gösterin.'
                : '<strong>Henüz takım bulunmuyor</strong>Yeni Takım Ekle ile ilk takımı oluşturabilirsiniz.';
            els.rows.innerHTML = `<tr class="table-empty"><td colspan="9">${msg}</td></tr>`;
        } else {
            els.rows.innerHTML = list.map((t) => `
        <tr data-team-id="${t.id}" data-id="${t.id}" class="${t.status === 'inactive' ? 'is-inactive' : ''}${t.id === highlightId ? ' is-new' : ''}"
            data-name="${esc(t.name)}" data-code="${esc(t.code)}" data-city="${esc(t.city)}" data-region="${esc(t.region)}"
            data-stadium="${esc(t.stadium)}" data-capacity="${t.capacity}" data-founded="${t.founded}"
            data-color-a="${t.colorA}" data-color-b="${t.colorB}" data-status="${t.status}">
          <td class="col-id">${t.id}</td>
          <td>
            <div class="team-cell">
              <span class="team-crest" style="${crestStyle(t.colorA, t.colorB)}" aria-hidden="true">${esc(t.code)}</span>
              <div><span class="team-name">${esc(t.name)}</span><span class="team-code">${esc(t.code)}</span></div>
            </div>
          </td>
          <td class="city-cell">${esc(t.city)}<small><span class="region region--${esc(t.region)}">${REGION[t.region] || '—'}</span></small></td>
          <td>${t.stadium ? esc(t.stadium) : '—'}</td>
          <td class="col-num">${t.capacity > 0 ? num(t.capacity) : '—'}</td>
          <td class="col-num">${t.founded > 0 ? t.founded : '—'}</td>
          <td><span class="swatches" title="${t.colorA} / ${t.colorB}"><span class="swatch" style="--c:${t.colorA}"></span><span class="swatch" style="--c:${t.colorB}"></span></span></td>
          <td><span class="badge-a badge-a--${t.status}">${t.status === 'active' ? 'Aktif' : 'Pasif'}</span></td>
          <td class="col-actions">
            <div class="row-actions">
              <button type="button" class="act-btn" data-edit="${t.id}" title="Düzenle" aria-label="Düzenle: ${esc(t.name)}">${icon('edit')}</button>
              <button type="button" class="act-btn act-btn--danger" data-delete="${t.id}" title="Sil" aria-label="Sil: ${esc(t.name)}">${icon('trash')}</button>
            </div>
          </td>
        </tr>`).join('');
        }
        els.count.textContent = `${teams.length} takımdan ${list.length} tanesi gösteriliyor`;
        document.querySelectorAll('[data-sort-col]').forEach((th) =>
            th.setAttribute('aria-sort', th.dataset.sortCol === sort.key ? (sort.dir === 1 ? 'ascending' : 'descending') : 'none'));
    }
    function renderAll(highlightId) { renderStrip(); renderTable(highlightId); }

    /* Filtre + sıralama */
    els.filter.addEventListener('submit', (e) => {
        e.preventDefault();
        const fd = new FormData(els.filter);
        filters = { q: fd.get('q') || '', region: fd.get('region') || '', status: fd.get('status') || '' };
        renderTable();
    });
    $('[data-filter-reset]').addEventListener('click', () => {
        els.filter.reset();
        filters = { q: '', region: '', status: '' };
        renderTable();
    });
    document.querySelectorAll('[data-sort]').forEach((btn) => btn.addEventListener('click', () => {
        const key = btn.dataset.sort;
        sort = sort.key === key ? { key, dir: -sort.dir } : { key, dir: 1 };
        renderTable();
    }));

    /* ---------- Form ---------- */
    const f = els.form.elements;
    const preview = {
        crest: $('[data-preview-crest]'),
        name: $('[data-preview-name]'),
        meta: $('[data-preview-meta]'),
        colorA: $('[data-color-a]'),
        colorB: $('[data-color-b]'),
        statusHint: $('[data-status-hint]')
    };

    function updatePreview() {
        const a = f.colorA.value.toUpperCase(), b = f.colorB.value.toUpperCase();
        preview.crest.setAttribute('style', crestStyle(a, b));
        preview.crest.textContent = (f.code.value || '---').toUpperCase();
        preview.name.textContent = f.name.value.trim() || 'Takım adı';
        const meta = [f.city.value.trim(), f.stadium.value.trim()].filter(Boolean).join(', ');
        preview.meta.textContent = meta || 'Şehir, stadyum';
        preview.colorA.textContent = a;
        preview.colorB.textContent = b;
    }
    function updateStatusHint() {
        const editingActive = f.id.value && teams.find((t) => t.id === Number(f.id.value))?.status === 'active';
        const used = activeCount() - (editingActive ? 1 : 0);
        preview.statusHint.textContent = `Ligde şu anda ${activeCount()} aktif takım var (sınır ${MAX_ACTIVE}).` +
            (used >= MAX_ACTIVE ? ' Yeni aktif takım için önce bir takımı pasif yapın.' : '');
    }

    f.code.addEventListener('input', () => { f.code.value = f.code.value.toLocaleUpperCase('tr-TR').replace(/[^A-ZÇĞİÖŞÜ]/g, ''); });
    ['name', 'code', 'city', 'stadium', 'colorA', 'colorB'].forEach((n) => f[n].addEventListener('input', updatePreview));
    ['name', 'code', 'city', 'region', 'stadium', 'capacity', 'founded', 'status'].forEach((n) =>
        f[n].addEventListener('input', () => clearError(n)));
    ['colorA', 'colorB'].forEach((n) => f[n].addEventListener('input', () => clearError('colors')));

    function setError(name, msg) {
        const field = els.form.querySelector(`[data-field="${name}"]`);
        field.classList.add('has-error');
        field.querySelector('.field-error').textContent = msg;
        const input = f[name] || field.querySelector('input');
        if (input && input.setAttribute) input.setAttribute('aria-invalid', 'true');
    }
    function clearError(name) {
        const field = els.form.querySelector(`[data-field="${name}"]`);
        if (!field) return;
        field.classList.remove('has-error');
        field.querySelectorAll('[aria-invalid]').forEach((el) => el.removeAttribute('aria-invalid'));
    }
    function clearAllErrors() {
        els.form.querySelectorAll('.has-error').forEach((el) => el.classList.remove('has-error'));
        els.form.querySelectorAll('[aria-invalid]').forEach((el) => el.removeAttribute('aria-invalid'));
        els.alert.classList.remove('is-visible');
        els.alert.textContent = ALERT_DEFAULT;
    }

    function validate(d) {
        const errors = [];
        const selfId = Number(d.id) || null;
        const others = teams.filter((t) => t.id !== selfId);
        const name = d.name.trim();
        const code = d.code.trim().toUpperCase();

        if (!name) errors.push(['name', 'Lütfen takım adını girin.']);
        else if (name.length < 2) errors.push(['name', 'Takım adı en az 2 karakter olmalı.']);
        else if (others.some((t) => norm(t.name) === norm(name))) errors.push(['name', 'Bu isimde bir takım zaten kayıtlı.']);

        if (!code) errors.push(['code', 'Lütfen 3 harfli kısa adı girin.']);
        else if (code.length !== 3) errors.push(['code', 'Kısa ad tam olarak 3 harften oluşmalı.']);
        else if (others.some((t) => t.code === code)) errors.push(['code', 'Bu kısa ad başka bir takım tarafından kullanılıyor.']);

        if (!d.city.trim()) errors.push(['city', 'Lütfen şehir girin.']);
        if (!d.region) errors.push(['region', 'Lütfen bölge seçin.']);
        if (!d.stadium.trim()) errors.push(['stadium', 'Lütfen stadyum adını girin.']);

        const cap = Number(d.capacity);
        if (d.capacity === '') errors.push(['capacity', 'Lütfen stadyum kapasitesini girin.']);
        else if (!Number.isInteger(cap) || cap < 1000 || cap > 100000) errors.push(['capacity', 'Kapasite 1.000 ile 100.000 arasında olmalı.']);

        const year = Number(d.founded);
        if (d.founded === '') errors.push(['founded', 'Lütfen kuruluş yılını girin.']);
        else if (!Number.isInteger(year) || year < 1850 || year > THIS_YEAR) errors.push(['founded', `Kuruluş yılı 1850 ile ${THIS_YEAR} arasında olmalı.`]);

        if (d.colorA.toUpperCase() === d.colorB.toUpperCase()) errors.push(['colors', 'Ana renk ve ikinci renk aynı olamaz.']);

        if (d.status === 'active') {
            const wasActive = selfId && teams.find((t) => t.id === selfId)?.status === 'active';
            if (!wasActive && activeCount() >= MAX_ACTIVE)
                errors.push(['status', `Serie A'da en fazla ${MAX_ACTIVE} aktif takım olabilir. Önce başka bir takımı pasif yapın.`]);
        }
        return errors;
    }

    /* ---------- Modal aç / kapat ---------- */
    let lastFocus = null;
    function openModal(team) {
        lastFocus = document.activeElement;
        els.form.reset();
        clearAllErrors();
        f.id.value = '';
        if (team) {
            els.title.textContent = 'Takımı Düzenle';
            ['id', 'name', 'code', 'city', 'region', 'stadium', 'capacity', 'founded', 'status'].forEach((k) => { f[k].value = team[k]; });
            f.colorA.value = team.colorA;
            f.colorB.value = team.colorB;
        } else {
            els.title.textContent = 'Yeni Takım Ekle';
            f.status.value = activeCount() >= MAX_ACTIVE ? 'inactive' : 'active';
        }
        updatePreview();
        updateStatusHint();
        els.modal.showModal();
        f.name.focus();
    }
    const closeModal = () => els.modal.close();
    els.modal.addEventListener('close', () => { if (lastFocus) lastFocus.focus(); });
    els.modal.addEventListener('click', (e) => { if (e.target === els.modal) closeModal(); });
    document.querySelectorAll('[data-modal-close]').forEach((b) => b.addEventListener('click', closeModal));
    $('[data-open-create]').addEventListener('click', () => openModal(null));

    /* ---------- Kaydet ---------- */
    els.form.addEventListener('submit', async (e) => {
        e.preventDefault();
        clearAllErrors();
        const d = Object.fromEntries(new FormData(els.form).entries());
        const errors = validate(d);
        if (errors.length) {
            errors.forEach(([n, msg]) => setError(n, msg));
            els.alert.classList.add('is-visible');
            const first = f[errors[0][0]] || f.colorA;
            first.focus();
            return;
        }
        const record = {
            name: d.name.trim(),
            code: d.code.trim().toUpperCase(),
            city: d.city.trim(),
            region: d.region,
            stadium: d.stadium.trim(),
            capacity: Number(d.capacity),
            founded: Number(d.founded),
            colorA: d.colorA.toUpperCase(),
            colorB: d.colorB.toUpperCase(),
            status: d.status
        };
        const payload = {
            name: record.name,
            logoUrl: 'none',
            code: record.code,
            city: record.city,
            region: record.region,
            stadium: record.stadium,
            capacity: record.capacity,
            founded: record.founded,
            colorA: record.colorA,
            colorB: record.colorB,
            isActive: record.status === 'active'
        };

        const submitBtn = els.form.querySelector('[type="submit"]');
        submitBtn.disabled = true;
        try {
            if (d.id) {
                await sendTeam('/Admin/Teams/Update', { ...payload, id: Number(d.id) });
                reloadWithToast('Takım başarıyla güncellendi.');
            } else {
                await sendTeam('/Admin/Teams/Create', payload);
                reloadWithToast('Takım başarıyla eklendi.');
            }
        } catch (err) {
            els.alert.textContent = err.message;
            els.alert.classList.add('is-visible');
            submitBtn.disabled = false;
        }
    });

    /* ---------- Düzenle / Sil ---------- */
    let pendingDelete = null;
    els.rows.addEventListener('click', (e) => {
        const edit = e.target.closest('[data-edit], [data-edit-team]');
        const del = e.target.closest('[data-delete], [data-delete-team]');
        if (edit) {
            const id = Number(edit.dataset.edit || edit.dataset.editTeam);
            openModal(teams.find((t) => t.id === id));
        }
        if (del) {
            const id = Number(del.dataset.delete || del.dataset.deleteTeam);
            const t = teams.find((x) => x.id === id);
            pendingDelete = t.id;
            const warn = t.status === 'active'
                ? ' Takım şu anda Serie A\'da aktif; silinirse fikstür ve puan durumundaki kayıtları da etkilenir.'
                : '';
            els.delText.innerHTML = `<strong>${esc(t.name)}</strong> (${esc(t.code)}) kalıcı olarak silinecek.${warn} Bu işlem geri alınamaz.`;
            els.del.returnValue = '';
            els.del.showModal();
        }
    });
    els.del.addEventListener('close', async () => {
        const id = pendingDelete;
        pendingDelete = null;
        if (els.del.returnValue !== 'confirm' || id === null) return;
        try {
            await sendTeam('/Admin/Teams/Delete?id=' + id);
            reloadWithToast('Takım silindi.');
        } catch (err) {
            toast(err.message, 'info');
        }
    });

    /* ---------- Toast ---------- */
    function toast(message, type = 'success') {
        if (!els.toasts) return;  // sayfada [data-toasts] kabı yoksa sessizce geç
        const el = document.createElement('div');
        el.className = `toast-a toast-a--${type}`;
        el.setAttribute('role', 'status');
        el.innerHTML = `${icon(type === 'success' ? 'check' : 'info')}<span>${esc(message)}</span>`;
        els.toasts.appendChild(el);
        setTimeout(() => el.remove(), 3500);
    }

    renderAll();

    /* Sayfa yenilendikten sonra önceki işlemin bildirimini göster */
    try {
        const pending = sessionStorage.getItem('teamsToast');
        if (pending) {
            sessionStorage.removeItem('teamsToast');
            toast(pending);
        }
    } catch (_) { /* depolama kapalı */ }
})();