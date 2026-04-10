import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  imports: [FormsModule, Button, InputText, Password, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export default class ProfileComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private notify = inject(NotificationService);

  fullName = signal('');
  phone = signal('');
  saving = signal(false);

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  changingPassword = signal(false);

  ngOnInit() {
    const user = this.authService.currentUser();
    if (user) {
      this.fullName.set(user.fullName);
      this.phone.set(user.phone);
    }
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
        this.notify.success('Password changed successfully');
      },
      error: () => {
        this.changingPassword.set(false);
        this.notify.error('Failed to change password');
      }
    });
  }
}
