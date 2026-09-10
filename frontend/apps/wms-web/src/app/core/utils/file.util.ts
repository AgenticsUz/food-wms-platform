/**
 * Blob'ni fayl sifatida saqlaydi (eksport, shablon, PDF).
 *
 * Eski `export.service.ts` / `import.service.ts` da bu kod ikki nusxada edi.
 * `revokeObjectURL` keyingi tick'da — ba'zi brauzerlar `click()` sinxron
 * qaytgach ham yuklashni boshlamagan bo'ladi va darhol bekor qilingan URL
 * bo'sh fayl beradi.
 */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
