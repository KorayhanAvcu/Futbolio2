/* Serie A Puan Durumu - standings.js
   Bölge filtresi, takım arama, satır seçimi ve puan dağılımı <-> tablo eşlemesi. */
(function () {
    'use strict';

    const root = document.querySelector('[data-standings]');
    if (!root) return;

    const rows = Array.from(root.querySelectorAll('.standings-row'));
    const markers = Array.from(root.querySelectorAll('[data-ladder-team]'));
    const chips = Array.from(root.querySelectorAll('[data-zone-filter]'));
    const search = root.querySelector('[data-team-search]');
    const counter = root.querySelector('[data-visible-count]');
    const emptyRow = root.querySelector('[data-empty-row]');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    let activeZone = 'all';
    let selectedTeam = null;

    const normalize = (value) =>
        value.toLocaleLowerCase('tr-TR').normalize('NFD').replace(/[\u0300-\u036f]/g, '');

    /* Filtreleme */
    function applyFilters() {
        const query = normalize(search ? search.value.trim() : '');
        let visible = 0;

        rows.forEach((row) => {
            const zoneMatch = activeZone === 'all' || row.dataset.zone === activeZone;
            const textMatch = !query || normalize(row.dataset.teamName).includes(query);
            row.hidden = !(zoneMatch && textMatch);
            if (!row.hidden) visible += 1;
        });

        if (counter) counter.textContent = String(visible);
        if (emptyRow) emptyRow.hidden = visible !== 0;
    }

    function setZone(zone) {
        activeZone = zone;
        chips.forEach((chip) =>
            chip.setAttribute('aria-pressed', String(chip.dataset.zoneFilter === zone)));
        applyFilters();
    }

    function resetFilters() {
        if (search) search.value = '';
        setZone('all');
    }

    chips.forEach((chip) => chip.addEventListener('click', () => setZone(chip.dataset.zoneFilter)));
    if (search) search.addEventListener('input', applyFilters);

    /* Seçim */
    function selectTeam(team, { scrollToRow = false } = {}) {
        selectedTeam = selectedTeam === team ? null : team;

        rows.forEach((row) => row.classList.toggle('is-selected', row.dataset.team === selectedTeam));
        markers.forEach((marker) => {
            const on = marker.dataset.ladderTeam === selectedTeam;
            marker.classList.toggle('is-active', on);
            marker.setAttribute('aria-pressed', String(on));
        });

        if (!scrollToRow || !selectedTeam) return;
        const row = rows.find((r) => r.dataset.team === selectedTeam);
        if (!row) return;
        if (row.hidden) resetFilters();
        row.scrollIntoView({ block: 'center', behavior: reduceMotion ? 'auto' : 'smooth' });
    }

    rows.forEach((row) => {
        row.addEventListener('click', () => selectTeam(row.dataset.team));
        row.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                selectTeam(row.dataset.team);
            }
        });
    });

    markers.forEach((marker) =>
        marker.addEventListener('click', () => selectTeam(marker.dataset.ladderTeam, { scrollToRow: true })));

    /* Hover eşleme: tablo satırı <-> puan dağılımı */
    function linkHover(team, on) {
        rows.forEach((r) => { if (r.dataset.team === team) r.classList.toggle('is-hover', on); });
        markers.forEach((m) => { if (m.dataset.ladderTeam === team) m.classList.toggle('is-hover', on); });
    }
    rows.forEach((row) => {
        row.addEventListener('mouseenter', () => linkHover(row.dataset.team, true));
        row.addEventListener('mouseleave', () => linkHover(row.dataset.team, false));
    });
    markers.forEach((marker) => {
        marker.addEventListener('mouseenter', () => linkHover(marker.dataset.ladderTeam, true));
        marker.addEventListener('mouseleave', () => linkHover(marker.dataset.ladderTeam, false));
    });

    applyFilters();
})();
