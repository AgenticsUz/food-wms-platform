import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { TableModule } from 'primeng/table';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { LanguageService } from '@agentics/i18n';

import { isWmsApiError } from '../../../core/api/wms-api-error';
import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Product } from '../../products/product.model';
import { ProductService, productSearch } from '../../products/product.service';
import { injectTranslationTick } from '../../warehouse/translation-tick';
import type { Warehouse } from '../../warehouse/warehouse.model';
import { WarehouseService } from '../../warehouse/warehouse.service';
import {
  CounterpartyType,
  TransferLookupsService,
  type AgentOption,
  type CounterpartyOption,
} from '../transfer-lookups.service';
import { ReturnReason, TransferType, type TransferCreateDto, type TransferItemDto } from '../transfer.model';
import { TransferService } from '../transfer.service';

interface DraftItem extends TransferItemDto {
  readonly productName: string;
}

/** Guid (`xxxxxxxx-xxxx-…`) — asl transfer id'si shu shaklda bo'lmasa backend 400 beradi. */
const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Shtrix-kod maydonida nom yozilganda ko'rsatiladigan takliflar soni. */
const BARCODE_SEARCH_LIMIT = 5;

/**
 * Yangi transfer (eski `transfers/transfer-create`).
 *
 * Eskisidan farqlar (yangi backend/qobiq talabi):
 *  - shtrix-kod (klaviatura skaneri, Enter) SERVERDA qidiriladi
 *    (`products/by-barcode`); topilmasa yuklangan ro'yxatdan nomi bo'yicha;
 *  - asl transfer (qaytarishda) — Guid matni, raqam emas;
 *  - kontragent/agent ro'yxati modul va ruxsat bo'lsagina so'raladi;
 *  - `app-limit-notice` qobiqda hali yo'q — olib tashlandi (limitda backend 402 beradi).
 */
