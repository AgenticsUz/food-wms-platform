import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ApiService } from './api.service';
import {
  PortalLoginDto, PortalAuthResponse, PortalCounterparty, PortalFinance
} from '../models/settings.model';
import { Transfer } from '../models/transfer.model';
import { PaymentHistory } from '../models/counterparty.model';

@Injectable({ providedIn: 'root' })
export class PortalService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private api = inject(ApiService);

  portalToken = signal<string | null>(localStorage.getItem('portalToken'));
  counterparty = signal<PortalCounterparty | null>(this.loadCounterparty());

  private loadCounterparty(): PortalCounterparty | null {
    const stored = localStorage.getItem('portalCounterparty');
    if (stored) { try { return JSON.parse(stored); } catch { return null; } }
    return null;
  }

  login(dto: PortalLoginDto) {
    return this.http.post<ApiResponse<PortalAuthResponse>>(
      `${environment.apiUrl}/portal/login`, dto
    ).pipe(tap(res => {
      if (res.success && res.data) {
        this.portalToken.set(res.data.token);
        this.counterparty.set(res.data.counterparty);
        localStorage.setItem('portalToken', res.data.token);
        localStorage.setItem('portalCounterparty', JSON.stringify(res.data.counterparty));
      }
    }));
  }

  logout() {
    this.portalToken.set(null);
    this.counterparty.set(null);
    localStorage.removeItem('portalToken');
    localStorage.removeItem('portalCounterparty');
    this.router.navigate(['/portal/login']);
  }

  isAuthenticated(): boolean { return !!this.portalToken(); }

  getMe() { return this.api.get<PortalCounterparty>('portal/me'); }
  getTransfers(params?: Record<string, string | number | boolean>) { return this.api.get<Transfer[]>('portal/transfers', params); }
  getTransfer(id: number) { return this.api.get<Transfer>(`portal/transfers/${id}`); }
  getFinance() { return this.api.get<PortalFinance>('portal/finance'); }
  getPayments() { return this.api.get<PaymentHistory[]>('portal/payments'); }
}
