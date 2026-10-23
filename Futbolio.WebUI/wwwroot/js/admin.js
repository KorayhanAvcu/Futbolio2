/* =============================================================
   Serie A Admin - admin.js
   Tüm yönetim sayfalarında ortak: mobilde sidebar aç/kapat.
   ============================================================= */
(function () {
  'use strict';

  const toggleBtn = document.querySelector('[data-sidebar-toggle]');
  const backdrop = document.querySelector('[data-sidebar-close]');
  if (!toggleBtn) return;

  function setSidebar(open) {
    document.body.classList.toggle('sidebar-open', open);
    toggleBtn.setAttribute('aria-expanded', String(open));
  }

  toggleBtn.addEventListener('click', () => setSidebar(!document.body.classList.contains('sidebar-open')));
  if (backdrop) backdrop.addEventListener('click', () => setSidebar(false));
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && document.body.classList.contains('sidebar-open')) setSidebar(false);
  });
})();
