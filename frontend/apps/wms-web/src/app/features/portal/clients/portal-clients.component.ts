import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';

import type { PortalAgentClient } from '../portal.model';
import { PortalService } from '../portal.service';

/** Agent kabineti: o'z mijozlari va ularning qarzi (o'zga agentniki ko'rinmaydi). */
@Component({
  selector: 'app-portal-clients',
  imports: [DecimalPipe, TranslocoDirective, TableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-clients.component.html',
  styleUrl: './portal-clients.component.scss',
})
export default class PortalClientsComponent implements OnInit {
  private readonly portal = inject(PortalService);

  readonly clients = signal<PortalAgentClient[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.portal.getAgentClients().subscribe({
      next: (res) => {
        this.clients.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
