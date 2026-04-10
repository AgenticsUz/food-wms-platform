import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, InputText, Password, Button, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export default class LoginComponent {
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private notify = inject(NotificationService);

  phone = signal('');
  password = signal('');
  tenantSlug = signal('');
  loading = signal(false);

  login() {
    if (!this.phone() || !this.password() || !this.tenantSlug()) {
      this.notify.warn('Please fill in all fields');
      return;
    }

    this.loading.set(true);
    this.authService.login({
      phone: this.phone(),
      password: this.password(),
      tenantSlug: this.tenantSlug()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.tenantService.loadModules();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.notify.error(err.error?.message ?? 'Login failed. Please check your credentials.');
      }
    });
  }
}
