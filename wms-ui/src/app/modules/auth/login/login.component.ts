import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { NotificationBellService } from '../../../core/services/notification-bell.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { LoadingService } from '../../../core/services/loading.service';
import { environment } from '../../../../environments/environment';

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

  phone = signal('');
  password = signal('');
  loading = signal(false);

  onPhoneInput(value: string) {
    this.phone.set(value.replace(/\D/g, ''));
  }

  login() {
    if (!this.phone() || !this.password()) {
      this.notify.warn('Please fill in all fields');
      return;
    }

    if (this.phone().length !== 9) {
      this.notify.warn('Telefon raqam 9 ta raqamdan iborat bo\'lishi kerak');
      return;
    }

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
        this.notify.error(err.error?.message ?? 'Login failed. Please check your credentials.');
      }
    });
  }
}
