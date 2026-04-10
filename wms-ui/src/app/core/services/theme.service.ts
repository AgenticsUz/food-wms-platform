import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal<boolean>(localStorage.getItem('theme') === 'dark');

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
