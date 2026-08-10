/**
 * Matnni buferga ko'chiradi.
 *
 * Clipboard API faqat xavfsiz kontekstda (https yoki localhost) ishlaydi va haqiqiy
 * foydalanuvchi harakatini talab qiladi. Ilova oddiy `http` orqali ochilgan bo'lsa
 * foydalanuvchini bir martalik parolni qo'lda ko'chirishga majburlamaslik uchun eski
 * `execCommand` usuli zaxira sifatida qoladi.
 *
 * `false` qaytsa — chaqiruvchi foydalanuvchiga aytishi kerak, jimgina yutib yubormasin.
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
