import { Injectable, signal, effect } from '@angular/core';

declare const ApexCharts: { exec: (chartId: string, method: string, options: unknown) => void };

@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal<boolean>(localStorage.getItem('theme') === 'dark');

  constructor() {
    effect(() => {
      const dark = this.isDark();
      try {
        ApexCharts.exec('*', 'updateOptions', {
          theme: { mode: dark ? 'dark' : 'light' },
          tooltip: { theme: dark ? 'dark' : 'light' }
        });
      } catch {
        // ApexCharts not loaded yet or no charts rendered
      }
    });
  }

  toggle() {
    const newValue = !this.isDark();
    this.isDark.set(newValue);
    localStorage.setItem('theme', newValue ? 'dark' : 'light');
    this.applyTheme();
  }

  applyTheme() {
    if (this.isDark()) {
      document.documentElement.classList.add('dark-mode');
    } else {
      document.documentElement.classList.remove('dark-mode');
    }
  }

  init() {
    this.applyTheme();
  }
}
