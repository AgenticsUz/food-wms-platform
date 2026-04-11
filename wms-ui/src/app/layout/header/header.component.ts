import { Component, inject, computed, OnInit, ChangeDetectionStrategy, output } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';
import { CurrencyService } from '../../core/services/currency.service';
import { NotificationBellComponent } from '../../shared/components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [AsyncPipe, DecimalPipe, NotificationBellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit {
  private translocoService = inject(TranslocoService);
  themeService = inject(ThemeService);
  private authService = inject(AuthService);
  currencyService = inject(CurrencyService);

  currentUser = this.authService.currentUser;
  currentLang = this.translocoService.langChanges$;
  menuToggled = output<void>();

  mainRates = computed(() => {
    const rates = this.currencyService.rates();
    return ['USD', 'EUR', 'RUB']
      .map(code => ({ code, rate: rates[code] ?? 0 }))
      .filter(r => r.rate > 0);
  });

  ngOnInit() {
    this.currencyService.loadRates();
  }

  toggleTheme() {
    this.themeService.toggle();
  }

  switchLang(lang: string) {
    this.translocoService.setActiveLang(lang);
    localStorage.setItem('lang', lang);
  }

  toggleMobileMenu() {
    this.menuToggled.emit();
  }
}
