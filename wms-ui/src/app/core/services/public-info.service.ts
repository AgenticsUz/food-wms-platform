import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { resolveTenantSlug } from '../../shared/utils/tenant-slug.util';

interface PublicInfoResponse {
  data?: {
    name?: string | null;
    supportPhone?: string | null;
    supportEmail?: string | null;
  };
}

/**
 * Tokensiz ochiladigan sahifalar (login, demo so'rovi) uchun ma'lumot.
 *
 * Qo'llab-quvvatlash aloqasi serverning `Support:Phone` / `Support:Email` sozlamasidan
 * keladi. Ilgari bu qiymatlar `environment.ts` da yozilgan edi va raqamni almashtirish
 * uchun butun Angular ilovasini qayta yig'ish kerak bo'lardi — obuna bloklanganda mijoz
 * aynan shu raqamni ko'rishini hisobga olsak, bu juda qimmat yo'l edi.
 *
 * `environment` qiymatlari faqat zaxira sifatida qoladi: server javob bermasa ham
 * bloklangan mijoz bo'sh joy emas, biror aloqa ko'rishi kerak.
 */
@Injectable({ providedIn: 'root' })
export class PublicInfoService {
  private http = inject(HttpClient);

  supportPhone = signal(environment.supportPhone);
  supportEmail = signal(environment.supportEmail);

  private loaded = false;

  /** Sahifa ochilganda bir marta chaqiriladi; xato bo'lsa jimgina zaxirada qolamiz. */
  load(): void {
    if (this.loaded) return;
    this.loaded = true;

    const slug = resolveTenantSlug();
    this.http.get<PublicInfoResponse['data']>(`${environment.apiUrl}/public/branding`, { params: { slug } })
      .subscribe({
        next: (res: unknown) => {
          const data = (res as PublicInfoResponse)?.data ?? (res as PublicInfoResponse['data']);
          if (data?.supportPhone) this.supportPhone.set(data.supportPhone);
          if (data?.supportEmail) this.supportEmail.set(data.supportEmail);
        },
        error: () => { /* zaxira qiymatlar o'z holicha qoladi */ }
      });
  }
}
