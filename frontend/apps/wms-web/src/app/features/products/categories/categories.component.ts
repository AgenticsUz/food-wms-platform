import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Category } from '../product.model';
import { ProductService } from '../product.service';

interface CategoryForm {
  readonly id: string | null;
  readonly name: string;
  readonly parentId: string | null;
}

const EMPTY_FORM: CategoryForm = { id: null, name: '', parentId: null };

/** Mahsulot kategoriyalari daraxti (eski `products/categories`). */
@Component({
  selector: 'app-categories',
  imports: [FormsModule, TableModule, Button, InputText, Dialog, Select, TranslocoDirective, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export default class CategoriesComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly notify = inject(NotificationService);

  readonly categories = signal<Category[]>([]);
  /** Jadval uchun yassi ro'yxat; daraja nomdagi chekinish bilan ko'rsatiladi (`white-space: pre`). */
  readonly flatCategories = computed(() => flatten(this.categories()));
  /** Ota kategoriya sifatida faqat ildizlar (eski ekran bilan bir xil). */
  readonly parentOptions = computed(() => this.categories().map((c) => ({ label: c.name, value: c.id })));
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CategoryForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.productService.getCategories().subscribe({
      next: (res) => {
        this.categories.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(cat: Category): void {
    this.form.set({ id: cat.id, name: cat.name.trim(), parentId: cat.parentId });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Category name is required');
      return;
    }

    this.saving.set(true);
    const dto = { name: f.name.trim(), parentId: f.parentId };
    const request =
      f.id !== null ? this.productService.updateCategory(f.id, dto) : this.productService.createCategory(dto);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(f.id !== null ? 'Category updated' : 'Category created');
        this.loadCategories();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteCategory(cat: Category): void {
    this.notify.confirmDelete(`Delete "${cat.name.trim()}"?`, () => {
      this.productService.deleteCategory(cat.id).subscribe({
        next: () => {
          this.notify.success('Category deleted');
          this.loadCategories();
        },
        error: () => undefined,
      });
    });
  }

  updateForm<K extends keyof CategoryForm>(field: K, value: CategoryForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}

function flatten(cats: readonly Category[], level = 0): Category[] {
  return cats.flatMap((cat) => [
    { ...cat, name: '  '.repeat(level) + cat.name },
    ...flatten(cat.children ?? [], level + 1),
  ]);
}
