import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { NotificationBellService } from '../../../core/services/notification-bell.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, InputText, Password, InputGroup, InputGroupAddon, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss'
})
export default class RegisterComponent {
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private bellService = inject(NotificationBellService);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  tenantName = signal('');
  slug = signal('');
  slugEdited = signal(false);
  fullName = signal('');
  phone = signal('');
  password = signal('');
  loading = signal(false);

  onTenantNameInput(value: string) {
    this.tenantName.set(value);
    // Slug avtomatik taklif qilinadi (foydalanuvchi qo'lda o'zgartirmagan bo'lsa)
    if (!this.slugEdited()) this.slug.set(this.slugify(value));
  }

  onSlugInput(value: string) {
    this.slugEdited.set(true);
    this.slug.set(this.slugify(value));
  }

  onPhoneInput(value: string) {
    this.phone.set(value.replace(/\D/g, ''));
  }

  private slugify(v: string): string {
    return v.toLowerCase().trim()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/[\s-]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  register() {
    if (!this.tenantName().trim() || !this.slug().trim() || !this.fullName().trim()
      || !this.phone() || !this.password()) {
      this.notify.warn(this.transloco.translate('auth.fillAllFields'));
      return;
    }
    if (this.phone().length !== 9) {
      this.notify.warn(this.transloco.translate('auth.phoneLength'));
      return;
    }
    if (this.password().length < 6) {
      this.notify.warn(this.transloco.translate('auth.passwordTooShort'));
      return;
    }

    this.loading.set(true);
    this.authService.register({
      tenantName: this.tenantName().trim(),
      slug: this.slug().trim(),
      fullName: this.fullName().trim(),
      phone: '+998' + this.phone(),
      password: this.password()
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.tenantService.loadModules();
          this.bellService.startPolling();
          this.notify.success(this.transloco.translate('auth.registerSuccess'));
          this.router.navigate(['/dashboard']);
        } else {
          this.notify.error(res.message ?? this.transloco.translate('auth.registerFailed'));
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.notify.error(err.error?.message ?? this.transloco.translate('auth.registerFailed'));
      }
    });
  }
}
