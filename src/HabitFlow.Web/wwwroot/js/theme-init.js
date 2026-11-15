(() => {
  let theme = 'light';
  try { theme = localStorage.getItem('habitflow-theme') || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'); } catch {}
  theme = theme === 'dark' ? 'dark' : 'light';

  const applyTheme = () => {
    document.documentElement.dataset.bsTheme = theme;
  };
  applyTheme();

  window.habitflowToggleTheme = () => {
    theme = theme === 'dark' ? 'light' : 'dark';
    applyTheme();
    try { localStorage.setItem('habitflow-theme', theme); } catch {}
  };

  // Enhanced navigation can replace the html attributes without rerunning this script.
  document.addEventListener('DOMContentLoaded', () => {
    window.Blazor?.addEventListener('enhancedload', applyTheme);
  }, { once: true });
})();
