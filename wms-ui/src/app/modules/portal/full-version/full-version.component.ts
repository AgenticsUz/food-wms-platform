import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

/** Portal ichidagi tanishtiruv sahifasi — banner shunga havola qiladi. */
@Component({
  selector: 'app-full-version',
  standalone: true,
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './full-version.component.html',
  styleUrl: './full-version.component.scss'
})
export default class FullVersionComponent {
  readonly items = [
    { icon: 'pi pi-box', key: 'warehouse' },
    { icon: 'pi pi-cog', key: 'production' },
    { icon: 'pi pi-arrow-right-arrow-left', key: 'transfers' },
    { icon: 'pi pi-wallet', key: 'finance' },
    { icon: 'pi pi-chart-line', key: 'kpi' },
    { icon: 'pi pi-truck', key: 'delivery' }
  ];
}
