import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToggleSwitch } from 'primeng/toggleswitch';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { TelegramLinkDialogComponent, type TelegramLinkApi } from '../../../shared/components/telegram-link-dialog/telegram-link-dialog.component';
import type { Driver } from '../delivery.model';
import { DeliveryService } from '../delivery.service';

interface DriverForm {
  fullName: string;
  phone: string | null;
  licenseNumber: string | null;
  isActive: boolean;
}

const EMPTY_FORM: DriverForm = { fullName: '', phone: null, licenseNumber: null, isActive: true };

@Component({
  selector: 'app-drivers',
  imports: [
    FormsModule,
    TranslocoDirective,
    TableModule,
    Button,
    Dialog,
    InputText,
    ToggleSwitch,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective,
    TelegramLinkDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './drivers.component.html',
  styleUrl: '../vehicles/vehicles.component.scss',
})
export default class DriversComponent implements OnInit {
  private readonly service = inject(DeliveryService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly drivers = signal<Driver[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly dialogVisible = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly form = signal<DriverForm>(EMPTY_FORM);

  /** Telegram ulash dialogi (TG12): tanlangan haydovchi va uning API'si. */
  readonly telegramVisible = signal(false);
  readonly telegramDriver = signal<Driver | null>(null);
  readonly telegramApi = signal<TelegramLinkApi | null>(null);

  openTelegram(d: Driver): void {
    this.telegramDriver.set(d);
    this.telegramApi.set(this.service.driverTelegram(d.id));
    this.telegramVisible.set(true);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getDrivers().subscribe({
      next: (res) => {
        this.drivers.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editingId.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(d: Driver): void {
    this.form.set({
      fullName: d.fullName,
      phone: d.phone,
      licenseNumber: d.licenseNumber,
      isActive: d.isActive,
    });
    this.editingId.set(d.id);
    this.dialogVisible.set(true);
  }

  updateForm<K extends keyof DriverForm>(field: K, value: DriverForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const f = this.form();
    if (!f.fullName.trim()) {
      this.notify.warn(this.language.translate('delivery.driverNameRequired'));
      return;
    }
    this.saving.set(true);
    const dto = {
      fullName: f.fullName.trim(),
      phone: f.phone,
      licenseNumber: f.licenseNumber,
      isActive: f.isActive,
    };
    const id = this.editingId();
    const request = id === null ? this.service.createDriver(dto) : this.service.updateDriver(id, dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(d: Driver): void {
    this.notify.confirmDelete(`${d.fullName}?`, () => {
      this.service.deleteDriver(d.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.load();
        },
      });
    });
  }
}
