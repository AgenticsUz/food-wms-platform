import { Injectable, inject } from '@angular/core';
import { AuthService } from '@agentics/auth';
import { ConfigService } from '@agentics/config';
import { LanguageService } from '@agentics/i18n';
import { TenantStore } from '@agentics/tenant';

import { ApiService } from '../../core/api/api.service';
import { WmsSession } from '../../core/auth/wms-session';
import type {
  AiConversation,
  AiConversationDetail,
  AiPaymentDraft,
  AiStreamEvent,
  AiTransferDraft,
} from './ai.model';

/** Savol yuborilayotgan kontekst — system promptning o'zgaruvchan qismiga tushadi. */
export interface AiAskOptions {
  readonly conversationId?: string | null;
  /** Foydalanuvchi ko'rayotgan sahifa («Omborlar / Sklad1»). */
  readonly pageContext?: string | null;
  readonly signal?: AbortSignal;
}

/**
 * AI paneli API'si.
 *
 * ⚠️ <b>Nega `HttpClient` emas, `fetch`.</b> Javob `text/event-stream` bo'lib
 * BO'LAK-BO'LAK keladi; `HttpClient` esa javobni to'liq kutib, oxirida bir marta
 * beradi — ya'ni oqimning ma'nosi qolmasdi. Brauzerning `EventSource` i ham
 * yaramaydi: u `Authorization` sarlavhasini yubora olmaydi va bizda token
 * cookie'da emas.
 *
 * Buning narxi — interceptor zanjiri (token, tenant, til, xato toasti) BU YO'LDA
 * ISHLAMAYDI, shuning uchun sarlavhalar shu yerda qo'lda qo'yiladi. Ro'yxat
 * qisqa va u zanjirdagi to'rttasi bilan AYNAN bir xil bo'lishi kerak.
 */
@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly config = inject(ConfigService);
  private readonly tenants = inject(TenantStore);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);

  /** Suhbatlar ro'yxati (faqat o'zining). */
  getConversations() {
    return this.api.get<AiConversation[]>('ai/conversations');
  }

  /** Bitta suhbat va uning xabarlari. */
  getConversation(id: string) {
    return this.api.get<AiConversationDetail>(`ai/conversations/${id}`);
  }

  /**
   * Qoralamadan hujjat yaratadi (`Pending`).
   *
   * ⚠️ Bu — ODAM tugmasi (F10 §0.6). AI qatlamida bunday tool YO'Q: model bu manzilni
   * ko'rmaydi ham, chaqira ham olmaydi. `Source = ai` ni SERVER qo'yadi — mijoz o'z
   * hujjatini «AI yozgan» deb ko'rsata olmasin.
   */
  createTransfer(draft: AiTransferDraft, conversationId: string | null) {
    return this.api.post<{ id: string; number: number }>('ai/drafts/transfer', {
      draft,
      conversationId,
    });
  }

  /** Qoralamadan to'lov yozuvini kiritadi (izohi yuqorida). */
  createPayment(draft: AiPaymentDraft, conversationId: string | null) {
    return this.api.post<{ id: string }>('ai/drafts/payment', { draft, conversationId });
  }

  /**
   * Savol yuboradi va hodisalarni kelgan sayin qaytaradi.
   *
   * @throws Xato javobda (AI o'chiq, kvota) — `Error` matn bilan. Rad javobi
   * BIRINCHI hodisadan oldin keladi, ya'ni odatdagi JSON bo'lib tushadi
   * (backend controlleridagi izoh).
   */
  async *ask(text: string, options: AiAskOptions = {}): AsyncGenerator<AiStreamEvent> {
    const response = await fetch(`${this.config.apiUrl()}/${this.endpoint()}`, {
      method: 'POST',
      headers: this.headers(),
      body: JSON.stringify({
        text,
        conversationId: options.conversationId ?? null,
        pageContext: options.pageContext ?? null,
      }),
      signal: options.signal,
    });

    if (!response.ok) {
      throw new Error(await readErrorMessage(response));
    }

    const body = response.body;
    if (body === null) {
      return;
    }

    yield* readEvents(body);
  }

  /**
   * Kabinet va ilova BOSHQA manzilga boradi.
   *
   * ⚠️ Gateway bitta, tool to'plami boshqa: kabinet foydalanuvchisining WMS ruxsati
   * ataylab bo'sh va unga `portal.self` kodini AYNAN o'sha manzil beradi
   * (`PortalController` izohi). Bitta manzil bo'lsa, ruxsat to'plamini mijoz
   * tanlagan bo'lardi — ya'ni chegara mijozda qolardi.
   */
  private endpoint(): string {
    return this.session.isPortalUser() ? 'portal/ai/chat' : 'ai/chat';
  }

  /**
   * ⚠️ Zanjirdagi to'rt sarlavha: token, tenant, til va JSON turi. Bittasi
   * tushib qolsa nosozlik tushunarsiz bo'ladi — masalan tenant sarlavhasisiz
   * platforma admini boshqa tenantning kontekstida qolib ketardi.
   */
  private headers(): Record<string, string> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream',
      'Accept-Language': this.language.language(),
    };

    const token = this.auth.accessToken();
    if (token !== null) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const tenantId = this.tenants.tenantId();
    if (tenantId !== null) {
      headers['X-Tenant-Id'] = tenantId;
    }

    return headers;
  }
}

/**
 * SSE oqimini hodisalarga ajratadi.
 *
 * ⚠️ Bo'lak chegarasi hodisa chegarasi EMAS: bitta `data:` qatori ikki bo'lakka
 * bo'linib kelishi mumkin. Shuning uchun bufer saqlanadi va faqat TO'LIQ
 * hodisalar (`\n\n` bilan tugagani) uzatiladi.
 */
async function* readEvents(body: ReadableStream<Uint8Array>): AsyncGenerator<AiStreamEvent> {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      let boundary = buffer.indexOf('\n\n');
      while (boundary !== -1) {
        const chunk = buffer.slice(0, boundary);
        buffer = buffer.slice(boundary + 2);

        const event = parseEvent(chunk);
        if (event !== null) {
          yield event;
        }

        boundary = buffer.indexOf('\n\n');
      }
    }
  } finally {
    // Panel yopilganda (`AbortSignal`) o'qish to'xtaydi va ulanish bo'shatiladi.
    reader.releaseLock();
  }
}

function parseEvent(chunk: string): AiStreamEvent | null {
  const line = chunk.split('\n').find((part) => part.startsWith('data:'));
  if (line === undefined) {
    return null;
  }

  try {
    return JSON.parse(line.slice('data:'.length).trim()) as AiStreamEvent;
  } catch {
    // Buzuq hodisa butun suhbatni to'xtatmasin — keyingisi kelaveradi.
    return null;
  }
}

/** Rad javobidagi matn (`ApiResponse.message`); o'qib bo'lmasa — holat kodi. */
async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string };
    if (typeof body.message === 'string' && body.message.length > 0) {
      return body.message;
    }
  } catch {
    // JSON emas — pastdagi zaxira matn.
  }

  return `HTTP ${response.status}`;
}
