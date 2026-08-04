import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { MultiSelect } from 'primeng/multiselect';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import { Plan } from '../../core/models/plan.model';
import { ModuleInfo } from '../../core/models/plan.model';
import { FeatureInfo } from '../../core/models/feature.model';

interface PlanForm {
  id?: number; name: string; code: string; price: number; isActive: boolean;
  moduleCodes: string[]; featureCodes: string[];
  maxUsers: number; maxWarehouses: number; maxTransfersPerMonth: number;
  trialDays: number; isDefault: boolean;
}

/** Modulga bog'lanmagan feature'lar guruhi (backend `moduleCode: null` yuboradi). */
const GENERAL_GROUP = 'GENERAL';

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [TranslocoDirective, DecimalPipe, FormsModule, TableModule, Button, Dialog, InputText, InputNumber, MultiSelect, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './plans.component.html',
  styleUrl: '../tenants/tenants.component.scss'
})
export default class PlansComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  plans = signal<Plan[]>([]);
  modules = signal<ModuleInfo[]>([]);
  features = signal<FeatureInfo[]>([]);
  loading = signal(true);
  saving = signal(false);
  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<PlanForm>(this.empty());

  private empty(): PlanForm {
    return { name: '', code: '', price: 0, isActive: true, moduleCodes: [], featureCodes: [],
      maxUsers: 10, maxWarehouses: 3, maxTransfersPerMonth: 1000, trialDays: 0, isDefault: false };
  }

  ngOnInit() {
    this.load();
    this.service.getModules().subscribe(r => { if (r.success && r.data) this.modules.set(r.data); });
    // Custom feature'lar planga kirmaydi — ular faqat tenant override orqali yoqiladi
    this.service.getFeatures().subscribe(r => {
      if (r.success && r.data) this.features.set(r.data.filter(f => !f.isCustom));
    });
  }

  moduleOptions() { return this.modules().map(m => ({ label: m.moduleName, value: m.moduleCode })); }

  /** Feature'lar modul bo'yicha guruhlanadi; moduli tanlanmagan guruh o'chirilgan ko'rinadi. */
  featureGroups = computed(() => {
    const selected = this.form().moduleCodes;
    const groups = new Map<string, FeatureInfo[]>();
    for (const f of this.features()) {
      // Modulga bog'lanmagan feature'lar (export, import, analitika) alohida guruhda va
      // har doim tanlanadigan — ular hech qanday modulga tobe emas.
      const key = f.moduleCode ?? GENERAL_GROUP;
      const list = groups.get(key);
      if (list) list.push(f); else groups.set(key, [f]);
    }
    return [...groups.entries()]
      .map(([moduleCode, items]) => ({
        moduleCode,
        items,
        moduleSelected: moduleCode === GENERAL_GROUP || selected.includes(moduleCode)
      }))
      .sort((a, b) => a.moduleCode.localeCompare(b.moduleCode));
  });

  isFeatureSelected(code: string) { return this.form().featureCodes.includes(code); }

  toggleFeature(code: string, checked: boolean) {
    this.form.update(f => ({
      ...f,
      featureCodes: checked ? [...f.featureCodes, code] : f.featureCodes.filter(c => c !== code)
    }));
  }

  load() {
    this.loading.set(true);
    this.service.getPlans().subscribe({
      next: (r) => { this.plans.set(r.success && r.data ? r.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openNew() { this.form.set(this.empty()); this.editing.set(false); this.dialogVisible.set(true); }
  openEdit(p: Plan) {
    this.form.set({ id: p.id, name: p.name, code: p.code, price: p.price, isActive: p.isActive,
      moduleCodes: [...p.moduleCodes], featureCodes: [...(p.featureCodes ?? [])],
      maxUsers: p.maxUsers, maxWarehouses: p.maxWarehouses,
      maxTransfersPerMonth: p.maxTransfersPerMonth, trialDays: p.trialDays, isDefault: p.isDefault });
    this.editing.set(true); this.dialogVisible.set(true);
  }
  updateForm(field: keyof PlanForm, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.code.trim()) { this.notify.warn(this.transloco.translate('plans.nameRequired')); return; }
    this.saving.set(true);
    const dto = { name: f.name.trim(), code: f.code.trim(), price: f.price, isActive: f.isActive,
      moduleCodes: f.moduleCodes, featureCodes: f.featureCodes,
      maxUsers: f.maxUsers, maxWarehouses: f.maxWarehouses,
      maxTransfersPerMonth: f.maxTransfersPerMonth, trialDays: f.trialDays, isDefault: f.isDefault };
    const obs = this.editing() ? this.service.updatePlan(f.id!, dto) : this.service.createPlan(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.transloco.translate('plans.saved')); this.load(); },
      error: () => this.saving.set(false)
    });
  }

  remove(p: Plan) {
    this.notify.confirmDelete(this.transloco.translate('plans.deleteConfirm', { name: p.name }), () => {
      this.service.deletePlan(p.id).subscribe(() => {
        this.notify.success(this.transloco.translate('plans.deleted'));
        this.load();
      });
    });
  }
}
