import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { toSignal } from '@angular/core/rxjs-interop';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { TableModule } from 'primeng/table';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { TransferService } from '../../../core/services/transfer.service';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { TransferType, TransferCreateDto, TransferItemDto } from '../../../core/models/transfer.model';
import { Counterparty } from '../../../core/models/counterparty.model';
import { Warehouse } from '../../../core/models/warehouse.model';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-transfer-create',
  standalone: true,
  imports: [DecimalPipe, FormsModule, TranslocoDirective, Button, InputNumber, Select, Textarea, TableModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-create.component.html',
  styleUrl: './transfer-create.component.scss'
})
export default class TransferCreateComponent implements OnInit {
  private transferService = inject(TransferService);
  private counterpartyService = inject(CounterpartyService);
  private warehouseService = inject(WarehouseService);
  private productService = inject(ProductService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  private transloco = inject(TranslocoService);
  private lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  saving = signal(false);
  transferType = signal<TransferType>(TransferType.Incoming);
  counterpartyId = signal<number | null>(null);
  fromWarehouseId = signal<number | null>(null);
  toWarehouseId = signal<number | null>(null);
  note = signal('');
  items = signal<(TransferItemDto & { productName?: string })[]>([]);

  // Item form
  itemProductId = signal<number | null>(null);
  itemQuantity = signal<number>(0);
  itemUnitPrice = signal<number>(0);

  counterparties = signal<Counterparty[]>([]);
  warehouses = signal<Warehouse[]>([]);
  products = signal<Product[]>([]);

  // Barcode search
  barcodeQuery = signal('');
  barcodeResults = signal<Product[]>([]);

  typeOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('transfer.incoming'), value: TransferType.Incoming },
      { label: this.transloco.translate('transfer.outgoing'), value: TransferType.Outgoing },
      { label: this.transloco.translate('transfer.internal'), value: TransferType.Internal }
    ];
  });

  ngOnInit() {
    this.loadCounterparties();
    this.loadWarehouses();
    this.loadProducts();
  }

  private loadCounterparties() {
    this.counterpartyService.getCounterparties().subscribe({
      next: (res) => { if (res.success && res.data) this.counterparties.set(res.data); }
    });
  }

  private loadWarehouses() {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => { if (res.success && res.data) this.warehouses.set(res.data); }
    });
  }

  private loadProducts() {
    this.productService.getProducts().subscribe({
      next: (res) => { if (res.success && res.data) this.products.set(res.data); }
    });
  }

  get isIncoming() { return this.transferType() === TransferType.Incoming; }
  get isOutgoing() { return this.transferType() === TransferType.Outgoing; }
  get isInternal() { return this.transferType() === TransferType.Internal; }

  get filteredCounterparties() {
    const type = this.transferType();
    return this.counterparties().filter(c => {
      if (type === TransferType.Incoming) return c.type === 1 || c.type === 3;
      if (type === TransferType.Outgoing) return c.type === 2 || c.type === 3;
      return false;
    });
  }

  get totalAmount(): number {
    return this.items().reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);
  }

  addItem() {
    const pid = this.itemProductId();
    const qty = this.itemQuantity();
    const price = this.itemUnitPrice();
    if (!pid) { this.notify.warn('Select a product'); return; }
    if (qty <= 0) { this.notify.warn('Quantity must be greater than 0'); return; }
    const product = this.products().find(p => p.id === pid);
    this.items.update(list => [...list, {
      productId: pid,
      productName: product?.name,
      batchId: null,
      quantity: qty,
      unitPrice: price
    }]);
    this.itemProductId.set(null);
    this.itemQuantity.set(0);
    this.itemUnitPrice.set(0);
  }

  removeItem(index: number) {
    this.items.update(list => list.filter((_, i) => i !== index));
  }

  onBarcodeInput(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.barcodeQuery.set(value);
    if (!value.trim()) {
      this.barcodeResults.set([]);
    }
  }

  searchByBarcode() {
    const query = this.barcodeQuery().trim();
    if (!query) return;

    const exactMatch = this.products().find(p => p.barcode === query);
    if (exactMatch) {
      this.addProductToItems(exactMatch);
      this.barcodeQuery.set('');
      this.barcodeResults.set([]);
      this.notify.success(`Added: ${exactMatch.name}`);
      return;
    }

    const nameMatches = this.products().filter(p =>
      p.name.toLowerCase().includes(query.toLowerCase()) ||
      (p.barcode && p.barcode.toLowerCase().includes(query.toLowerCase()))
    );

    if (nameMatches.length === 1) {
      this.addProductToItems(nameMatches[0]);
      this.barcodeQuery.set('');
      this.barcodeResults.set([]);
      this.notify.success(`Added: ${nameMatches[0].name}`);
    } else if (nameMatches.length > 1) {
      this.barcodeResults.set(nameMatches.slice(0, 5));
    } else {
      this.notify.warn('Product not found');
      this.barcodeResults.set([]);
    }
  }

  selectFromResults(product: Product) {
    this.addProductToItems(product);
    this.barcodeResults.set([]);
    this.barcodeQuery.set('');
    this.notify.success(`Added: ${product.name}`);
  }

  private addProductToItems(product: Product) {
    this.items.update(list => [...list, {
      productId: product.id,
      productName: product.name,
      batchId: null,
      quantity: 1,
      unitPrice: product.costPrice ?? 0
    }]);
  }

  submit() {
    if (this.items().length === 0) { this.notify.warn('Add at least one item'); return; }
    const type = this.transferType();
    if ((type === TransferType.Incoming || type === TransferType.Outgoing) && !this.counterpartyId()) {
      this.notify.warn('Select a counterparty'); return;
    }
    if (type === TransferType.Incoming && !this.toWarehouseId()) {
      this.notify.warn('Select a destination warehouse'); return;
    }
    if (type === TransferType.Outgoing && !this.fromWarehouseId()) {
      this.notify.warn('Select a source warehouse'); return;
    }
    if (type === TransferType.Internal && (!this.fromWarehouseId() || !this.toWarehouseId())) {
      this.notify.warn('Select source and destination warehouses'); return;
    }

    this.saving.set(true);
    const dto: TransferCreateDto = {
      type,
      fromWarehouseId: this.fromWarehouseId(),
      toWarehouseId: this.toWarehouseId(),
      counterpartyId: this.counterpartyId(),
      note: this.note() || null,
      items: this.items().map(i => ({
        productId: i.productId,
        batchId: i.batchId,
        quantity: i.quantity,
        unitPrice: i.unitPrice
      }))
    };

    this.transferService.createTransfer(dto).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.notify.success('Transfer created');
        this.router.navigate(['/transfers', res.data?.id ?? '']);
      },
      error: () => { this.saving.set(false); this.notify.error('Failed to create transfer'); }
    });
  }

  goBack() { this.router.navigate(['/transfers']); }
}
