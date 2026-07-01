import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Password } from 'primeng/password';
import { Tooltip } from 'primeng/tooltip';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { AgentService } from '../../../core/services/agent.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Agent, AgentCreateDto } from '../../../core/models/agent.model';

@Component({
  selector: 'app-agent-list',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule, TableModule, Button, InputText, InputNumber, Dialog,
    ToggleSwitch, Password, Tooltip,
    PageHeaderComponent, StatusBadgeComponent, PhoneInputComponent,
    HasPermissionDirective, TranslocoDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './agent-list.component.html',
  styleUrl: './agent-list.component.scss'
})
export default class AgentListComponent implements OnInit {
  private agentService = inject(AgentService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  agents = signal<Agent[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<AgentCreateDto & { id?: number }>({
    name: '',
    phone: null,
    commissionPercent: 0,
    isActive: true,
    portalEnabled: false,
    portalPhone: null,
    portalPassword: ''
  });

  ngOnInit() {
    this.loadAgents();
  }

  loadAgents() {
    this.loading.set(true);
    this.agentService.getAgents().subscribe({
      next: (res) => {
        this.agents.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load agents');
      }
    });
  }

  openNew() {
    this.form.set({
      name: '',
      phone: null,
      commissionPercent: 0,
      isActive: true,
      portalEnabled: false,
      portalPhone: null,
      portalPassword: ''
    });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(agent: Agent) {
    this.form.set({
      id: agent.id,
      name: agent.name,
      phone: agent.phone,
      commissionPercent: agent.commissionPercent,
      isActive: agent.isActive,
      portalEnabled: agent.portalEnabled,
      portalPhone: agent.portalPhone,
      portalPassword: ''
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  openDetail(agent: Agent) {
    this.router.navigate(['/agents', agent.id]);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Name is required');
      return;
    }
    if (f.commissionPercent < 0 || f.commissionPercent > 100) {
      this.notify.warn('Commission percent must be between 0 and 100');
      return;
    }
    if (f.portalEnabled && !f.portalPhone) {
      this.notify.warn('Portal phone is required when portal is enabled');
      return;
    }
    if (f.portalEnabled && !this.editing() && !(f.portalPassword ?? '').trim()) {
      this.notify.warn('Portal password is required for new portal access');
      return;
    }

    const dto: AgentCreateDto = {
      name: f.name.trim(),
      phone: f.phone,
      commissionPercent: f.commissionPercent,
      isActive: f.isActive,
      portalEnabled: f.portalEnabled,
      portalPhone: f.portalEnabled ? f.portalPhone : null,
      portalPassword: (f.portalPassword ?? '').trim() ? f.portalPassword : null
    };

    this.saving.set(true);

    if (this.editing()) {
      this.agentService.updateAgent(f.id!, dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success('Agent updated');
          this.loadAgents();
        },
        error: () => {
          this.saving.set(false);
          this.notify.error('Failed to update agent');
        }
      });
    } else {
      this.agentService.createAgent(dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success('Agent created');
          this.loadAgents();
        },
        error: () => {
          this.saving.set(false);
          this.notify.error('Failed to create agent');
        }
      });
    }
  }

  deleteAgent(agent: Agent) {
    this.notify.confirmDelete(`Delete "${agent.name}"?`, () => {
      this.agentService.deleteAgent(agent.id).subscribe({
        next: () => {
          this.notify.success('Agent deleted');
          this.loadAgents();
        },
        error: () => this.notify.error('Failed to delete agent')
      });
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
