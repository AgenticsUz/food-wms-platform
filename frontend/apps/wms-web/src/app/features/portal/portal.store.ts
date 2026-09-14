import { Injectable, computed, inject, signal } from '@angular/core';
import { map, of, shareReplay, tap, type Observable } from 'rxjs';

import { PortalService } from './portal.service';
import type { PortalMe } from './portal.model';

/**
 * Kabinet egasi haqidagi ma'lumot — qobiq va sahifalar uchun BITTA manba.
 *
 * `GET /api/portal/me` har sahifada qayta so'ralmaydi: birinchi chaqiruv so'rov
 * yuboradi, qolganlari `shareReplay` dan o'qiydi. Sahifalar «kim men» javobini
 * KUTADI (`ensure()`), chunki qaysi so'rov ketishi rolga bog'liq: kontragent
 * agent yuzasiga borsa 403 olardi. Ilovadagi `WmsSession` ning muqobili.
 */
@Injectable({ providedIn: 'root' })
export class PortalStore {
  private readonly portal = inject(PortalService);

  private readonly state = signal<PortalMe | null>(null);
  private request: Observable<PortalMe | null> | null = null;

  readonly me = this.state.asReadonly();
  readonly name = computed(() => this.state()?.name ?? '');
  readonly tenantName = computed(() => this.state()?.tenantName ?? '');

  /** Kim ekanini bir marta so'raydi; keyingi chaqiruvlar keshdan. */
  ensure(): Observable<PortalMe | null> {
    this.request ??= this.portal.getMe().pipe(
      map((res) => (res.success ? (res.data ?? null) : null)),
      tap((me) => this.state.set(me)),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    return this.request;
  }

  /**
   * Kabinet ochilmagan (403) holatida ham sahifa yiqilmasin: xato `ApiService`
   * darajasida ko'rsatiladi, bu yerda oqim bo'sh qiymat bilan davom etadi.
   */
  ensureSafe(): Observable<PortalMe | null> {
    return this.state() ? of(this.state()) : this.ensure();
  }
}
