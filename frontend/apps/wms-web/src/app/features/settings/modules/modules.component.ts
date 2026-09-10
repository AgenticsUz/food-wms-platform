import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { WMS_MODULES } from '../../../core/auth/wms-me.model';
import { WmsSession } from '../../../core/auth/wms-session';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

/**
 * Faqat-ko'rish sahifa (D6). Modullar Agentics orqali sotiladi va Identity'dagi
 * tenant obunasida yoqiladi — WMS'da o'chirgich yo'q. Manba `/api/me`
 * (`WmsSession.modules`), alohida so'rov kerak emas: menyu ham shu ro'yxatga qaraydi,
 * ikki manba bo'lsa bir-biridan ajralib qolardi.
 */
@Component({
  selector: 'app-modules',
  imports: [RouterLink, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './modules.component.html',
  styleUrl: './modules.component.scss',
})
export default class ModulesComponent {
  private readonly session = inject(WmsSession);

  /** Hamma 11 modul — yoqilganmi yoki yo'qmi; o'chiqlari ham ko'rinadi (yumshoq upsell). */
  readonly modules = computed(() =>
    WMS_MODULES.map((code) => ({ code, isEnabled: this.session.modules().has(code) }))
  );
}
