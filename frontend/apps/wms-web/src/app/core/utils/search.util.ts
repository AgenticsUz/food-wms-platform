import { DestroyRef, inject, type Signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, map, skip } from 'rxjs';

import type { ApiCallOptions } from '../api/api.service';

/**
 * Qidiruv harfi bilan so'rov orasidagi kutish.
 *
 * NEGA kerak: qidiruv endi SERVERDA (pg_trgm + lotin↔kirill transliteratsiya) —
 * mijozdagi `includes` alifboni bilmagani uchun «Сникерс» yozgan odam «Snikers»
 * ni topa olmasdi. Lekin har bosilgan harfga so'rov yuborish serverni ham,
 * tarmoqni ham behuda yuklaydi. 300 ms — yozish tugagach deyarli darhol, lekin
 * bir so'z bitta so'rov.
 */
export const SEARCH_DEBOUNCE_MS = 300;

/**
 * Qidiruv so'rovi FON so'rovi: global progress chizig'i har harfda chaqnamasin.
 * Kutish holatini ekran o'zining `loading` signali bilan (jadval ustida)
 * ko'rsatadi. Xato toasti esa qoladi — qidiruv uzilganini bilish kerak.
 */
export const SEARCH_CALL: ApiCallOptions = { skipLoading: true };

/**
 * `search` signalini kuzatadi va tinchigach `run(query)` ni chaqiradi.
 *
 * Faqat inyeksiya kontekstidan (maydon initsializatori yoki konstruktor)
 * chaqiriladi: `toObservable`/`DestroyRef` shuni talab qiladi.
 *
 * `skip(1)` — `toObservable` joriy qiymatni DARHOL bir marta beradi; ekran esa
 * dastlabki ro'yxatni `ngOnInit` da o'zi yuklaydi, aks holda ochilishda ikkita
 * bir xil so'rov ketardi.
 */
export function onSearchChange(search: Signal<string>, run: (query: string) => void): void {
  const destroyRef = inject(DestroyRef);
  toObservable(search)
    .pipe(
      skip(1),
      map((value) => value.trim()),
      debounceTime(SEARCH_DEBOUNCE_MS),
      // Bo'shliq qo'shish yoki harfni yozib o'chirish — so'rov takrorlanmasin.
      distinctUntilChanged(),
      takeUntilDestroyed(destroyRef)
    )
    .subscribe(run);
}
