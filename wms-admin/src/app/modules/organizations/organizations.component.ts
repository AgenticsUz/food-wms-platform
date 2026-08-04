import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Drawer } from 'primeng/drawer';
import { InputText } from 'primeng/inputtext';
import { PlatformService } from '../../core/services/platform.service';
import { Organization, OrganizationDetail } from '../../core/models/organization.model';

/**
 * "Kim kim bilan ishlaydi" ko'rinishi: bir xil STIR li kompaniya bir necha
 * tenantda counterparty sifatida uchrasa — bu upsell uchun tayyor signal.
 */
@Component({
  selector: 'app-organizations',
  standalone: true,
  imports: [DatePipe, FormsModule, TableModule, Button, Drawer, InputText],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './organizations.component.html',
  styleUrl: '../leads/leads.component.scss'
})
export default class OrganizationsComponent implements OnInit {
  private service = inject(PlatformService);
  private router = inject(Router);

  organizations = signal<Organization[]>([]);
  loading = signal(true);
  search = signal('');

  drawerVisible = signal(false);
  selected = signal<OrganizationDetail | null>(null);
  loadingDetail = signal(false);

  visible = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.organizations();
    return this.organizations().filter(o =>
      o.name.toLowerCase().includes(q) || (o.inn ?? '').includes(q));
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getOrganizations().subscribe({
      next: (r) => {
        const data = r.success ? r.data : null;
        this.organizations.set(Array.isArray(data) ? data : (data?.items ?? []));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  open(org: Organization) {
    this.selected.set(null);
    this.drawerVisible.set(true);
    this.loadingDetail.set(true);
    this.service.getOrganization(org.id).subscribe({
      next: (r) => { if (r.success && r.data) this.selected.set(r.data); this.loadingDetail.set(false); },
      error: () => this.loadingDetail.set(false)
    });
  }

  goToTenant(id: number) {
    this.drawerVisible.set(false);
    this.router.navigate(['/tenants'], { queryParams: { highlight: id } });
  }
}
