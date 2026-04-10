import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Category, CategoryCreateDto } from '../../../core/models/product.model';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, Select, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export default class CategoriesComponent implements OnInit {
  private productService = inject(ProductService);
  private notify = inject(NotificationService);

  categories = signal<Category[]>([]);
  flatCategories = signal<Category[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<CategoryCreateDto & { id?: number }>({
    name: '',
    parentId: null
  });

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.loading.set(true);
    this.productService.getCategories().subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.categories.set(list);
        this.flatCategories.set(this.flattenCategories(list));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load categories');
      }
    });
  }

  private flattenCategories(cats: Category[], level = 0): Category[] {
    const result: Category[] = [];
    for (const cat of cats) {
      result.push({ ...cat, name: '  '.repeat(level) + cat.name });
      if (cat.children && cat.children.length > 0) {
        result.push(...this.flattenCategories(cat.children, level + 1));
      }
    }
    return result;
  }

  openNew() {
    this.form.set({ name: '', parentId: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(cat: Category) {
    this.form.set({ id: cat.id, name: cat.name.trim(), parentId: cat.parentId });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Category name is required');
      return;
    }

    this.saving.set(true);
    const dto: CategoryCreateDto = { name: f.name.trim(), parentId: f.parentId };

    const obs = this.editing()
      ? this.productService.updateCategory(f.id!, dto)
      : this.productService.createCategory(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Category updated' : 'Category created');
        this.loadCategories();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save category');
      }
    });
  }

  deleteCategory(cat: Category) {
    this.notify.confirmDelete(`Delete "${cat.name.trim()}"?`, () => {
      this.productService.deleteCategory(cat.id).subscribe({
        next: () => {
          this.notify.success('Category deleted');
          this.loadCategories();
        },
        error: () => this.notify.error('Failed to delete category')
      });
    });
  }

  parentOptions() {
    return this.categories().map(c => ({ label: c.name, value: c.id }));
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
