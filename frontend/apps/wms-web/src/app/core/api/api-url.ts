/**
 * So'rov manzili WMS API'sigami — ya'ni javobi `ApiResponse<T>` konvertidami.
 *
 * Nega kerak: HTTP zanjiri ikki xil backendga boradi. `apiUrl` (`/api`) — WMS,
 * javob eski WMS konvertida (D15). `identityUrl` — Identity'ning BFF sessiya
 * endpointlari, xato RFC 9457 shaklida. Xatoni to'g'ri o'qish uchun avval
 * uning QAYERDAN kelganini bilish kerak.
 *
 * Solishtirish PARSER orqali (origin + yo'l prefiksi): `apiUrl` nisbiy ham
 * (`/api`, dev-proksi va nginx), absolyut ham (`https://…/api`) bo'lishi mumkin.
 * Satr boshini qo'lda solishtirish nisbiy/absolyut aralashganda adashardi.
 */
function resolve(url: string): URL | null {
  try {
    return new URL(url, globalThis.location?.href ?? 'http://localhost/');
  } catch {
    return null;
  }
}

function trimSlashes(value: string): string {
  return value.replace(/\/+$/, '');
}

/** Manzil `apiUrl` ostidami. */
export function isWmsApiUrl(url: string, apiUrl: string): boolean {
  const target = resolve(url);
  const base = resolve(`${trimSlashes(apiUrl)}/`);
  if (target === null || base === null) {
    return false;
  }
  return target.origin === base.origin && target.pathname.startsWith(base.pathname);
}

/** Manzil aynan `GET {apiUrl}/me` mi (so'rov satrisiz). */
export function isMeUrl(url: string, apiUrl: string): boolean {
  const target = resolve(url);
  const base = resolve(`${trimSlashes(apiUrl)}/me`);
  if (target === null || base === null) {
    return false;
  }
  return target.origin === base.origin && trimSlashes(target.pathname) === base.pathname;
}
