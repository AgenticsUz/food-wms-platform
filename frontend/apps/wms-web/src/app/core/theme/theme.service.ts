import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { APP_STORAGE_PREFIX } from '@agentics/config';

interface ApexGlobal {
  exec(chartId: string, method: string, options: unknown): void;
}

/**
 * Yorug'/qorong'i mavzu (eski `wms-ui/core/services/theme.service.ts`).
 *
 * Mavzu `documentElement` dagi `.dark-mode` sinfi bilan boshqariladi; PrimeNG
 * (`darkModeSelector: '.dark-mode'`) va Tailwind `dark:` (`styles.css` dagi
 * `@custom-variant`) shunga ulangan. Komponent SCSS'i — `:host-context(.dark-mode)`.
 *
 * Kalit `wms.theme.mode` — `@agentics/auth` chiqishda `theme.mode` qo'shimchasini
 * SAQLAYDI (u odamga emas, qurilmaga tegishli), ya'ni tanlov chiqishdan omon qoladi.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = `${inject(APP_STORAGE_PREFIX)}theme.mode`;

  readonly isDark = signal<boolean>(this.read() === 'dark');

  init(): void {
    this.apply();
  }

  toggle(): void {
    this.isDark.update((dark) => !dark);
    try {
      globalThis.localStorage?.setItem(this.storageKey, this.isDark() ? 'dark' : 'light');
    } catch {
      // Maxfiylik rejimi — tanlov faqat shu sahifa uchun.
    }
    this.apply();
  }

  private apply(): void {
    const dark = this.isDark();
    this.document.documentElement.classList.toggle('dark-mode', dark);
    // Ochiq grafiklar ham mavzuga ergashsin (ApexCharts o'z global'ini yozadi).
    try {
      (globalThis as { ApexCharts?: ApexGlobal }).ApexCharts?.exec('*', 'updateOptions', {
        theme: { mode: dark ? 'dark' : 'light' },
        tooltip: { theme: dark ? 'dark' : 'light' },
      });
    } catch {
      // Grafik hali chizilmagan.
    }
  }

  private read(): string | null {
    try {
      return globalThis.localStorage?.getItem(this.storageKey) ?? null;
    } catch {
      return null;
    }
  }
}
