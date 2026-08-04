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
import { LoadingService } from '../../../core/services/loading.service';
import { environment } from '../../../../environments/environment';
import { blockedReasonKey } from '../../../core/models/subscription.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, InputText, Password, InputGroup, InputGroupAddon, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export default class LoginComponent {
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private bellService = inject(NotificationBellService);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private loadingService = inject(LoadingService);
  private transloco = inject(TranslocoService);

  phone = signal('');
  password = signal('');
  loading = signal(false);
  /** 402 — obuna to'xtatilgan yoki muddati o'tgan. Toast emas, ko'rinarli panel. */
  blockedKey = signal<string | null>(null);
  /** Backend o'z matnini yuborsa — standart tarjimadan ustun turadi. */
  blockedText = signal<string | null>(null);

  readonly supportPhone = environment.supportPhone;
  readonly supportEmail = environment.supportEmail;

  onPhoneInput(value: string) {
    this.phone.set(value.replace(/\D/g, ''));
  }

  login() {
    if (!this.phone() || !this.password()) {
      this.notify.warn(this.transloco.translate('auth.fillAllFields'));
      return;
    }

    if (this.phone().length !== 9) {
      this.notify.warn(this.transloco.translate('auth.phoneLength'));
      return;
    }

    this.blockedKey.set(null);
    this.blockedText.set(null);
    this.loading.set(true);
    this.loadingService.show();
    this.authService.login({
      phone: '+998' + this.phone(),
      password: this.password(),
      tenantSlug: environment.tenantSlug
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.tenantService.loadModules();
        this.bellService.startPolling();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.loadingService.hide();
        if (err.status === 402) {
          // Obuna bloki — sabab panelda qoladi, toast bilan yo'qolib ketmaydi
          this.blockedKey.set(blockedReasonKey(err.error?.code));
          this.blockedText.set(err.error?.blockedMessage ?? null);
          return;
        }
        this.notify.error(err.error?.message ?? this.transloco.translate('auth.loginFailed'));
      }
    });
  }
}
