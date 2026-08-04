import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Drawer } from 'primeng/drawer';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  Lead, LeadStatus, LeadSource, LEAD_STATUSES, LEAD_SOURCES, LEAD_STATUS_CLASS
} from '../../core/models/lead.model';
import { Plan } from '../../core/models/plan.model';
import { utcDateOnly } from '../../core/utils/date.util';

interface ConvertForm {
  name: string; slug: string; planId: number | null;
  adminFullName: string; adminPhone: string; adminPassword: string;
  paidUntil: Date | null;
}

@Component({
  selector: 'app-leads',
  standalone: true,
  imports: [DatePipe, FormsModule, TableModule, Button, Dialog, Drawer, InputText,
    Textarea, Password, Select, DatePicker],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leads.component.html',
  styleUrl: './leads.component.scss'
})
export default class LeadsComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  leads = signal<Lead[]>([]);
  plans = signal<Plan[]>([]);
  loading = signal(true);
  saving = signal(false);

  statusFilter = signal<LeadStatus | null>(null);
  sourceFilter = signal<LeadSource | null>(null);
  search = signal('');
  fromDate = signal<Date | null>(null);
  toDate = signal<Date | null>(null);

  readonly statusOptions = LEAD_STATUSES;
  readonly sourceOptions = LEAD_SOURCES;

  /**
   * Platforma miqyosida lead soni kichik — bir marta yuklab, filtrlarni
   * brauzerda qo'llaymiz, har filtr o'zgarishida so'rov yubormaymiz.
   */
  visibleLeads = computed(() => {
    const status = this.statusFilter();
    const source = this.sourceFilter();
    const q = this.search().trim().toLowerCase();
    const from = this.fromDate()?.getTime();
    const to = this.toDate()?.getTime();

    return this.leads()
      .filter(l => !status || l.status === status)
      .filter(l => !source || l.source === source)
      .filter(l => !q || l.companyName.toLowerCase().includes(q) || l.phone.includes(q))
      .filter(l => {
        if (!from && !to) return true;
        const created = new Date(l.createdAt).getTime();
        if (from && created < from) return false;
        if (to && created > to + 86_400_000) return false;
        return true;
      })
      // Javob berilmagani tepada, keyin eng yangisi
      .sort((a, b) => {
        const aNew = a.status === 'New' ? 0 : 1;
        const bNew = b.status === 'New' ? 0 : 1;
        if (aNew !== bNew) return aNew - bNew;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
  });

  newCount = computed(() => this.leads().filter(l => l.status === 'New').length);

  // Detail drawer
  drawerVisible = signal(false);
  selected = signal<Lead | null>(null);
  statusDraft = signal<LeadStatus>('New');
  statusNote = signal('');

  // Convert dialog
  convertVisible = signal(false);
  convertForm = signal<ConvertForm>(this.emptyConvert());

  private emptyConvert(): ConvertForm {
    return { name: '', slug: '', planId: null, adminFullName: '', adminPhone: '', adminPassword: '', paidUntil: null };
  }

  ngOnInit() {
    this.load();
    this.service.getPlans().subscribe(r => { if (r.success && r.data) this.plans.set(r.data); });
    // Dashboard kartasi "New" bilan havola qiladi
    const status = this.route.snapshot.queryParamMap.get('status') as LeadStatus | null;
    if (status && LEAD_STATUSES.some(s => s.value === status)) this.statusFilter.set(status);
  }

  load() {
    this.loading.set(true);
    this.service.getLeads().subscribe({
      next: (r) => {
        const data = r.success ? r.data : null;
        // Backend sahifalangan yoki oddiy massiv qaytarishi mumkin
        this.leads.set(Array.isArray(data) ? data : (data?.items ?? []));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  planOptions() { return this.plans().map(p => ({ label: p.name, value: p.id })); }

  statusClass(status: LeadStatus) { return LEAD_STATUS_CLASS[status] ?? 'pill pill-neutral'; }
  statusLabel(status: LeadStatus) { return LEAD_STATUSES.find(s => s.value === status)?.label ?? status; }

  clearFilters() {
    this.statusFilter.set(null); this.sourceFilter.set(null);
    this.search.set(''); this.fromDate.set(null); this.toDate.set(null);
  }

  // ---- Detail -------------------------------------------------------------

  open(lead: Lead) {
    this.selected.set(lead);
    this.statusDraft.set(lead.status);
    this.statusNote.set('');
    this.drawerVisible.set(true);
  }

  saveStatus() {
    const lead = this.selected(); if (!lead) return;
    this.saving.set(true);
    this.service.updateLead(lead.id, {
      status: this.statusDraft(),
      statusNote: this.statusNote().trim() || null
    }).subscribe({
      next: (r) => {
        this.saving.set(false);
        this.notify.success('Lead updated');
        if (r.success && r.data) this.selected.set(r.data);
        this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  goToTenant(id: number) {
    this.drawerVisible.set(false);
    this.router.navigate(['/tenants'], { queryParams: { highlight: id } });
  }

  // ---- Convert ------------------------------------------------------------

  openConvert() {
    const lead = this.selected(); if (!lead) return;
    this.convertForm.set({
      ...this.emptyConvert(),
      name: lead.companyName,
      slug: this.slugify(lead.companyName),
      adminFullName: lead.contactName,
      adminPhone: lead.phone
    });
    this.convertVisible.set(true);
  }

  updateConvert(field: keyof ConvertForm, value: unknown) {
    this.convertForm.update(f => ({ ...f, [field]: value }));
  }

  private slugify(v: string): string {
    return v.toLowerCase().trim()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/[\s-]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  convert() {
    const lead = this.selected(); const f = this.convertForm();
    if (!lead) return;
    if (!f.name.trim() || !f.slug.trim()) { this.notify.warn('Name and slug are required'); return; }
    if (!f.adminFullName.trim() || !f.adminPhone.trim() || f.adminPassword.length < 6) {
      this.notify.warn('Admin name, phone and a 6+ char password are required'); return;
    }

    this.saving.set(true);
    this.service.convertLead(lead.id, {
      name: f.name.trim(), slug: f.slug.trim(), planId: f.planId,
      adminFullName: f.adminFullName.trim(), adminPhone: f.adminPhone.trim(),
      adminPassword: f.adminPassword,
      paidUntil: f.paidUntil ? utcDateOnly(f.paidUntil) : null
    }).subscribe({
      next: () => {
        // Slug band bo'lsa backend 400 qaytaradi va lead o'zgarmaydi
        this.saving.set(false);
        this.convertVisible.set(false);
        this.notify.success(`Tenant "${f.name.trim()}" created — lead marked as won`);
        this.load();
        this.drawerVisible.set(false);
      },
      error: () => this.saving.set(false)
    });
  }
}
