/* Maç Detay - match-detail.js
   Olay filtresi + maç akışı şeridi <-> zaman çizelgesi eşlemesi. */
(function () {
    'use strict';

    const root = document.querySelector('[data-match]');
    if (!root) return;

    const chips = Array.from(root.querySelectorAll('[data-event-filter]'));
    const events = Array.from(root.querySelectorAll('.event[data-type]'));
    const markers = Array.from(root.querySelectorAll('[data-flow-ref]'));
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function setFilter(type) {
        chips.forEach((c) => c.setAttribute('aria-pressed', String(c.dataset.eventFilter === type)));
        events.forEach((ev) => { ev.hidden = type !== 'all' && ev.dataset.type !== type; });
    }
    chips.forEach((chip) => chip.addEventListener('click', () => setFilter(chip.dataset.eventFilter)));

    markers.forEach((marker) => {
        const target = document.getElementById(marker.dataset.flowRef);
        if (!target) return;

        marker.addEventListener('click', () => {
            if (target.hidden) setFilter('all');
            target.scrollIntoView({ block: 'center', behavior: reduceMotion ? 'auto' : 'smooth' });
            target.classList.remove('is-flash');
            void target.offsetWidth;
            target.classList.add('is-flash');
            setTimeout(() => target.classList.remove('is-flash'), 1600);
        });

        const on = () => { target.classList.add('is-hover'); marker.classList.add('is-hover'); };
        const off = () => { target.classList.remove('is-hover'); marker.classList.remove('is-hover'); };
        marker.addEventListener('mouseenter', on);
        marker.addEventListener('mouseleave', off);
        target.addEventListener('mouseenter', on);
        target.addEventListener('mouseleave', off);
    });
})();
