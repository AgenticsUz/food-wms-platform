import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../../../core/services/settings.service';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ModuleInfo } from '../../../core/models/settings.model';

@Component({
  selector: 'app-modules',
  standalone: true,
  imports: [FormsModule, ToggleSwitch, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './modules.component.html',
  styleUrl: './modules.component.scss'
})
export default class ModulesComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private notify = inject(NotificationService);

  modules = signal<ModuleInfo[]>([]);
  loading = signal(true);
  saving = signal(false);

  ngOnInit() {
    this.loadModules();
  }

  loadModules() {
    const user = this.authService.currentUser();
    if (!user) return;

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

  onToggle(module: ModuleInfo, isEnabled: boolean) {
    const user = this.authService.currentUser();
    if (!user) return;

    // Optimistically update UI
    this.modules.update(mods =>
      mods.map(m => m.moduleId === module.moduleId ? { ...m, isEnabled } : m)
    );

    this.saving.set(true);
    this.settingsService.toggleModules(user.tenantId, [{ moduleId: module.moduleId, isEnabled }]).subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success(`${module.moduleName} ${isEnabled ? 'enabled' : 'disabled'}`);
        this.tenantService.loadModules();
      },
      error: () => {
        // Revert on error
        this.modules.update(mods =>
          mods.map(m => m.moduleId === module.moduleId ? { ...m, isEnabled: !isEnabled } : m)
        );
        this.saving.set(false);
        this.notify.error('Failed to update module');
      }
    });
  }
}
