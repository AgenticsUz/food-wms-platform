import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [AsyncPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  private translocoService = inject(TranslocoService);
  themeService = inject(ThemeService);
  private authService = inject(AuthService);

  currentUser = this.authService.currentUser;
  currentLang = this.translocoService.langChanges$;

  toggleTheme() {
    this.themeService.toggle();
  }

  switchLang(lang: string) {
    this.translocoService.setActiveLang(lang);
    localStorage.setItem('lang', lang);
  }
}
