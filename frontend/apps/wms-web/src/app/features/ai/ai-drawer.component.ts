import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';

import type { AiDebtRow, AiStockLevelRow, AiToolOutput, AiTransferRow } from './ai.model';
import { AiStore } from './ai.store';

/**
 * AI yordamchisi paneli — qobiqdagi global tortma.
 *
 * ⚠️ Panel SAHIFAGA bog'liq emas: u qobiqda turadi va sahifa almashganda
 * suhbat saqlanadi (holat `AiStore` da). Har sahifada o'z paneli bo'lsa,
 * foydalanuvchi hujjatga o'tib qaytganda savol-javobni yo'qotardi.
 */
@Component({
  selector: 'app-ai-drawer',
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ai-drawer.component.html',
  styleUrl: './ai-drawer.component.scss',
})
export class AiDrawerComponent {
  readonly store = inject(AiStore);

  /** Kiritish maydoni — panel ochilganda fokus shu yerga tushadi. */
  private readonly input = viewChild<ElementRef<HTMLTextAreaElement>>('input');
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  draft = '';

  constructor() {
    effect(() => {
      if (this.store.isOpen()) {
        // Panel ochilishi bilan yozishga tayyor: bir bosish kamayadi.
        queueMicrotask(() => this.input()?.nativeElement.focus());
      }
    });

    effect(() => {
      // Yangi yozuv kelganda pastga suriladi — javob ekran tashqarisida qolmasin.
      this.store.entries();
      queueMicrotask(() => {
        const element = this.scroller()?.nativeElement;
        if (element) {
          element.scrollTop = element.scrollHeight;
        }
      });
    });
  }

  send(): void {
    const text = this.draft;
    this.draft = '';
    void this.store.ask(text);
  }

  /**
   * Enter — yuborish, Shift+Enter — yangi qator.
   *
   * Chat yuzalarining standart xatti-harakati; boshqacha bo'lsa foydalanuvchi
   * har savoldan keyin sichqonchani izlardi.
   */
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  /** Tool natijasi jadval bilan chiziladimi. */
  isTable(tool: AiToolOutput): boolean {
    return tool.code === 'stock_query' || tool.code === 'debt_query' || tool.code === 'pending_transfers';
  }

  stockRows(tool: AiToolOutput): readonly AiStockLevelRow[] {
    return isArrayOf<AiStockLevelRow>(tool.data, 'productName') ? tool.data : [];
  }

  debtRows(tool: AiToolOutput): readonly AiDebtRow[] {
    return isArrayOf<AiDebtRow>(tool.data, 'counterpartyName') ? tool.data : [];
  }

  transferRows(tool: AiToolOutput): readonly AiTransferRow[] {
    return isArrayOf<AiTransferRow>(tool.data, 'number') ? tool.data : [];
  }
}

/**
 * Natija kutilgan shakldagi massivmi.
 *
 * ⚠️ Tool natijasi `unknown`: backend DTO'si o'zgarsa yuza JIM qolishi kerak
 * (matn baribir ko'rsatiladi), jadval chizishga urinib yiqilmasligi kerak.
 */
function isArrayOf<T>(data: unknown, marker: keyof T & string): data is readonly T[] {
  return Array.isArray(data) && data.length > 0 && typeof data[0] === 'object'
    && data[0] !== null && marker in (data[0] as object);
}
