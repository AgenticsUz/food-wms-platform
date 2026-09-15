import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { SEARCH_CALL, onSearchChange } from '../../../core/utils/search.util';
import { ImportButtonComponent } from '../../../shared/components/import-button/import-button.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { injectTranslationTick } from '../../warehouse/translation-tick';
import { ProductType, type Category, type Product, type ProductCreateDto, type Unit } from '../product.model';
import { ALL_PRODUCTS, ProductService, productSearch } from '../product.service';

interface ProductForm {
  readonly id: string | null;
  readonly name: string;
  readonly categoryId: string | null;
  readonly unitId: string | null;
  readonly type: ProductType;
  readonly minStock: number;
  readonly shelfLifeDays: number | null;
  readonly barcode: string | null;
  readonly costPrice: number | null;
  readonly packSize: number | null;
  readonly packUnit: string | null;
}

const EMPTY_FORM: ProductForm = {
  id: null,
  name: '',
  categoryId: null,
  unitId: null,
  type: ProductType.Raw,
  minStock: 0,
  shelfLifeDays: null,
  barcode: null,
  costPrice: null,
  packSize: null,
  packUnit: null,
};

/** Mahsulot katalogi (eski `products/product-list`). */
@Component({
  selector: 'app-product-list',
  imports: [
    DecimalPipe,
    FormsModule,
    TableModule,
    Button,
    InputText,
    Select,
    Dialog,
    InputNumber,
    TranslocoDirective,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective,
    ImportButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
})
export default class ProductListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);
  private readonly translationTick = injectTranslationTick();
  readonly exportService = inject(ExportService);

  readonly products = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly units = signal<Unit[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly typeFilter = signal<ProductType | null>(null);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly form = signal<ProductForm>(EMPTY_FORM);

  /**
   * Excel eksport `export.excel` feature'i ostida. Feature'lar endi fail-closed —
   * yoqilmagan tarifda tugma bosilsa 403 toast chiqardi, shuning uchun ko'rsatilmaydi.
   */
  readonly canExport = computed(() => this.session.isFeatureEnabled('export.excel'));

  readonly typeOptions = computed(() => {
    this.translationTick();
    const t = (key: string): string => this.language.translate(key);
    return [
      { label: t('products.allTypes'), value: null },
      { label: t('products.raw'), value: ProductType.Raw },
      { label: t('products.semiFinished'), value: ProductType.SemiFinished },
      { label: t('products.finished'), value: ProductType.Finished },
    ];
  });
  readonly formTypeOptions = computed(() => this.typeOptions().slice(1));

  /**
   * Tur filtri MIJOZDA QOLDI: backend `GET products` `type` ni bilmaydi (eski
   * ekran uni yuborardi — server e'tiborsiz qoldirardi). Qidiruv esa endi
   * SERVERDA, shuning uchun bu yerda `name.includes(q)` yo'q: u alifboni
   * bilmasdi va «Сникерс» yozgan odam «Snikers» ni topa olmasdi.
   */
  readonly filteredProducts = computed(() => {
    const type = this.typeFilter();
    return type === null ? this.products() : this.products().filter((p) => p.type === type);
  });

  constructor() {
    // Har harfga so'rov ketmasin — `onSearchChange` 300 ms kutadi (search.util.ts).
    onSearchChange(this.search, () => this.loadProducts());
  }

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
    this.loadUnits();
  }

  loadProducts(): void {
    const query = this.search().trim();
    this.loading.set(true);
    // Qidiruvda global progress chizig'i chaqnamasin (`SEARCH_CALL`) — kutish
    // holati jadvalning o'z `loading` i bilan ko'rsatiladi.
    this.productService
      .getProducts(query ? productSearch(query) : ALL_PRODUCTS, query ? SEARCH_CALL : undefined)
      .subscribe({
        next: (res) => {
          this.products.set(res.success && res.data ? res.data : []);
          this.loading.set(false);
        },
        // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
        error: () => this.loading.set(false),
      });
  }

  private loadCategories(): void {
    this.productService.getCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) this.categories.set(res.data);
      },
    });
  }

  private loadUnits(): void {
    this.productService.getUnits().subscribe({
      next: (res) => {
        if (res.success && res.data) this.units.set(res.data);
      },
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(product: Product): void {
    this.form.set({
      id: product.id,
      name: product.name,
      categoryId: product.categoryId,
      unitId: product.unitId,
      type: product.type,
      minStock: product.minStock,
      shelfLifeDays: product.shelfLifeDays,
      barcode: product.barcode,
      costPrice: product.costPrice,
      packSize: product.packSize,
      packUnit: product.packUnit,
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Product name is required');
      return;
    }
    // Backend `CategoryId`/`UnitId` — majburiy Guid: bo'sh qiymat JSON bog'lanishida
    // tushunarsiz 400 berardi (eskisi `0` yuborib FK xatosini olardi). Oldindan aytamiz.
    if (f.categoryId === null) {
      this.notify.warn(this.requiredMessage('products.category'));
      return;
    }
    if (f.unitId === null) {
      this.notify.warn(this.requiredMessage('common.unit'));
      return;
    }
    // Qadoq JUFT maydon (P2.7). Serverda ham shu qoida bor, lekin bu yerda
    // aytilmasa odam «12» yozib, nomini unutib, 400 xatosini olardi.
    const packUnit = f.packUnit?.trim() || null;
    const hasSize = f.packSize !== null;
    if (hasSize !== (packUnit !== null)) {
      this.notify.warn(this.language.translate('products.packPairRequired'));
      return;
    }
    if (hasSize && (f.packSize ?? 0) <= 0) {
      this.notify.warn(this.language.translate('products.packSizePositive'));
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
      costPrice: f.costPrice,
      packSize: f.packSize,
      packUnit,
    };

    const request =
      f.id !== null ? this.productService.updateProduct(f.id, dto) : this.productService.createProduct(dto);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(f.id !== null ? 'Product updated' : 'Product created');
        this.loadProducts();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteProduct(product: Product): void {
    this.notify.confirmDelete(`Delete "${product.name}"?`, () => {
      this.productService.deleteProduct(product.id).subscribe({
        next: () => {
          this.notify.success('Product deleted');
          this.loadProducts();
        },
        error: () => undefined,
      });
    });
  }

  typeKey(type: ProductType): string {
    switch (type) {
      case ProductType.Raw:
        return 'products.raw';
      case ProductType.SemiFinished:
        return 'products.semiFinished';
      default:
        return 'products.finished';
    }
  }

  typeStatus(type: ProductType): string {
    switch (type) {
      case ProductType.Raw:
        return 'Pending';
      case ProductType.SemiFinished:
        return 'InProgress';
      case ProductType.Finished:
        return 'Confirmed';
      default:
        return 'Neutral';
    }
  }

  /**
   * Jadvaldagi qadoq ustuni: «1 quti = 12 dona». Bitta satr sifatida shu yerda
   * yig'iladi — shablonda bo'laklab yozilsa tarjima qorovuli (`template/i18n`)
   * orasidagi «=» ni qotirilgan matn deb ushlardi.
   */
  packLabel(product: Product): string | null {
    if (product.packSize === null || !product.packUnit) return null;
    const base = product.unitShortName || product.unitName;
    return `1 ${product.packUnit} = ${product.packSize} ${base}`;
  }

  exportProducts(): void {
    this.exportService.download('export/products', `products-${toLocalDateString(new Date())}.xlsx`);
  }

  updateForm<K extends keyof ProductForm>(field: K, value: ProductForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  private requiredMessage(fieldKey: string): string {
    return `${this.language.translate(fieldKey)}: ${this.language.translate('common.required')}`;
  }
}
