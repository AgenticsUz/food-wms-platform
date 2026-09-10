// Runtime konfiguratsiya faylini tayyorlaydi (Wash `tools/ensure-app-config.mjs` naqshi).
//
// MUAMMO: `apps/<app>/src/assets/config/app-config.json` `.gitignore` da — u har
// stend uchun alohida va repoga tushmaydi. Toza klondan keyin fayl yo'q bo'ladi,
// `@agentics/config` esa zaxira qiymatga tushadi — va zaxirada `auth` bloki
// ATAYLAB yo'q (fail-closed): login Identity tomonidan `invalid_request` bilan
// rad etiladi.
//
// YECHIM: fayl yo'q bo'lsa, yonidagi `app-config.sample.json` dan nusxa olinadi.
// Mavjud fayl HECH QACHON qayta yozilmaydi — dasturchi o'z sozlamasini yo'qotmaydi.

import { copyFile, access } from 'node:fs/promises';
import { constants } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const workspaceRoot = dirname(dirname(fileURLToPath(import.meta.url)));
// Ro'yxat — bitta element bo'lsa ham: yangi ilova qo'shilsa skript emas, shu qator o'zgaradi.
const apps = ['wms-web'];

async function exists(path) {
  try {
    await access(path, constants.F_OK);
    return true;
  } catch {
    return false;
  }
}

let created = 0;

for (const app of apps) {
  const dir = join(workspaceRoot, 'apps', app, 'src', 'assets', 'config');
  const target = join(dir, 'app-config.json');
  const sample = join(dir, 'app-config.sample.json');

  if (await exists(target)) {
    continue;
  }
  if (!(await exists(sample))) {
    console.warn(`[app-config] ${app}: namuna fayl topilmadi (${sample}) — o'tkazib yuborildi.`);
    continue;
  }

  await copyFile(sample, target);
  console.log(`[app-config] ${app}: app-config.sample.json -> app-config.json (yaratildi)`);
  created += 1;
}

if (created === 0) {
  console.log('[app-config] barcha runtime konfiguratsiya fayllari joyida.');
}
