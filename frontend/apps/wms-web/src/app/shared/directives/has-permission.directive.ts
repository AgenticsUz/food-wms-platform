import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';

import { WmsSession } from '../../core/auth/wms-session';

/**
 * `*hasPermission="'warehouse.manage'"` — ruxsat bo'lmasa element chizilmaydi.
 * Eski `wms-ui` direktivasi; manba endi `WmsSession` (`/api/me`). Faqat UX —
 * haqiqiy tekshiruv backendda.
 *
 * Selektor ESKI nomida (`app` prefiksisiz) ataylab: ko'chirilgan shablonlar
 * o'zgarishsiz ishlasin. Yangi direktivalar `app*` prefiksi bilan yoziladi.
 */
@Directive({
  // eslint-disable-next-line @angular-eslint/directive-selector -- eski shablonlar bilan moslik (D2)
  selector: '[hasPermission]',
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly session = inject(WmsSession);

  readonly hasPermission = input.required<string | readonly string[]>();

  private hasView = false;

  constructor() {
    effect(() => {
      const value = this.hasPermission();
      const codes = typeof value === 'string' ? [value] : value;
      const allowed = this.session.canAny(...codes);

      if (allowed && !this.hasView) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.hasView = true;
      } else if (!allowed && this.hasView) {
        this.viewContainer.clear();
        this.hasView = false;
      }
    });
  }
}
