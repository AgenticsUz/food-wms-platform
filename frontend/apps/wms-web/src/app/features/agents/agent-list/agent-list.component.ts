import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Tooltip } from 'primeng/tooltip';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Agent, AgentSaveDto } from '../agent.model';
import { AgentService } from '../agent.service';

interface AgentForm {
  readonly id: string | null;
  readonly name: string;
  readonly phone: string | null;
  readonly commissionPercent: number;
  readonly isActive: boolean;
}

/**
 * ⚠️ «Portal kirishi», portal telefoni va paroli formadan olib tashlandi: agent
 * portali F6 da o'chdi (D8) — Identity'dan tashqari ikkinchi login bo'lmaydi.
 */
@Component({
  selector: 'app-agent-list',
  imports: [
    DecimalPipe, FormsModule, TableModule, Button, InputText, InputNumber, Dialog, ToggleSwitch, Tooltip,
    PageHeaderComponent, StatusBadgeComponent, PhoneInputComponent, HasPermissionDirective, TranslocoDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './agent-list.component.html',
  styleUrl: './agent-list.component.scss',
})
export default class AgentListComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  readonly agents = signal<Agent[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);

  readonly form = signal<AgentForm>(this.emptyForm());

  ngOnInit(): void {
    this.loadAgents();
  }

  loadAgents(): void {
    this.loading.set(true);
    this.agentService.getAgents().subscribe({
      next: (res) => {
        this.agents.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(this.emptyForm());
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(agent: Agent): void {
    this.form.set({
      id: agent.id,
      name: agent.name,
      phone: agent.phone,
      commissionPercent: agent.commissionPercent,
      isActive: agent.isActive,
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  openDetail(agent: Agent): void {
    void this.router.navigate(['/agents', agent.id]);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Name is required');
      return;
    }
    if (f.commissionPercent < 0 || f.commissionPercent > 100) {
      this.notify.warn('Commission percent must be between 0 and 100');
      return;
    }

    const dto: AgentSaveDto = {
      name: f.name.trim(),
      phone: f.phone,
      commissionPercent: f.commissionPercent,
      isActive: f.isActive,
    };
    const editing = this.editing() && f.id !== null;
    const request = editing && f.id !== null ? this.agentService.updateAgent(f.id, dto) : this.agentService.createAgent(dto);

    this.saving.set(true);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(editing ? 'Agent updated' : 'Agent created');
        this.loadAgents();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteAgent(agent: Agent): void {
    this.notify.confirmDelete(`Delete "${agent.name}"?`, () => {
      this.agentService.deleteAgent(agent.id).subscribe({
        next: () => {
          this.notify.success('Agent deleted');
          this.loadAgents();
        },
        error: () => undefined,
      });
    });
  }

  updateForm<K extends keyof AgentForm>(field: K, value: AgentForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  private emptyForm(): AgentForm {
    return { id: null, name: '', phone: null, commissionPercent: 0, isActive: true };
  }
}
