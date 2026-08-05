import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

/**
 * Backend xato va tasdiq matnlarini foydalanuvchi o'qiyotgan tilda qaytarishi uchun
 * har so'rovga `Accept-Language` qo'shadi.
 *
 * Brauzerning o'z tili emas, aynan interfeysda tanlangan til yuboriladi — foydalanuvchi
 * ilovani ruschaga o'tkazgan bo'lsa, server xabari ham ruscha bo'lishi kerak.
 *
 * `uz-cyrl` ni backend `uz` sifatida qabul qiladi (lotin), chunki serverda hozircha
 * uch til bor: uz, ru, en.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const lang = inject(TranslocoService).getActiveLang();
  return next(req.clone({ setHeaders: { 'Accept-Language': lang } }));
};
