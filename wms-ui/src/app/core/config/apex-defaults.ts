export const APEX_DEFAULTS = {
  chart: {
    fontFamily: 'Inter, sans-serif',
    toolbar: { show: false },
    animations: {
      enabled: true,
      easing: 'easeinout' as const,
      speed: 600,
    },
  },

  grid: {
    borderColor: 'rgba(99,88,70,0.08)',
    strokeDashArray: 4,
    xaxis: { lines: { show: false } },
  },

  tooltip: {
    theme: 'light' as const,
    style: { fontFamily: 'Inter, sans-serif', fontSize: '13px' },
  },

  stroke: { curve: 'smooth' as const, width: 2 },

  // Brand palitra: pistachio, berry, caramel, blueberry, cocoa, pistachio-soft
  colors: ['#5e9540', '#d94c66', '#c8902f', '#4a6fb5', '#8a7e6a', '#7eb35a'],
};
