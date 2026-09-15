import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { AuthService } from '@agentics/auth';
import { ConfigService } from '@agentics/config';
import { LanguageService } from '@agentics/i18n';
import { TenantStore } from '@agentics/tenant';

import { ApiService } from '../../core/api/api.service';
import { AiService } from './ai.service';
import type { AiStreamEvent } from './ai.model';

/**
 * SSE oqimini o'qish.
 *
 * ⚠️ Eng muhim da'vo — <b>bo'lak chegarasi hodisa chegarasi EMAS</b>: bitta
 * `data:` qatori ikkiga bo'linib kelishi mumkin va buferlanmasa hodisa
 * yo'qolardi. Bu tarmoqqa bog'liq va lokal dev'da deyarli hech qachon
 * ko'rinmaydi — shuning uchun test bilan qo'riqlanadi.
 */
describe('AiService.ask', () => {
  let service: AiService;

  const stream = (chunks: readonly string[]): ReadableStream<Uint8Array> => {
    const encoder = new TextEncoder();
    return new ReadableStream({
      start(controller) {
        for (const chunk of chunks) {
          controller.enqueue(encoder.encode(chunk));
        }
        controller.close();
      },
    });
  };

  const mockFetch = (body: ReadableStream<Uint8Array> | null, ok = true, payload?: unknown): void => {
    globalThis.fetch = (() =>
      Promise.resolve({
        ok,
        status: ok ? 200 : 403,
        body,
        json: () => Promise.resolve(payload),
      } as Response)) as typeof fetch;
  };

  const collect = async (): Promise<AiStreamEvent[]> => {
    const events: AiStreamEvent[] = [];
    for await (const event of service.ask('Qoldiq qancha?')) {
      events.push(event);
    }
    return events;
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),

        // `ApiService` faqat suhbat ro'yxati uchun kerak; oqim testida u
        // chaqirilmaydi, lekin uning bog'liqliklari (toast) TestBed'ni yiqitardi.
        { provide: ApiService, useValue: {} },
        { provide: AuthService, useValue: { accessToken: signal('token-1') } },
        { provide: ConfigService, useValue: { apiUrl: signal('/api') } },
        { provide: TenantStore, useValue: { tenantId: signal('tenant-1') } },
        { provide: LanguageService, useValue: { language: signal('uz-Latn') } },
      ],
    });

    service = TestBed.inject(AiService);
  });

  it('bir nechta hodisani tartibda qaytaradi', async () => {
    mockFetch(
      stream([
        'data: {"type":"tool_result","tool":{"code":"stock_query","text":"34","data":null}}\n\n',
        'data: {"type":"text","text":"Omborda 34 dona"}\n\n',
        'data: {"type":"done","conversationId":"c-1"}\n\n',
      ])
    );

    const events = await collect();

    expect(events.map((e) => e.type)).toEqual(['tool_result', 'text', 'done']);
    expect(events[0].tool?.code).toBe('stock_query');
    expect(events[2].conversationId).toBe('c-1');
  });

  it('ikkiga bo\'lingan hodisa YO\'QOLMAYDI', async () => {
    // Tarmoq bo'lagi o'rtasidan uzilgan: birinchi bo'lakda hodisa to'liq emas.
    mockFetch(stream(['data: {"type":"text","te', 'xt":"Javob"}\n\ndata: {"type":"done"}\n\n']));

    const events = await collect();

    expect(events.map((e) => e.type)).toEqual(['text', 'done']);
    expect(events[0].text).toBe('Javob');
  });

  it('buzuq hodisa oqimni TO\'XTATMAYDI', async () => {
    mockFetch(stream(['data: {buzuq\n\n', 'data: {"type":"text","text":"Javob"}\n\n']));

    const events = await collect();

    expect(events.map((e) => e.type)).toEqual(['text']);
  });

  it('rad javobi server matni bilan tashlanadi', async () => {
    // AI o'chiq — javob SSE emas, odatdagi JSON (birinchi hodisadan oldin).
    mockFetch(null, false, { message: 'AI yordamchisi yoqilmagan.' });

    await expect(collect()).rejects.toThrow('AI yordamchisi yoqilmagan.');
  });
});
