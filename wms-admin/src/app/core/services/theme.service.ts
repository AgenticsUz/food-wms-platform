import { Injectable, signal } from '@angular/core';

const THEME_KEY = 'adminTheme';

/**
 * Mavzu `documentElement` ga `.dark-mode` klassini qo'yadi — `styles.scss` dagi
 * cocoa palitrasi shu klassdan ishlaydi, PrimeNG ham shu selektorga sozlangan
 * (`app.config.ts` → `darkModeSelector`).
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal(false);

  init() {
    const saved = localStorage.getItem(THEME_KEY);
    // Saqlangan tanlov bo'lmasa — tizim sozlamasiga ergashamiz
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
    this.apply(saved ? saved === 'dark' : prefersDark);
  }

  toggle() {
    this.apply(!this.isDark());
    localStorage.setItem(THEME_KEY, this.isDark() ? 'dark' : 'light');
  }

  private apply(dark: boolean) {
    this.isDark.set(dark);
    document.documentElement.classList.toggle('dark-mode', dark);
  }
}
