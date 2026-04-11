export const APEX_DEFAULTS = {
  chart: {
    fontFamily: 'DM Sans, sans-serif',
    toolbar: { show: false },
    animations: {
      enabled: true,
      easing: 'easeinout' as const,
      speed: 600,
    },
  },

  grid: {
    borderColor: '#e2e8f0',
    strokeDashArray: 4,
    xaxis: { lines: { show: false } },
  },

  tooltip: {
    theme: 'light' as const,
    style: { fontFamily: 'DM Sans, sans-serif', fontSize: '13px' },
  },

  colors: ['#6366f1', '#10b981', '#f59e0b', '#3b82f6', '#ef4444', '#8b5cf6'],
};
