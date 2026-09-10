/**
 * Matnni buferga ko'chiradi (eski `wms-ui/shared/utils/clipboard.util.ts`).
 *
 * Clipboard API faqat xavfsiz kontekstda (https yoki localhost) ishlaydi. Ilova
 * `http` orqali ochilgan bo'lsa eski `execCommand` usuli zaxira sifatida qoladi.
 *
 * `false` qaytsa — chaqiruvchi foydalanuvchiga aytishi kerak, jimgina yutmasin.
 */
export async function writeToClipboard(value: string): Promise<boolean> {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(value);
      return true;
    }
  } catch {
    // quyidagi zaxira usulga o'tamiz
  }

  try {
    const ta = document.createElement('textarea');
    ta.value = value;
    ta.setAttribute('readonly', '');
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand('copy');
    document.body.removeChild(ta);
    return ok;
  } catch {
    return false;
  }
}
