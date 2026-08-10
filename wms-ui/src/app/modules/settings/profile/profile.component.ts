import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../../../core/services/settings.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ChangePasswordDto } from '../../../core/models/settings.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, Button, InputText, Password, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export default class ProfileComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private auth = this.authService;
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  /**
   * Foydalanuvchi bu sahifaga o'z xohishi bilan kelmadi — `mustChangePasswordGuard`
   * uni shu yerga yubordi. Shunda sabab ko'rsatiladi va parol o'zgargach bosh
   * sahifaga qaytariladi.
   */
  wasForced = signal(false);

  fullName = signal('');
  phone = signal('');
  saving = signal(false);

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  changingPassword = signal(false);

  telegramChatId = signal('');
  savingTelegram = signal(false);

  ngOnInit() {
    const user = this.authService.currentUser();
    if (user) {
      this.fullName.set(user.fullName);
      this.phone.set(user.phone);
      this.telegramChatId.set(user.telegramChatId ?? '');
    }
    this.wasForced.set(
      this.route.snapshot.queryParamMap.get('mustChangePassword') === '1' ||
      (user?.mustChangePassword ?? false));
  }

  saveTelegram() {
    this.savingTelegram.set(true);
    const chatId = this.telegramChatId().trim() || null;
    this.settingsService.setTelegram(chatId).subscribe({
      next: () => {
        this.savingTelegram.set(false);
        this.notify.success('Telegram updated');
        const user = this.authService.currentUser();
        if (user) {
          this.authService.currentUser.set({ ...user, telegramChatId: chatId });
          localStorage.setItem('currentUser', JSON.stringify(this.authService.currentUser()));
        }
      },
      error: () => this.savingTelegram.set(false)
    });
  }

  saveProfile() {
    if (!this.fullName().trim() || !this.phone().trim()) {
      this.notify.warn('Full name and phone are required');
      return;
    }

    this.saving.set(true);
    this.settingsService.updateProfile({
      fullName: this.fullName().trim(),
      phone: this.phone().trim()
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success('Profile updated');
        // Update local user state
        const user = this.authService.currentUser();
        if (user) {
          this.authService.currentUser.set({
            ...user,
            fullName: this.fullName().trim(),
            phone: this.phone().trim()
          });
          localStorage.setItem('currentUser', JSON.stringify(this.authService.currentUser()));
        }
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to update profile');
      }
    });
  }

  changePassword() {
    if (!this.currentPassword().trim()) {
      this.notify.warn('Current password is required');
      return;
    }
    if (!this.newPassword().trim()) {
      this.notify.warn('New password is required');
      return;
    }
    if (this.newPassword() !== this.confirmPassword()) {
      this.notify.warn('Passwords do not match');
      return;
    }
    if (this.newPassword().length < 6) {
      this.notify.warn('Password must be at least 6 characters');
      return;
    }

    this.changingPassword.set(true);
    const dto: ChangePasswordDto = {
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword()
    };

    this.settingsService.changePassword(dto).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        this.auth.clearMustChangePassword();
        // Parol o'zgarishi `SecurityStamp` ni aylantiradi — bu ataylab, boshqa
        // sessiyalarni o'ldirish uchun. Ammo joriy token ham o'sha stamp bilan
        // yozilgan, ya'ni u ham o'ladi. Kutib turilsa foydalanuvchi keyingi so'rovda
        // 401 olib, sababsiz login sahifasiga uloqtiriladi. Shuning uchun sababni
        // aytib, o'zimiz chiqaramiz.
        this.notify.success(this.transloco.translate('settings.reset.changedSignOut'));
        this.auth.logout();
      },
      // Xato toastini interceptor chiqaradi — ikkinchisini qo'shmaymiz.
      error: () => this.changingPassword.set(false)
    });
  }
}
