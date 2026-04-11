import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { PortalService } from '../../../core/services/portal.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-portal-login',
  standalone: true,
  imports: [FormsModule, InputText, Password, Button, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-login.component.html',
  styleUrl: './portal-login.component.scss'
})
export default class PortalLoginComponent {
  private portalService = inject(PortalService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  phone = signal('');
  password = signal('');
  loading = signal(false);

  login() {
    const phoneVal = this.phone().trim();
    const passVal = this.password().trim();

    if (!phoneVal || !passVal) {
      this.notify.warn('Please enter phone and password');
      return;
    }

    this.loading.set(true);
    this.portalService.login({ phone: phoneVal, password: passVal }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.notify.success('Welcome to WMS Portal');
          this.router.navigate(['/portal/dashboard']);
        } else {
          this.notify.error(res.message ?? 'Login failed');
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.notify.error(err?.error?.message ?? 'Invalid credentials');
        this.loading.set(false);
      }
    });
  }
}
