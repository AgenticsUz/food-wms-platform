import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import type {
  AiChatEntry,
  AiConversation,
  AiPaymentDraft,
  AiToolOutput,
  AiTransferDraft,
} from './ai.model';
import { AiService } from './ai.service';

/**
 * AI panelining holati — Signals.
 *
 * ⚠️ Panel qobiqda (shell) turadi va sahifa almashganda YO'QOLMAYDI: store
 * `providedIn: 'root'`, ya'ni suhbat sahifadan sahifaga o'tganda saqlanadi.
 * Komponent holatida tursa, foydalanuvchi hujjatga o'tib qaytganda savol-javob
 * yo'qolardi — bu esa AI'dan foydalanishning eng tabiiy yo'lini buzardi.
 */
@Injectable({ providedIn: 'root' })
export class AiStore {
  private readonly ai = inject(AiService);
  private readonly router = inject(Router);

  private readonly openState = signal(false);
  private readonly entriesState = signal<readonly AiChatEntry[]>([]);
  private readonly conversationState = signal<string | null>(null);
  private readonly busyState = signal(false);
  private readonly conversationsState = signal<readonly AiConversation[]>([]);

  /** Ketayotgan so'rov — panel yopilganda yoki yangi suhbatda uziladi. */
  private inflight: AbortController | null = null;

  readonly isOpen = this.openState.asReadonly();
  readonly entries = this.entriesState.asReadonly();
  readonly busy = this.busyState.asReadonly();
  readonly conversations = this.conversationsState.asReadonly();
  readonly conversationId = this.conversationState.asReadonly();

  /**
   * Yozuvga aylantirilgan qoralamalar (`<yozuv>:<tool>` kalitlari).
   *
   * ⚠️ Kerak, chunki tugma BIR MARTA bosiladi: ikkinchi bosish ikkinchi hujjat
   * yaratardi va odam buni faqat ro'yxatda ko'rgan bo'lardi.
   */
  private readonly createdState = signal<ReadonlySet<string>>(new Set());

  readonly isEmpty = computed(() => this.entriesState().length === 0);

  /** Shu qoralama allaqachon yozuvga aylantirilganmi. */
  isCreated(entryIndex: number, toolIndex: number): boolean {
    return this.createdState().has(`${entryIndex}:${toolIndex}`);
  }

  toggle(): void {
    this.openState.update((open) => !open);
  }

  close(): void {
    this.openState.set(false);
  }

  /**
   * Yangi suhbat boshlaydi.
   *
   * ⚠️ Ketayotgan so'rov UZILADI: aks holda eski javob yangi suhbatning ustiga
   * tushib, foydalanuvchi so'ramagan savolga javob ko'rardi.
   */
  reset(): void {
    this.abort();
    this.entriesState.set([]);
    this.conversationState.set(null);
    this.createdState.set(new Set());
  }

  /** Suhbatlar ro'yxatini yangilaydi. */
  loadConversations(): void {
    this.ai.getConversations().subscribe((res) => {
      if (res.success && res.data) {
        this.conversationsState.set(res.data);
      }
    });
  }

  /** Saqlangan suhbatni ochadi. */
  openConversation(id: string): void {
    this.abort();

    this.ai.getConversation(id).subscribe((res) => {
      if (!res.success || !res.data) {
        return;
      }

      this.conversationState.set(id);
      this.entriesState.set(
        res.data.messages
          // Tool yozuvlari ALOHIDA qator sifatida ko'rsatilmaydi: ular javobning
          // qismi va tarixda ularning natijasi baribir saqlanmaydi (backend izohi).
          .filter((message) => message.role !== 3 && (message.text ?? '').length > 0)
          .map((message) => ({
            role: message.role === 1 ? ('user' as const) : ('assistant' as const),
            text: message.text ?? '',
            tools: [],
            pending: false,
          }))
      );
    });
  }

