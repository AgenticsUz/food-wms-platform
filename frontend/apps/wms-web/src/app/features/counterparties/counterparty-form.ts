import type { Counterparty, CounterpartySaveDto, CounterpartyType } from './counterparty.model';

/**
 * Yetkazib beruvchi va mijoz ro'yxatlarining umumiy forma holati.
 *
 * F6: `portalPhone`/`portalEnabled` (D8) formadan olib tashlandi — backend ularni
 * endi qabul qilmaydi. INN — oddiy maydon (D10: avtomatik bog'lash yo'q).
 */
export interface CounterpartyForm {
  readonly id: string | null;
  readonly name: string;
  readonly type: CounterpartyType;
  readonly phone: string | null;
  readonly inn: string | null;
  readonly address: string | null;
  readonly note: string | null;
  readonly agentId: string | null;
}

export function emptyCounterpartyForm(type: CounterpartyType): CounterpartyForm {
  return { id: null, name: '', type, phone: null, inn: null, address: null, note: null, agentId: null };
}

export function counterpartyFormOf(c: Counterparty): CounterpartyForm {
  return {
    id: c.id,
    name: c.name,
    type: c.type,
    phone: c.phone,
    inn: c.inn,
    address: c.address,
    note: c.note,
    agentId: c.agentId,
  };
}

export function counterpartySaveDtoOf(f: CounterpartyForm): CounterpartySaveDto {
  return {
    name: f.name,
    type: f.type,
    phone: f.phone,
    inn: f.inn?.trim() || null,
    address: f.address,
    note: f.note,
    agentId: f.agentId,
  };
}

/** O'zbekiston STIR — aynan 9 raqam yoki bo'sh. */
export function isInnInvalid(inn: string | null): boolean {
  const value = inn?.trim();
  return !!value && !/^\d{9}$/.test(value);
}

/** Ro'yxat qidiruvi — nom, telefon yoki INN bo'yicha. */
export function matchesCounterparty(c: Counterparty, query: string): boolean {
  return (
    c.name.toLowerCase().includes(query) ||
    (c.phone ?? '').includes(query) ||
    (c.inn ?? '').includes(query)
  );
}
