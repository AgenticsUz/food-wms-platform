import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProductionService } from '../../../core/services/production.service';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ProductionOrderCreateDto, ProductionRecipe } from '../../../core/models/production.model';

interface SimpleUser {
  id: number;
  fullName: string;
}

@Component({
  selector: 'app-order-create',
  standalone: true,
  imports: [FormsModule, Button, InputNumber, Select, DatePicker, Textarea, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss'
})
export default class OrderCreateComponent implements OnInit {
  private productionService = inject(ProductionService);
  private apiService = inject(ApiService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  saving = signal(false);
  recipes = signal<ProductionRecipe[]>([]);
  users = signal<SimpleUser[]>([]);

  recipeId = signal<number | null>(null);
  plannedQuantity = signal<number>(0);
  plannedStartDate = signal<Date | null>(null);
  plannedEndDate = signal<Date | null>(null);
  assignedToUserId = signal<number | null>(null);
  note = signal('');

  ngOnInit() {
    this.loadRecipes();
    this.loadUsers();
  }

  private loadRecipes() {
    this.productionService.getRecipes().subscribe({
      next: (res) => { if (res.success && res.data) this.recipes.set(res.data); }
    });
  }

  private loadUsers() {
    this.apiService.get<SimpleUser[]>('users').subscribe({
      next: (res) => { if (res.success && res.data) this.users.set(res.data); }
    });
  }

  submit() {
    if (!this.recipeId()) { this.notify.warn('Select a recipe'); return; }
    if (this.plannedQuantity() <= 0) { this.notify.warn('Planned quantity must be greater than 0'); return; }
    if (!this.plannedStartDate()) { this.notify.warn('Select a start date'); return; }

    this.saving.set(true);

    const dto: ProductionOrderCreateDto = {
      recipeId: this.recipeId()!,
      plannedQuantity: this.plannedQuantity(),
      plannedStartDate: toLocalDateString(this.plannedStartDate()!),
      plannedEndDate: (this.plannedEndDate() ? toLocalDateString(this.plannedEndDate()!) : null) ?? null,
      assignedToUserId: this.assignedToUserId(),
      note: this.note().trim() || null
    };

    this.productionService.createOrder(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success('Production order created');
        this.router.navigate(['/production/orders']);
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to create production order');
      }
    });
  }

  goBack() {
    this.router.navigate(['/production/orders']);
  }
}
