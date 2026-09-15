import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Warehouse, WarehouseDefaults } from '../../warehouse/warehouse.model';
import { WarehouseService } from '../../warehouse/warehouse.service';

/**
 * Standart ombor sozlamasi (P2.6) — transfer formasi omborni har safar
 * so'ramasligi uchun.
 *
 * Sahifada IKKI XIL sozlama bor va ular ataylab bir ekranda: odam «mening
 * omborim» ni o'zgartirganda tenant sozlamasi nimaligini ham ko'rib tursin, aks
 * holda «nega mening tanlovim ishlamadi» degan savol tug'ilardi.
 *
 *  - Tenant sozlamasi — `settings.modules` ruxsati bilan (tenant egasining qarori,
 *    `modules.component` naqshi: ruxsat yo'q bo'lsa ko'rinadi, lekin tahrir yopiq).
 *  - Shaxsiy tanlov («Mening omborim») — har qanday xodim o'zgartira oladi va u
 *    tenant sozlamasini BOSIB KETADI.
 *
 * ⚠️ Ustunlik tartibi SERVERDA hisoblanadi (`effective*`). Bu ekran uni qayta
 * hisoblamaydi — faqat serverdan kelgan natijani ko'rsatadi.
 */
@Component({
  selector: 'app-warehouse-defaults',
  imports: [FormsModule, TranslocoDirective, Button, Select, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './warehouse-defaults.component.html',
  styleUrl: './warehouse-defaults.component.scss',
})
export default class WarehouseDefaultsComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly session = inject(WmsSession);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly warehouses = signal<Warehouse[]>([]);
  readonly defaults = signal<WarehouseDefaults | null>(null);
  readonly loading = signal(true);
  readonly savingTenant = signal(false);
  readonly savingMine = signal(false);

  /** Tenant sozlamasini faqat `settings.modules` egasi yozadi (API ham shunday). */
  readonly canManage = computed(() => this.session.can('settings.modules'));

  // Formadagi tahrir qilinayotgan qiymatlar — serverdan kelgan holatdan ajratilgan:
  // «Saqlash» bosilmaguncha ekrandagi tanlov serverdagi haqiqatni o'zgartirmaydi.
  readonly rawId = signal<string | null>(null);
  readonly finishedId = signal<string | null>(null);
  readonly myId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadDefaults();
  }

  private loadWarehouses(): void {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) this.warehouses.set(res.data);
      },
    });
  }

  private loadDefaults(): void {
    this.loading.set(true);
    this.warehouseService.getDefaults().subscribe({
      next: (res) => {
        const data = res.success && res.data ? res.data : null;
        this.defaults.set(data);
        this.rawId.set(data?.rawWarehouseId ?? null);
        this.finishedId.set(data?.finishedWarehouseId ?? null);
        this.myId.set(data?.userWarehouseId ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  /** Ombor nomi — `effective*` natijasini odam o'qiydigan holda ko'rsatish uchun. */
  warehouseName(id: string | null): string | null {
    if (!id) return null;
    return this.warehouses().find((w) => w.id === id)?.name ?? null;
  }

  /** ⚠️ ALMASHTIRISH semantikasi: ikkala maydon ham yuboriladi, `null` — o'chiradi. */
  saveTenant(): void {
    if (!this.canManage()) return;
    this.savingTenant.set(true);
    this.warehouseService
      .setDefaults({ rawWarehouseId: this.rawId(), finishedWarehouseId: this.finishedId() })
      .subscribe({
        next: () => {
          this.savingTenant.set(false);
          this.notify.success(this.language.translate('common.success'));
          // `effective*` server tomonda qayta hisoblanadi — uni o'zimiz taxmin qilmaymiz.
          this.loadDefaults();
        },
        error: () => this.savingTenant.set(false),
      });
  }

  saveMine(): void {
    this.savingMine.set(true);
    this.warehouseService.setMyDefaultWarehouse(this.myId()).subscribe({
      next: () => {
        this.savingMine.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.loadDefaults();
      },
      error: () => this.savingMine.set(false),
    });
  }

  /** Shaxsiy tanlovni olib tashlash — shundan keyin tenant sozlamasi amal qiladi. */
  clearMine(): void {
    this.myId.set(null);
    this.saveMine();
  }
}
