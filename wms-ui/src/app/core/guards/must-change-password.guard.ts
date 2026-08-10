import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Parolni egasidan boshqa odam qo'ygan bo'lsa (yangi hisob yoki tiklangan parol),
 * foydalanuvchi avval o'zinikini qo'yishi kerak.
 *
 * Backend buni ataylab majburlamaydi: bloklash `change-password` endpointining o'zini
 * ham to'sib qo'yish xavfini tug'diradi. Shuning uchun majburlash shu yerda va
 * **bitta istisno bilan** — profil sahifasi ochiq qoladi, chunki parol aynan o'sha
 * yerda o'zgartiriladi. Chiqish ham har doim ishlaydi (u marshrut emas).
 */
export const mustChangePasswordGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.currentUser()?.mustChangePassword) return true;

  // Yagona ruxsat etilgan manzil — aks holda foydalanuvchi qulflanib qoladi.
  if (state.url.startsWith(PROFILE_URL)) return true;

  return router.createUrlTree([PROFILE_URL], { queryParams: { mustChangePassword: 1 } });
};

const PROFILE_URL = '/settings/profile';
