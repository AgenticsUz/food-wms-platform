import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../services/notification.service';

const DISMISS_KEY = 'upgradeBannerDismissedAt';
const DISMISS_DAYS = 14;

interface UpgradeInterest {
  submitted: boolean;
  phone?: string | null;
  createdAt?: string | null;
}

/**
 * Portal foydalanuvchisi — A ning ta'minotchisi yoki xaridori — tizimni allaqachon
 * ko'rgan tayyor lead. Unga to'liq versiyani taklif qilamiz.
 *
 * Bu banner **faqat portal** ichida ishlatiladi. A ning o'z xodimlariga
 * ko'rsatilmaydi: portal foydalanuvchisi A ning mijozi, agressiv reklama A ga zarar.
 */
@Component({
  selector: 'app-upgrade-banner',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoDirective, Dialog, Button, InputText, Textarea],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './upgrade-banner.component.html',
  styleUrl: './upgrade-banner.component.scss'
})
export class UpgradeBannerComponent implements OnInit {
  private api = inject(ApiService);
  private notify = inject(NotificationService);

  /** Tokendan olingan telefon — formada oldindan to'ldiriladi, tahrirlanadi. */
  defaultPhone = input<string | null>(null);

  /**
   * Qaysi portal ichida turibmiz. Bu faqat manzil emas — `auth.interceptor`
   * tokenni URL bo'yicha tanlaydi, ya'ni agent portalidan `portal/...` ga
   * murojaat qilinsa noto'g'ri token ketadi.
   */
  portalBase = input<'portal' | 'agent-portal'>('portal');

  private dismissedAt = signal<string | null>(localStorage.getItem(DISMISS_KEY));
  submitted = signal(false);
  loaded = signal(false);

  dialogVisible = signal(false);
  phone = signal('');
  note = signal('');
  saving = signal(false);

  visible = computed(() => {
    if (!this.loaded()) return false;
    if (this.submitted()) return true;   // "so'rov qabul qilindi" holati ham ko'rsatiladi
    const at = this.dismissedAt();
    if (!at) return true;
    const days = (Date.now() - new Date(at).getTime()) / 86_400_000;
    return days >= DISMISS_DAYS;
  });

  ngOnInit() {
    // Holat serverdan olinadi — sahifa yangilanganda ham saqlanadi
    this.api.get<UpgradeInterest>(`${this.portalBase()}/upgrade-interest`).subscribe({
      next: (res) => {
        if (res.success && res.data?.submitted) this.submitted.set(true);
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true)
    });
  }

  open() {
    this.phone.set(this.defaultPhone() ?? '');
    this.note.set('');
    this.dialogVisible.set(true);
  }

  submit() {
    if (!this.phone().trim()) return;
    this.saving.set(true);
    this.api.post<void>(`${this.portalBase()}/upgrade-interest`, {
      phone: this.phone().trim(),
      note: this.note().trim() || null
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.submitted.set(true);
      },
      error: () => this.saving.set(false)
    });
  }

  dismiss() {
    const now = new Date().toISOString();
    localStorage.setItem(DISMISS_KEY, now);
    this.dismissedAt.set(now);
  }
}
