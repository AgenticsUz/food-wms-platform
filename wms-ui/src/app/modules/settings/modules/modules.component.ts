import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../../../core/services/settings.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ModuleInfo } from '../../../core/models/settings.model';

/**
 * Faqat-ko'rish sahifa. Modul to'plami obuna plani bilan belgilanadi va
 * faqat SuperAdmin o'zgartira oladi (backend `PUT tenants/{id}/modules` ni
 * tenant admin uchun 403 bilan rad etadi).
 */
@Component({
  selector: 'app-modules',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './modules.component.html',
  styleUrl: './modules.component.scss'
})
export default class ModulesComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private notify = inject(NotificationService);

  modules = signal<ModuleInfo[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadModules();
  }

  loadModules() {
    const user = this.authService.currentUser();
    if (!user) { this.loading.set(false); return; }

    this.loading.set(true);
    this.settingsService.getModules(user.tenantId).subscribe({
      next: (res) => {
        this.modules.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load modules');
      }
    });
  }
}
