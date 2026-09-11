import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, model, signal } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import type { Observable } from 'rxjs';

import type { ApiResponse } from '../../../core/api/api-response.model';
import { NotificationService } from '../../../core/notify/notification.service';
import type { TelegramLinkToken, TelegramSubjectLink } from '../../../features/settings/settings.model';

/** Haydovchi/kontragent kartasining Telegram API'si — dialog manbaga befarq. */
export interface TelegramLinkApi {
  get(): Observable<ApiResponse<TelegramSubjectLink>>;
  create(): Observable<ApiResponse<TelegramLinkToken>>;
  unlink(): Observable<ApiResponse<void>>;
}

/**
 * Foydalanuvchi bo'lmagan odamni (haydovchi — TG12, kontragent — TG13) botga ulash: menejer
 * havola yaratadi va uni o'zi yuboradi (telefon, qog'oz). Havola 24 soat yashaydi, bir marta ishlaydi.
 */
@Component({
  selector: 'app-telegram-link-dialog',
  imports: [Button, Dialog, InputText, TranslocoDirective, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-container *transloco="let t">
      <p-dialog
        [visible]="visible()"
        (visibleChange)="visible.set($event)"
        [header]="t('telegramLink.title', { name: name() })"
        [modal]="true"
        [style]="{ width: '460px' }"
        [draggable]="false"
      >
        @if (status(); as s) {
          @if (!s.enabled) {
            <p class="tg-hint">{{ t('settings.telegramDisabled') }}</p>
          } @else if (s.linked) {
            <p class="tg-linked">
              <i class="pi pi-check-circle"></i>
              <strong>{{ t('settings.telegramLinked') }}</strong>
              @if (s.username) { · &#64;{{ s.username }} }
              @if (s.linkedAt) { · {{ s.linkedAt | date: 'dd.MM.yyyy HH:mm' }} }
            </p>
          } @else {
            <p class="tg-hint">{{ t('telegramLink.hint', { name: name() }) }}</p>
            @if (url(); as u) {
              <div class="tg-url">
                <input pInputText [value]="u" readonly class="w-full" (focus)="selectAll($event)" />
                <p-button icon="pi pi-copy" [label]="t('telegramLink.copy')" [outlined]="true" (onClick)="copy(u)" />
              </div>
              <p class="tg-hint small">{{ t('telegramLink.expires') }}</p>
            }
          }
        } @else {
          <p class="tg-hint">…</p>
        }
        <ng-template #footer>
          <p-button [label]="t('common.close')" [text]="true" (onClick)="visible.set(false)" />
          @if (status()?.enabled) {
            @if (status()?.linked) {
              <p-button [label]="t('settings.telegramUnlink')" icon="pi pi-times" severity="secondary" [outlined]="true" [loading]="busy()" (onClick)="unlink()" />
            } @else {
              <p-button [label]="t('telegramLink.create')" icon="pi pi-link" [loading]="busy()" (onClick)="create()" />
            }
          }
        </ng-template>
      </p-dialog>
    </ng-container>
  `,
  styles: `
    .tg-hint { margin: 0 0 12px; font-size: 13px; color: var(--text-muted); }
    .tg-hint.small { font-size: 12px; margin-top: 8px; }
    .tg-linked { display: flex; align-items: center; gap: 6px; margin: 0; color: var(--text-primary); }
    .tg-url { display: flex; gap: 8px; align-items: center; }
  `,
})
export class TelegramLinkDialogComponent {
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly api = input.required<TelegramLinkApi>();
  readonly name = input('');
  readonly visible = model(false);

  readonly status = signal<TelegramSubjectLink | null>(null);
  readonly url = signal<string | null>(null);
  readonly busy = signal(false);

  constructor() {
    // Har ochilishda holat qayta o'qiladi — boshqa oynada ulangan bo'lishi mumkin.
    effect(() => {
      if (this.visible()) this.load();
    });
  }

  create(): void {
    this.busy.set(true);
    this.api().create().subscribe({
      next: (res) => {
        this.busy.set(false);
        if (res.success && res.data) this.url.set(res.data.url);
      },
      error: () => this.busy.set(false),
    });
  }

  unlink(): void {
    this.busy.set(true);
    this.api().unlink().subscribe({
      next: () => {
        this.busy.set(false);
        this.notify.success(this.language.translate('settings.telegramDisconnected'));
        this.load();
      },
      error: () => this.busy.set(false),
    });
  }

  copy(url: string): void {
    void navigator.clipboard?.writeText(url).then(() => this.notify.success(this.language.translate('telegramLink.copied')));
  }

  selectAll(event: FocusEvent): void {
    (event.target as HTMLInputElement | null)?.select();
  }

  private load(): void {
    this.status.set(null);
    this.url.set(null);
    this.api().get().subscribe((res) => {
      if (res.success && res.data) this.status.set(res.data);
    });
  }
}
