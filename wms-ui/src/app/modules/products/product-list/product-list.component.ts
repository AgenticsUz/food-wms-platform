import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Product, ProductCreateDto, ProductType, Category, Unit } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule, TableModule, Button, InputText, Select,
    Dialog, InputNumber, TranslocoDirective, PageHeaderComponent, StatusBadgeComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export default class ProductListComponent implements OnInit {
  private productService = inject(ProductService);
  private notify = inject(NotificationService);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  units = signal<Unit[]>([]);
  loading = signal(true);
  search = signal('');
  typeFilter = signal<number | null>(null);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  typeOptions = [
    { label: 'All Types', value: null },
    { label: 'Raw Material', value: ProductType.Raw },
    { label: 'Semi-Finished', value: ProductType.SemiFinished },
    { label: 'Finished', value: ProductType.Finished }
  ];

  form = signal<ProductCreateDto & { id?: number }>({
    name: '',
    categoryId: 0,
    unitId: 0,
    type: ProductType.Raw,
    minStock: 0,
    shelfLifeDays: null,
    barcode: null,
    costPrice: null
  });

  filteredProducts = signal<Product[]>([]);

  ngOnInit() {
    this.loadProducts();
    this.loadCategories();
    this.loadUnits();
  }

  loadProducts() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = {};
    if (this.typeFilter()) {
      params['type'] = this.typeFilter()!;
    }
    this.productService.getProducts(params).subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.products.set(list);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load products');
      }
    });
  }

  private loadCategories() {
    this.productService.getCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) this.categories.set(res.data);
      }
    });
  }

  private loadUnits() {
    this.productService.getUnits().subscribe({
      next: (res) => {
        if (res.success && res.data) this.units.set(res.data);
      }
    });
  }

  applyFilter() {
    let list = this.products();
    const q = this.search().toLowerCase();
    if (q) {
      list = list.filter(p => p.name.toLowerCase().includes(q) || (p.barcode && p.barcode.toLowerCase().includes(q)));
    }
    if (this.typeFilter()) {
      list = list.filter(p => p.type === this.typeFilter());
    }
    this.filteredProducts.set(list);
  }

  onSearchChange(value: string) {
    this.search.set(value);
    this.applyFilter();
  }

  onTypeChange(value: number | null) {
    this.typeFilter.set(value);
    this.loadProducts();
  }

  openNew() {
    this.form.set({
      name: '',
      categoryId: 0,
      unitId: 0,
      type: ProductType.Raw,
      minStock: 0,
      shelfLifeDays: null,
      barcode: null,
      costPrice: null
    });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(product: Product) {
    this.form.set({
      id: product.id,
      name: product.name,
      categoryId: product.categoryId,
      unitId: product.unitId,
      type: product.type,
      minStock: product.minStock,
      shelfLifeDays: product.shelfLifeDays,
      barcode: product.barcode,
      costPrice: product.costPrice
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Product name is required');
      return;
    }

    this.saving.set(true);
    const dto: ProductCreateDto = {
      name: f.name,
      categoryId: f.categoryId,
      unitId: f.unitId,
      type: f.type,
      minStock: f.minStock,
      shelfLifeDays: f.shelfLifeDays,
      barcode: f.barcode,
      costPrice: f.costPrice
    };

    const obs = this.editing()
      ? this.productService.updateProduct(f.id!, dto)
      : this.productService.createProduct(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Product updated' : 'Product created');
        this.loadProducts();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save product');
      }
    });
  }

  deleteProduct(product: Product) {
    this.notify.confirmDelete(`Delete "${product.name}"?`, () => {
      this.productService.deleteProduct(product.id).subscribe({
        next: () => {
          this.notify.success('Product deleted');
          this.loadProducts();
        },
        error: () => this.notify.error('Failed to delete product')
      });
    });
  }

  getTypeName(type: ProductType): string {
    switch (type) {
      case ProductType.Raw: return 'Raw';
      case ProductType.SemiFinished: return 'Semi-Finished';
      case ProductType.Finished: return 'Finished';
      default: return 'Unknown';
    }
  }

  getTypeStatus(type: ProductType): string {
    switch (type) {
      case ProductType.Raw: return 'Pending';
      case ProductType.SemiFinished: return 'InProgress';
      case ProductType.Finished: return 'Confirmed';
      default: return 'Neutral';
    }
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
