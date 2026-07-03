import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, InputText, Password, InputGroup, InputGroupAddon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export default class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private notify = inject(NotificationService);

  phone = signal('');
  password = signal('');
  loading = signal(false);

  onPhoneInput(value: string) { this.phone.set(value.replace(/\D/g, '')); }

  login() {
    if (!this.phone() || !this.password()) { this.notify.warn('Please fill in all fields'); return; }
    if (this.phone().length !== 9) { this.notify.warn('Phone number must be 9 digits'); return; }

    this.loading.set(true);
    this.auth.login({ phone: '+998' + this.phone(), password: this.password(), tenantSlug: 'admin' }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.router.navigate(['/dashboard']);
        } else {
          this.notify.error(res.message ?? 'Login failed');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.notify.error(err.error?.message ?? 'Login failed. Check your credentials.');
      }
    });
  }
}
