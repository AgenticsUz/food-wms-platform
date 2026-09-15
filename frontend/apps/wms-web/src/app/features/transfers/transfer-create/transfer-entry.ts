import { computed, type Signal } from '@angular/core';

import type { Product } from '../../products/product.model';
import type { WarehouseDefaults } from '../../warehouse/warehouse.model';
import { TransferType } from '../transfer.model';

/**
 * Transfer formasining KIRITISH qulayliklari (P2.6, P2.7) — komponentdan ajratilgan.
 *
 * Nega alohida fayl: ikkalasi ham sof hisob (signal → signal, qiymat → qiymat) va
 * hech qanday so'rov qilmaydi. Komponentda qolsa u faqat shishardi; bu yerda esa
 * qoidani bir joyda o'qish mumkin.
 */

/** Qadoq bilan kiritishning hisoblangan holati. */
export interface PackQuantity {
  /**
   * «dona / quti» o'tkazgichi uchun yorliqlar; qadoqsiz mahsulotda `null`.
   * ⚠️ `readonly` massiv EMAS: `p-selectbutton` ning `options` kirishi mutable
   * massiv kutadi, `readonly` bilan shablon TS4104 bilan yiqilardi.
   */
  readonly options: Signal<{ label: string; value: boolean }[] | null>;
  /** ⚠️ Serverga ketadigan miqdor — DOIM asosiy birlikda. */
  readonly baseQuantity: Signal<number>;
  /** Qator ostidagi «= 600 dona» ishorasi; qadoq rejimi o'chiq bo'lsa `null`. */
  readonly hint: Signal<string | null>;
}

/**
 * Qadoq (P2.7) faqat KIRITISH qulayligi: «50 quti» yozilsa forma uni
 * `50 × packSize` donaga o'giradi. Qoldiq, FEFO va hisobotlar qadoqni bilmaydi.
 *
 * Qadoq ISHLAYDI deyish uchun `packSize > 0` va `packUnit` JUFT bo'lishi shart —
 * bittasi yetishmasa o'tkazgich umuman ko'rsatilmaydi.
 */
export function createPackQuantity(
  products: Signal<readonly Product[]>,
  productId: Signal<string | null>,
  quantity: Signal<number>,
  inPacks: Signal<boolean>
): PackQuantity {
  const product = computed(() => products().find((p) => p.id === productId()) ?? null);
  const baseUnit = computed(() => {
    const p = product();
    return p ? p.unitShortName || p.unitName : '';
  });
  const packSize = computed(() => {
    const p = product();
    return p?.packSize && p.packSize > 0 && p.packUnit ? p.packSize : null;
  });

  const baseQuantity = computed(() => {
    const size = packSize();
    return size !== null && inPacks() ? quantity() * size : quantity();
  });

  return {
    // Yorliqlar — mahsulotning O'Z nomlari («dona», «quti»), tarjima kaliti emas.
    options: computed(() => {
      const p = product();
      return p && packSize() !== null
        ? [
            { label: baseUnit(), value: false },
            { label: p.packUnit ?? '', value: true },
          ]
        : null;
    }),
    baseQuantity,
    hint: computed(() =>
      packSize() !== null && inPacks() ? `= ${baseQuantity()} ${baseUnit()}` : null
    ),
  };
}

/**
 * Turga mos sukut ombor (P2.6): QAYSI maydonga va QAYSI ombor.
 *
 * Tur mantiqi: kirim — xomashyo omboriga keladi; qaytarish — tovar tayyor
 * mahsulot omboriga qaytadi; chiqim (sotuv) tayyor mahsulot omboridan chiqadi;
 * ichki o'tkazma esa odatda xomashyo omboridan boshlanadi.
 *
 * ⚠️ Faqat `effective*` o'qiladi: ustunlik tartibini (xodimning shaxsiy tanlovi →
 * tenant sozlamasi → yagona ombor) server hisoblab beradi.
 */
export function defaultWarehouseFor(
  defaults: WarehouseDefaults,
  type: TransferType
): { readonly field: 'from' | 'to'; readonly id: string | null } {
  if (type === TransferType.Incoming) return { field: 'to', id: defaults.effectiveRawId };
  if (type === TransferType.Return) return { field: 'to', id: defaults.effectiveFinishedId };
  if (type === TransferType.Outgoing) return { field: 'from', id: defaults.effectiveFinishedId };
  return { field: 'from', id: defaults.effectiveRawId };
}

/**
 * «Oxirgi narx» taklifi QAYSI tur bo'yicha so'ralishi: kirimda — oxirgi kirim
 * narxi, chiqimda — oxirgi sotuv narxi. Qaytarish ham SOTUV narxiga tayanadi
 * (tovar shu narxda chiqqan edi). Ichki o'tkazma va ishlab chiqarishda savdo
 * narxi ma'nosiz — taklif so'ralmaydi (`null`).
 */
export function priceTypeFor(type: TransferType): TransferType | null {
  if (type === TransferType.Incoming) return TransferType.Incoming;
  if (type === TransferType.Outgoing || type === TransferType.Return) return TransferType.Outgoing;
  return null;
}
