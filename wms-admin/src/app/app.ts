import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog],
  template: `
    <p-toast position="top-right" />
    <p-confirmdialog />
    <router-outlet />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App implements OnInit {
  private transloco = inject(TranslocoService);

  ngOnInit() {
    // Admin konsol doim yorug' rejimda (oddiy, tashqi holat kerak emas)
    document.documentElement.classList.remove('dark-mode');

    const saved = localStorage.getItem('adminLang');
    if (saved && ['uz', 'ru', 'en'].includes(saved)) this.transloco.setActiveLang(saved);
  }
}
