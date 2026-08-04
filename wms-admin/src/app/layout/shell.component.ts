import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private auth = inject(AuthService);
  private notify = inject(NotificationService);

  user = this.auth.currentUser;

  nav = [
    { label: 'Dashboard', icon: 'pi pi-chart-bar', route: '/dashboard' },
    { label: 'Tenants', icon: 'pi pi-building', route: '/tenants' },
    { label: 'Leads', icon: 'pi pi-inbox', route: '/leads' },
    { label: 'Organizations', icon: 'pi pi-sitemap', route: '/organizations' },
    { label: 'Plans', icon: 'pi pi-tags', route: '/plans' }
  ];

  logout() {
    this.notify.confirmAction('Sign out of the admin console?', 'Sign out', () => this.auth.logout());
  }
}
