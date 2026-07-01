import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ApiService } from './api.service';
import {
  AgentPortalLoginDto, AgentPortalAuthResponse, AgentPortalProfile,
  AgentSalesReport, CommissionRecord
} from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentPortalService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private api = inject(ApiService);

  token = signal<string | null>(localStorage.getItem('agentPortalToken'));
  agent = signal<AgentPortalProfile | null>(this.loadAgent());

  private loadAgent(): AgentPortalProfile | null {
    const stored = localStorage.getItem('agentPortalProfile');
    if (stored) { try { return JSON.parse(stored); } catch { return null; } }
    return null;
  }

  login(dto: AgentPortalLoginDto) {
    return this.http.post<ApiResponse<AgentPortalAuthResponse>>(
      `${environment.apiUrl}/agent-portal/login`, dto
    ).pipe(tap(res => {
      if (res.success && res.data) {
        this.token.set(res.data.token);
        this.agent.set(res.data.agent);
        localStorage.setItem('agentPortalToken', res.data.token);
        localStorage.setItem('agentPortalProfile', JSON.stringify(res.data.agent));
      }
    }));
  }

  logout() {
    this.token.set(null);
    this.agent.set(null);
    localStorage.removeItem('agentPortalToken');
    localStorage.removeItem('agentPortalProfile');
    this.router.navigate(['/agent-portal/login']);
  }

  isAuthenticated(): boolean { return !!this.token(); }

  getMe() { return this.api.get<AgentPortalProfile>('agent-portal/me'); }
  getSales() { return this.api.get<AgentSalesReport>('agent-portal/sales'); }
  getCommissions() { return this.api.get<CommissionRecord[]>('agent-portal/commissions'); }
}
