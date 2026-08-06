import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { LeadService } from '../../../core/services/lead.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { PublicInfoService } from '../../../core/services/public-info.service';

/**
 * Avval bu sahifa tenant yaratardi. Yangi modelda hisobni faqat platforma egasi
 * ochadi, shuning uchun sahifa demo so'rovini qoldiradi — slug, parol va modul
 * tanlash umuman yo'q.
 */
@Component({
  selector: 'app-request-demo',
  standalone: true,
  imports: [FormsModule, RouterLink, InputText, Textarea, InputGroup, InputGroupAddon, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss'
})
export default class RequestDemoComponent {
  private leads = inject(LeadService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  companyName = signal('');
  contactName = signal('');
  phone = signal('');
  email = signal('');
  note = signal('');
  loading = signal(false);
  /** Yuborilgach forma o'rniga tasdiq ekrani qoladi — qayta yuborish yo'q. */
  sent = signal(false);

  // Login sahifasi bilan bir xil manba — server sozlamasi, `environment` zaxira.
  private publicInfo = inject(PublicInfoService);
  readonly supportPhone = this.publicInfo.supportPhone;

  constructor() { this.publicInfo.load(); }

  onPhoneInput(value: string) {
    this.phone.set(value.replace(/\D/g, ''));
  }

  submit() {
    if (!this.companyName().trim() || !this.contactName().trim() || !this.phone()) {
      this.notify.warn(this.transloco.translate('auth.fillAllFields'));
      return;
    }
    if (this.phone().length !== 9) {
      this.notify.warn(this.transloco.translate('auth.phoneLength'));
      return;
    }

    this.loading.set(true);
    this.leads.requestDemo({
      companyName: this.companyName().trim(),
      contactName: this.contactName().trim(),
      phone: '+998' + this.phone(),
      email: this.email().trim() || null,
      note: this.note().trim() || null
    }).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 429) {
          this.notify.warn(this.transloco.translate('auth.demoTooMany'));
          return;
        }
        this.notify.error(err.error?.message ?? this.transloco.translate('auth.demoFailed'));
      }
    });
  }
}