@Component({
  selector: 'app-transfer-create',
  imports: [DecimalPipe, FormsModule, TranslocoDirective, Button, InputNumber, InputText, Select, Textarea, TableModule, ToggleSwitch, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-create.component.html',
  styleUrl: './transfer-create.component.scss',
})
export default class TransferCreateComponent implements OnInit {
  private readonly transferService = inject(TransferService);
  private readonly lookups = inject(TransferLookupsService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly session = inject(WmsSession);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly translationTick = injectTranslationTick();

  readonly saving = signal(false);
  readonly transferType = signal<TransferType>(TransferType.Incoming);
  readonly counterpartyId = signal<string | null>(null);
  readonly fromWarehouseId = signal<string | null>(null);
  readonly toWarehouseId = signal<string | null>(null);
  readonly note = signal('');
  readonly items = signal<DraftItem[]>([]);

  // Qo'lda qo'shish qatori
  readonly itemProductId = signal<string | null>(null);
  readonly itemQuantity = signal(0);
  readonly itemUnitPrice = signal(0);

  /**
   * Manba ombordagi mavjud qoldiq (mahsulot → miqdor).
   *
   * Nega kerak: ilgari forma qoldiqni umuman bilmasdi — menejer yo'q tovarga hujjat
   * yozar, xato esa faqat TASDIQDA chiqardi. Server tekshiruvi qoladi (bu yerda
   * ko'rsatilgan raqam eskirgan bo'lishi mumkin), bu — erta ogohlantirish.
   */
  readonly sourceStock = signal<ReadonlyMap<string, number>>(new Map());

  readonly counterparties = signal<CounterpartyOption[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly products = signal<Product[]>([]);
  readonly agents = signal<AgentOption[]>([]);

  // Agent (faqat chiqim — sotuv)
  readonly viaAgent = signal(false);
  readonly agentId = signal<string | null>(null);
  readonly commissionPercent = signal<number | null>(null);

  // Qaytarish
  readonly returnReason = signal<ReturnReason | null>(null);
  readonly originalTransferId = signal('');

  // Shtrix-kod
  readonly barcodeQuery = signal('');
  readonly barcodeResults = signal<Product[]>([]);
  private barcodeBusy = false;

  /**
   * `agents` — AGENTS moduli va `agents.view` ostida. Ular bo'lmasa so'rov 403
   * berib, forma har ochilganda qizil toast chiqardi; «agent orqali» tanlovi ham
   * ma'nosiz — yashiriladi.
   */
  readonly agentsAvailable = computed(
    () => this.session.isModuleEnabled('AGENTS') && this.session.can('agents.view')
  );
  /** `counterparties` — SUPPLIERS yoki CLIENTS moduli va `partners.view` ostida. */
  private readonly counterpartiesAvailable = computed(
    () =>
      (this.session.isModuleEnabled('SUPPLIERS') || this.session.isModuleEnabled('CLIENTS')) &&
      this.session.can('partners.view')
  );

  readonly isIncoming = computed(() => this.transferType() === TransferType.Incoming);
  readonly isOutgoing = computed(() => this.transferType() === TransferType.Outgoing);
  readonly isInternal = computed(() => this.transferType() === TransferType.Internal);
  readonly isReturn = computed(() => this.transferType() === TransferType.Return);

  readonly typeOptions = computed(() => {
    this.translationTick();
    const t = (key: string): string => this.language.translate(key);
    return [
      { label: t('transfer.incoming'), value: TransferType.Incoming },
      { label: t('transfer.outgoing'), value: TransferType.Outgoing },
      { label: t('transfer.internal'), value: TransferType.Internal },
      { label: t('transfer.return'), value: TransferType.Return },
    ];
  });

  readonly returnReasonOptions = computed(() => {
    this.translationTick();
    const t = (key: string): string => this.language.translate(key);
    return [
      { label: t('transfer.expired'), value: ReturnReason.Expired },
      { label: t('transfer.unsold'), value: ReturnReason.Unsold },
      { label: t('transfer.defective'), value: ReturnReason.Defective },
      { label: t('transfer.other'), value: ReturnReason.Other },
    ];
  });

  /** Kirim — yetkazib beruvchilar; chiqim va qaytarish — mijozlar; «ikkalasi» — hammasida. */
  readonly filteredCounterparties = computed(() => {
    const type = this.transferType();
    return this.counterparties().filter((c) => {
      if (type === TransferType.Incoming) return c.type === CounterpartyType.Supplier || c.type === CounterpartyType.Both;
      if (type === TransferType.Outgoing || type === TransferType.Return) {
        return c.type === CounterpartyType.Client || c.type === CounterpartyType.Both;
      }
      return false;
    });
  });

  readonly totalAmount = computed(() => this.items().reduce((sum, i) => sum + i.quantity * i.unitPrice, 0));

  /** Qoldiq faqat ombordan CHIQADIGAN hujjatlarda ma'noli. */
  readonly checksStock = computed(() => this.isOutgoing() || this.isInternal());

  /** Qatorlar bo'yicha yig'ilgan talab — bitta mahsulot ikki qatorda bo'lishi mumkin. */
  private readonly requested = computed(() => {
    const map = new Map<string, number>();
    for (const item of this.items()) {
      map.set(item.productId, (map.get(item.productId) ?? 0) + item.quantity);
    }
    return map;
  });

  /** Qoldig'i yetmaydigan mahsulot nomlari — tugma ostidagi ogohlantirish uchun. */
  readonly shortages = computed(() => {
    if (!this.checksStock() || !this.fromWarehouseId()) return [];
    const stock = this.sourceStock();
    return [...this.requested()]
      .filter(([productId, quantity]) => quantity > (stock.get(productId) ?? 0))
      .map(([productId, quantity]) => ({
        name: this.products().find((p) => p.id === productId)?.name ?? productId,
        requested: quantity,
        available: stock.get(productId) ?? 0,
      }));
  });

  ngOnInit(): void {
    this.loadCounterparties();
    this.loadWarehouses();
    this.loadProducts();
    this.loadAgents();
  }

  private loadAgents(): void {
    if (!this.agentsAvailable()) return;
    this.lookups.getAgents().subscribe({
      next: (res) => {
        if (res.success && res.data) this.agents.set(res.data.filter((a) => a.isActive));
      },
    });
  }

  private loadCounterparties(): void {
    if (!this.counterpartiesAvailable()) return;
    this.lookups.getCounterparties().subscribe({
      next: (res) => {
        if (res.success && res.data) this.counterparties.set(res.data);
      },
    });
  }

  private loadWarehouses(): void {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) this.warehouses.set(res.data);
      },
    });
  }

  private loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (res) => {
        if (res.success && res.data) this.products.set(res.data);
      },
    });
  }

  /** Manba ombor tanlangach qoldiq qayta so'raladi (tanlov o'chsa — tozalanadi). */
  onFromWarehouseChange(id: string | null): void {
    this.fromWarehouseId.set(id);
    this.loadSourceStock();
  }

  /** Tur o'zgarsa manba ombor ham, qoldiq ham ma'nosini yo'qotishi mumkin. */
  onTypeChange(type: TransferType): void {
    this.transferType.set(type);
    this.loadSourceStock();
  }

  private loadSourceStock(): void {
    const warehouseId = this.fromWarehouseId();
    if (!warehouseId || !this.checksStock()) {
      this.sourceStock.set(new Map());
      return;
    }

    // Ruxsat bo'lmasa toast chiqmaydi: qoldiq — yordamchi ma'lumot, forma usiz ham ishlaydi.
    this.warehouseService.getStock(warehouseId).subscribe({
      next: (res) => {
        const rows = res.success && res.data ? res.data : [];
        this.sourceStock.set(new Map(rows.map((r) => [r.productId, r.availableQuantity])));
      },
      error: () => this.sourceStock.set(new Map()),
    });
  }

  /** Tanlangan mahsulotning manba omboridagi qoldig'i — qo'shish qatorining yonida. */
  availableFor(productId: string | null): number | null {
    if (!productId || !this.checksStock() || !this.fromWarehouseId()) return null;
    return this.sourceStock().get(productId) ?? 0;
  }

  /**
   * Qo'shish qatoridagi ishora uchun. ⚠️ Signal sifatida: shablonda `@if (x; as y)`
   * bilan olinsa NOL qiymat «yo'q» deb yashirilardi — aynan eng muhim holat.
   */
  readonly selectedAvailable = computed(() => this.availableFor(this.itemProductId()));

  onAgentChange(id: string | null): void {
    this.agentId.set(id);
    const agent = this.agents().find((a) => a.id === id);
    if (agent) this.commissionPercent.set(agent.commissionPercent);
  }

  /** Mijozga agent biriktirilgan bo'lsa chiqimda agent avtomatik tanlanadi. */
  onCounterpartyChange(id: string | null): void {
    this.counterpartyId.set(id);
    if (!this.isOutgoing() || !this.agentsAvailable()) return;
    const cp = this.counterparties().find((c) => c.id === id);
    if (cp?.agentId) {
      this.viaAgent.set(true);
      this.onAgentChange(cp.agentId);
    }
  }

  addItem(): void {
    const pid = this.itemProductId();
    const qty = this.itemQuantity();
    const price = this.itemUnitPrice();
    if (!pid) {
      this.notify.warn('Select a product');
      return;
    }
    if (qty <= 0) {
      this.notify.warn('Quantity must be greater than 0');
      return;
    }
    if (price < 0) {
      this.notify.warn('Price cannot be negative');
      return;
    }
    const product = this.products().find((p) => p.id === pid);

    // To'sib qo'yilmaydi — menejer bilib turib yozishi mumkin (kirim yo'lda); lekin
    // ogohlantirish TASDIQGACHA ko'rinsin.
    const available = this.availableFor(pid);
    if (available !== null && qty + (this.requested().get(pid) ?? 0) > available) {
      this.notify.warn(
        this.language.translate('transfer.stockShort', {
          name: product?.name ?? '',
          available,
        })
      );
    }

    this.items.update((list) => [
      ...list,
      { productId: pid, productName: product?.name ?? '', batchId: null, quantity: qty, unitPrice: price },
    ]);
    this.itemProductId.set(null);
    this.itemQuantity.set(0);
    this.itemUnitPrice.set(0);
  }

  removeItem(index: number): void {
    this.items.update((list) => list.filter((_, i) => i !== index));
  }

  onBarcodeInput(value: string): void {
    this.barcodeQuery.set(value);
    if (!value.trim()) this.barcodeResults.set([]);
  }

  /**
   * Skaner kodni yozib Enter bosadi. Avval SERVERDAN aniq shtrix-kod bo'yicha
   * (`products/by-barcode`): tanlov ro'yxati birinchi sahifa bilan cheklangan
   * bo'lishi mumkin, server esa butun katalogni biladi. 404 — kod emas, balki
   * nom yozilgan bo'lishi mumkin: nomni ham SERVERDAN qidiramiz.
   *
   * Toast o'chirilgan (`skipErrorNotify`): nom bo'yicha qidiruvda 404 — kutilgan
   * holat, u «topilmadi» xatosi bo'lib chiqmasligi kerak. Boshqa xatoni o'zimiz
   * ko'rsatamiz.
   */
  searchByBarcode(): void {
    const query = this.barcodeQuery().trim();
    if (!query || this.barcodeBusy) return;
    this.barcodeBusy = true;

    this.productService.getByBarcode(query, { skipErrorNotify: true, skipLoading: true }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.barcodeBusy = false;
          this.acceptProduct(res.data);
        } else {
          this.searchOnServer(query);
        }
      },
      error: (err: unknown) => {
        if (isWmsApiError(err) && err.status === 404) {
          this.searchOnServer(query);
          return;
        }
        this.barcodeBusy = false;
        const message = isWmsApiError(err) ? err.message : null;
        this.notify.error(message ?? this.language.translate('errors.unknown'));
      },
    });
  }

  selectFromResults(product: Product): void {
    this.acceptProduct(product);
  }

  /**
   * Nom bo'yicha qidiruv SERVERDA (pg_trgm + lotin↔kirill): mijozdagi
   * `includes` alifboni bilmasdi — «Сникерс» yozgan «Snikers» ni topa olmasdi,
   * ustiga yuklangan ro'yxat bilan ham cheklanardi. `pageSize` kichik: bu
   * tanlov ro'yxati, butun katalog emas.
   */
  private searchOnServer(query: string): void {
    this.productService
      .getProducts(productSearch(query, BARCODE_SEARCH_LIMIT), {
        skipErrorNotify: true,
        skipLoading: true,
      })
      .subscribe({
        next: (res) => {
          this.barcodeBusy = false;
          const matches = res.success && res.data ? res.data : [];
          if (matches.length === 1) {
            this.acceptProduct(matches[0]);
          } else if (matches.length > 1) {
            this.barcodeResults.set(matches);
          } else {
            this.notify.warn('Product not found');
            this.barcodeResults.set([]);
          }
        },
        error: () => {
          this.barcodeBusy = false;
          this.notify.warn('Product not found');
          this.barcodeResults.set([]);
        },
      });
  }

  private acceptProduct(product: Product): void {
    this.items.update((list) => [
      ...list,
      { productId: product.id, productName: product.name, batchId: null, quantity: 1, unitPrice: product.costPrice ?? 0 },
    ]);
    this.barcodeQuery.set('');
    this.barcodeResults.set([]);
    this.notify.success(`Added: ${product.name}`);
  }

  submit(): void {
    if (this.items().length === 0) {
      this.notify.warn('Add at least one item');
      return;
    }
    const type = this.transferType();
    const needsCounterparty =
      type === TransferType.Incoming || type === TransferType.Outgoing || type === TransferType.Return;
    if (needsCounterparty && !this.counterpartyId()) {
      this.notify.warn('Select a counterparty');
      return;
    }
    if ((type === TransferType.Incoming || type === TransferType.Return) && !this.toWarehouseId()) {
      this.notify.warn('Select a destination warehouse');
      return;
    }
    if (type === TransferType.Outgoing && !this.fromWarehouseId()) {
      this.notify.warn('Select a source warehouse');
      return;
    }
    if (type === TransferType.Internal && (!this.fromWarehouseId() || !this.toWarehouseId())) {
      this.notify.warn('Select source and destination warehouses');
      return;
    }
    if (type === TransferType.Internal && this.fromWarehouseId() === this.toWarehouseId()) {
      this.notify.warn('Source and destination warehouses must differ');
      return;
    }
    const originalId = this.originalTransferId().trim();
    if (type === TransferType.Return) {
      if (!this.returnReason()) {
        this.notify.warn('Select a return reason');
        return;
      }
      if (originalId && !GUID_RE.test(originalId)) {
        this.notify.warn('Original transfer: paste the full transfer ID');
        return;
      }
    }

    const useAgent = this.isOutgoing() && this.agentsAvailable() && this.viaAgent();
    if (useAgent && !this.agentId()) {
      this.notify.warn('Select an agent or turn off "via agent"');
      return;
    }

    const isReturn = type === TransferType.Return;
    const dto: TransferCreateDto = {
      type,
      fromWarehouseId: this.fromWarehouseId(),
      toWarehouseId: this.toWarehouseId(),
      counterpartyId: this.counterpartyId(),
      agentId: useAgent ? this.agentId() : null,
      commissionPercent: useAgent ? this.commissionPercent() : null,
      returnReason: isReturn ? this.returnReason() : null,
      originalTransferId: isReturn && originalId ? originalId : null,
      note: this.note() || null,
      items: this.items().map((i) => ({
        productId: i.productId,
        batchId: i.batchId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
      })),
    };

    this.saving.set(true);
    this.transferService.createTransfer(dto).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.notify.success('Transfer created');
        void this.router.navigate(res.data ? ['/transfers', res.data.id] : ['/transfers']);
      },
      // Xatoni (backend xabari bilan) `WmsErrorNotifier` ko'rsatdi — takrorlanmaydi.
      error: () => this.saving.set(false),
    });
  }

  goBack(): void {
    void this.router.navigate(['/transfers']);
  }
}
