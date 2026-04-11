import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ProgressBar } from 'primeng/progressbar';
import { ThemeService } from './core/services/theme.service';
import { TranslocoService } from '@jsverse/transloco';
import { LoadingService } from './core/services/loading.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog, ProgressBar],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private themeService = inject(ThemeService);
  private transloco = inject(TranslocoService);
  private router = inject(Router);
  loadingService = inject(LoadingService);

  ngOnInit() {
    this.themeService.init();
    const savedLang = localStorage.getItem('lang');
    if (savedLang && ['uz', 'ru', 'en'].includes(savedLang)) {
      this.transloco.setActiveLang(savedLang);
    }

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.loadingService.show();
      }
      if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
        this.loadingService.hide();
      }
    });
  }
}
