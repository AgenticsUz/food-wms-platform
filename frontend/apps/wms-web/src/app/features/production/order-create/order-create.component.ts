import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { ProductionOrderCreateDto, ProductionRecipe, UserOption } from '../production.model';
import { ProductionService } from '../production.service';

@Component({
  selector: 'app-order-create',
  imports: [FormsModule, Button, InputNumber, Select, DatePicker, Textarea, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss',
})
export default class OrderCreateComponent implements OnInit {
  private readonly productionService = inject(ProductionService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly recipes = signal<ProductionRecipe[]>([]);
  readonly users = signal<UserOption[]>([]);

  readonly recipeId = signal<string | null>(null);
  readonly plannedQuantity = signal<number>(0);
  readonly plannedStartDate = signal<Date | null>(null);
  readonly plannedEndDate = signal<Date | null>(null);
  readonly assignedToUserId = signal<string | null>(null);
  readonly note = signal('');

  ngOnInit(): void {
    this.productionService.getRecipes().subscribe({
      next: (res) => this.recipes.set(res.data ?? []),
    });
    this.productionService.getUsers().subscribe({
      next: (res) => this.users.set(res.data ?? []),
      // `settings.users` ruxsati bo'lmasa — maydon bo'sh qoladi (servis izohi).
      error: () => undefined,
    });
  }

  submit(): void {
    const recipeId = this.recipeId();
    const start = this.plannedStartDate();
    const end = this.plannedEndDate();
    if (!recipeId) { this.notify.warn('Select a recipe'); return; }
    if (this.plannedQuantity() <= 0) { this.notify.warn('Planned quantity must be greater than 0'); return; }
    if (!start) { this.notify.warn('Select a start date'); return; }

    // Reja sanasi — kalendar kuni, vaqt nuqtasi emas: `toLocalDateString` (date.util qoidasi).
    const dto: ProductionOrderCreateDto = {
      recipeId,
      plannedQuantity: this.plannedQuantity(),
      plannedStartDate: toLocalDateString(start),
      plannedEndDate: end ? toLocalDateString(end) : null,
      assignedToUserId: this.assignedToUserId(),
      note: this.note().trim() || null,
    };

    this.saving.set(true);
    this.productionService.createOrder(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success('Production order created');
        void this.router.navigate(['/production/orders']);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.saving.set(false),
    });
  }

  goBack(): void {
    void this.router.navigate(['/production/orders']);
  }
}
