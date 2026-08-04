import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

const LANG_KEY = 'adminLang';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, TranslocoDirective, InputText, Password, InputGroup, InputGroupAddon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export default class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  phone = signal('');
  password = signal('');
  loading = signal(false);

  // Til kirishdan oldin ham tanlanadi — konsolga kirmagan odam ham o'z tilini ko'rsin
  readonly langs = [
    { code: 'uz', label: "O'z" },
    { code: 'ru', label: 'Ру' },
    { code: 'en', label: 'En' }
  ];
  activeLang = signal(this.transloco.getActiveLang());

  switchLang(code: string) {
    this.transloco.setActiveLang(code);
    localStorage.setItem(LANG_KEY, code);
    this.activeLang.set(code);
  }

  onPhoneInput(value: string) { this.phone.set(value.replace(/\D/g, '')); }

  login() {
    if (!this.phone() || !this.password()) {
      this.notify.warn(this.transloco.translate('login.fillAllFields'));
      return;
    }
    if (this.phone().length !== 9) {
      this.notify.warn(this.transloco.translate('login.phoneLength'));
      return;
    }

    this.loading.set(true);
    this.auth.login({ phone: '+998' + this.phone(), password: this.password(), tenantSlug: 'admin' }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.router.navigate(['/dashboard']);
        } else {
          this.notify.error(res.message ?? this.transloco.translate('login.failed'));
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.notify.error(err.error?.message ?? this.transloco.translate('login.failed'));
      }
    });
  }
}
