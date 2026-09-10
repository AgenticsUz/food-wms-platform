import { inject, type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';

/**
 * Faol til lug'ati YUKLANGANDA (va til almashganda) yangilanadigan signal.
 *
 * `p-select` ga tanlovlar tayyor matn bilan beriladi (`optionLabel`), ya'ni
 * ular TS'da `translate()` bilan yig'iladi. `computed` shu signalni o'qisa,
 * ro'yxat yangi tilga o'tadi. Faqat `LanguageService.language` ga bog'lansa
 * yetmasdi: `ru` alohida chunk — til belgisi darhol o'zgaradi, lug'at esa
 * keyinroq keladi va ro'yxat kalitning o'zi bilan qotib qolardi.
 */
export function injectTranslationTick(): Signal<unknown> {
  return toSignal(inject(TranslocoService).selectTranslation(), { initialValue: null });
}
