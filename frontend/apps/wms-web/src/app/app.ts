import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
  RouterOutlet,
} from '@angular/router';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ProgressBar } from 'primeng/progressbar';
import { Toast } from 'primeng/toast';
import { filter, map } from 'rxjs';
import { LoadingService } from '@agentics/http';

/**
 * Ildiz: toast, tasdiqlash dialogi va global progress chizig'i butun ilova uchun
 * shu yerda bir marta (eski `wms-ui/app.ts`).
 *
 * Progress IKKI manbadan: lazy chunk yuklanishi (navigatsiya) va HTTP so'rovlari
 * (paketning `loadingInterceptor` i sanaydi, fon so'rovlari `skipLoading` bilan).
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog, ProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
})
export class App {
  private readonly http = inject(LoadingService);

  private readonly navigating = toSignal(
    inject(Router).events.pipe(
      filter(
        (e) =>
          e instanceof NavigationStart ||
          e instanceof NavigationEnd ||
          e instanceof NavigationCancel ||
          e instanceof NavigationError
      ),
      map((e) => e instanceof NavigationStart)
    ),
    { initialValue: false }
  );

  readonly loading = computed(() => this.navigating() || this.http.isLoading());
}
