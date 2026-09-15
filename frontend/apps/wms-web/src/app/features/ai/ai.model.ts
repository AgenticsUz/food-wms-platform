/** AI kanali — suhbat qaysi yuzada boshlangan. */
export const enum AiChannel {
  Web = 1,
  Telegram = 2,
}

/** Suhbatdagi xabar egasi (`AiMessageRole` backend enum'i bilan bir xil). */
export const enum AiMessageRole {
  User = 1,
  Assistant = 2,
  Tool = 3,
}

/** Bitta tool natijasi — yuza shuni chizadi. */
export interface AiToolOutput {
  /** Tool kodi (`stock_query`) — qaysi komponent chizishini shundan bilamiz. */
  readonly code: string;
  /** Modelga ketgan qisqa matn — komponenti yo'q tool'lar shu holida ko'rsatiladi. */
  readonly text: string;
  /** Strukturali natija; `null` — chizadigan narsa yo'q. */
  readonly data: unknown;
}

/**
 * SSE hodisasi.
 *
 * ⚠️ Hodisalar QADAM darajasida (backend izohi): har tool bajarilgach bittasi
 * keladi, model javobi esa oxirida bitta `text` bo'lib tushadi. Ya'ni harflar
 * paydo bo'lishini kutmang — ko'rinadigan narsa «nima qilinayotgani».
 */
export interface AiStreamEvent {
  readonly type: 'text' | 'tool_result' | 'done' | 'error';
  readonly text?: string;
  readonly tool?: AiToolOutput;
  readonly conversationId?: string;
  readonly code?: string;
}

/** Panelda ko'rinadigan bitta yozuv. */
export interface AiChatEntry {
  readonly role: 'user' | 'assistant' | 'error';
  readonly text: string;
  /** Javob tayyorlanayotganda bajarilgan tool'lar (javobdan oldin ko'rinadi). */
  readonly tools: readonly AiToolOutput[];
  /** Model hali javob bermadi — «yozmoqda» ko'rsatkichi. */
  readonly pending: boolean;
}

/** Ro'yxatdagi suhbat. */
export interface AiConversation {
  readonly id: string;
  readonly title: string | null;
  readonly channel: AiChannel;
  readonly lastActivityAt: string;
  readonly messageCount: number;
}

/** Saqlangan xabar. */
export interface AiMessage {
  readonly sequence: number;
  readonly role: AiMessageRole;
  readonly text: string | null;
  readonly tools: readonly string[];
  readonly createdAt: string;
}

/** Suhbat va uning xabarlari. */
export interface AiConversationDetail {
  readonly conversation: AiConversation;
  readonly messages: readonly AiMessage[];
}

/**
 * `stock_query` ning butun ombor kesimi — jadval bo'lib chiziladi.
 *
 * ⚠️ Maydonlar backend DTO'si bilan bir xil nomda: yuza JSON'ni QAYTA nomlamaydi,
 * aks holda backend maydon qo'shganda u jimgina yo'qolardi.
 */
export interface AiStockLevelRow {
  readonly productName: string;
  readonly unitShortName: string;
  readonly currentStock: number;
  readonly minStock: number;
  readonly isLow: boolean;
}

/** `debt_query` qatori. */
export interface AiDebtRow {
  readonly counterpartyId: string;
  readonly counterpartyName: string;
  readonly amount: number;
}

/** `pending_transfers` qatori. */
export interface AiTransferRow {
  readonly id: string;
  readonly number: number;
  readonly type: number;
  readonly documentDate: string;
  readonly counterpartyName: string | null;
}