  /** Savol yuboradi va javobni oqim bo'yicha to'ldiradi. */
  async ask(text: string): Promise<void> {
    const question = text.trim();
    if (question.length === 0 || this.busyState()) {
      return;
    }

    this.abort();
    const controller = new AbortController();
    this.inflight = controller;
    this.busyState.set(true);

    this.push({ role: 'user', text: question, tools: [], pending: false });
    this.push({ role: 'assistant', text: '', tools: [], pending: true });

    try {
      const tools: AiToolOutput[] = [];

      for await (const event of this.ai.ask(question, {
        conversationId: this.conversationState(),
        pageContext: this.pageContext(),
        signal: controller.signal,
      })) {
        switch (event.type) {
          case 'tool_result':
            if (event.tool) {
              tools.push(event.tool);
              this.replaceLast({ role: 'assistant', text: '', tools: [...tools], pending: true });
            }
            break;

          case 'text':
            this.replaceLast({
              role: 'assistant',
              text: event.text ?? '',
              tools: [...tools],
              pending: false,
            });
            break;

          case 'done':
            this.conversationState.set(event.conversationId ?? null);
            break;

          case 'error':
            this.replaceLast({ role: 'error', text: event.text ?? '', tools: [], pending: false });
            break;
        }
      }
    } catch (error) {
      // Panelni yopish ham `AbortError` beradi — u nosozlik emas, javob ham kerak emas.
      if (!isAbort(error)) {
        this.replaceLast({ role: 'error', text: messageOf(error), tools: [], pending: false });
      }
    } finally {
      if (this.inflight === controller) {
        this.inflight = null;
        this.busyState.set(false);
      }
    }
  }

  /**
   * Qoralamani yozuvga aylantiradi — ODAM tugmasi (F10 §0.6).
   *
   * ⚠️ AI buni o'zi qila olmaydi: shunday tool umuman mavjud emas. Yozuv `Pending`
   * bo'lib yaratiladi, tasdiqlash esa hujjatlar ekranida qoladi — AI oqimi
   * zaxirani kamaytiradigan qadamni qisqartirmaydi.
   */
  async createFromDraft(entryIndex: number, toolIndex: number, tool: AiToolOutput): Promise<void> {
    const key = `${entryIndex}:${toolIndex}`;
    if (this.createdState().has(key)) {
      return;
    }

    const conversationId = this.conversationState();

    const request =
      tool.code === 'draft_transfer'
        ? this.ai.createTransfer(tool.data as AiTransferDraft, conversationId)
        : this.ai.createPayment(tool.data as AiPaymentDraft, conversationId);

    request.subscribe({
      next: (res) => {
        if (!res.success) {
          return;
        }

        this.createdState.update((created) => new Set([...created, key]));

        const number = (res.data as { number?: number } | null)?.number;
        this.push({
          role: 'assistant',
          text:
            tool.code === 'draft_transfer'
              ? `✅ #${number ?? '—'} — hujjat yaratildi (tasdiqlanmagan).`
              : '✅ To\'lov yozuvi kiritildi.',
          tools: [],
          pending: false,
        });
      },
      // Xato toasti interceptor zanjirida chiqadi; bu yerda takrorlanmaydi.
      error: () => undefined,
    });
  }

  /**
   * Joriy sahifa — system promptning o'zgaruvchan qismiga tushadi.
   *
   * ⚠️ Faqat YO'L uzatiladi, sahifadagi ma'lumot emas: kontekst modelga nima
   * so'ralayotganini tushunishga yordam beradi, lekin u ham «ma'lumot, ko'rsatma
   * emas» qoidasi ostida (prompt qoidasi 3).
   */
  private pageContext(): string {
    return this.router.url;
  }

  private push(entry: AiChatEntry): void {
    this.entriesState.update((entries) => [...entries, entry]);
  }

  private replaceLast(entry: AiChatEntry): void {
    this.entriesState.update((entries) => [...entries.slice(0, -1), entry]);
  }

  private abort(): void {
    this.inflight?.abort();
    this.inflight = null;
    this.busyState.set(false);
  }
}

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
