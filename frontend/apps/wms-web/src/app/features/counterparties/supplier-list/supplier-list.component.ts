import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { ImportButtonComponent } from '../../../shared/components/import-button/import-button.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import {
  counterpartyFormOf,
  counterpartySaveDtoOf,
  emptyCounterpartyForm,
  isInnInvalid,
  matchesCounterparty,
  type CounterpartyForm,
} from '../counterparty-form';
import { CounterpartyType, type Counterparty } from '../counterparty.model';
import { CounterpartyService } from '../counterparty.service';

@Component({
  selector: 'app-supplier-list',
  imports: [
    FormsModule, TableModule, Button, InputText, Dialog, Textarea, TranslocoDirective,
    PageHeaderComponent, HasPermissionDirective, ImportButtonComponent, PhoneInputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.scss',
})
export default class SupplierListComponent implements OnInit {
  private readonly service = inject(CounterpartyService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  readonly exportService = inject(ExportService);

  readonly items = signal<Counterparty[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);

  readonly form = signal<CounterpartyForm>(emptyCounterpartyForm(CounterpartyType.Supplier));

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase();
    return q ? this.items().filter((c) => matchesCounterparty(c, q)) : this.items();
  });

  readonly innInvalid = computed(() => isInnInvalid(this.form().inn));

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.service.getCounterparties(CounterpartyType.Supplier).subscribe({
      next: (res) => {
        this.items.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(emptyCounterpartyForm(CounterpartyType.Supplier));
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(c: Counterparty): void {
    this.form.set(counterpartyFormOf(c));
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    if (this.innInvalid()) {
      this.notify.warn(this.language.translate('partners.innInvalid'));
      return;
    }
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Name is required');
      return;
    }
    this.saving.set(true);
    const dto = counterpartySaveDtoOf(f);
    const request =
      this.editing() && f.id !== null ? this.service.updateCounterparty(f.id, dto) : this.service.createCounterparty(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Supplier updated' : 'Supplier created');
        this.loadData();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteItem(c: Counterparty): void {
    this.notify.confirmDelete(`Delete "${c.name}"?`, () => {
      this.service.deleteCounterparty(c.id).subscribe({
        next: () => {
          this.notify.success('Supplier deleted');
          this.loadData();
        },
        // Xato toastini qobiq chiqaradi.
        error: () => undefined,
      });
    });
  }

  viewDetail(c: Counterparty): void {
    void this.router.navigate(['/counterparties', c.id]);
  }

  exportSuppliers(): void {
    this.exportService.download('export/counterparties', `suppliers-${toLocalDateString(new Date())}.xlsx`, {
      type: CounterpartyType.Supplier,
    });
  }

  updateForm<K extends keyof CounterpartyForm>(field: K, value: CounterpartyForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
