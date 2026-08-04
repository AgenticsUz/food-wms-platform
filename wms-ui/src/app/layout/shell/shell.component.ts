import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { SubscriptionBannerComponent } from '../../shared/components/subscription-banner/subscription-banner.component';
import { SubscriptionService } from '../../core/services/subscription.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, SubscriptionBannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);

  sidebarCollapsed = signal(false);
  mobileMenuOpen = signal(false);

  ngOnInit() {
    // Shell faqat autentifikatsiyalangan asosiy ilovada quriladi — login'dan keyin ham,
    // mavjud token bilan ochilganda ham obuna holati shu yerdan bir marta yuklanadi.
    this.subscriptionService.load();
  }

  onSidebarToggle(collapsed: boolean) {
    this.sidebarCollapsed.set(collapsed);
  }

  toggleMobileMenu() {
    this.mobileMenuOpen.update(v => !v);
  }

  closeMobileMenu() {
    this.mobileMenuOpen.set(false);
  }
}
