import { HttpErrorResponse } from '@angular/common/http';

import { toWmsApiError } from './wms-api-error';

const wmsError = (status: number, code: string | null, message = 'server matni'): HttpErrorResponse =>
  new HttpErrorResponse({
    status,
    url: '/api/x',
    error: { success: false, data: null, message, code, warning: null },
  });

describe('toWmsApiError', () => {
  it('402 + blok kodi → subscription, server matni saqlanadi', () => {
    const error = toWmsApiError(wmsError(402, 'trial_expired'));
    expect(error.kind).toBe('subscription');
    expect(error.title).toBe('errors.trialExpired');
    expect(error.message).toBe('server matni');
    expect(error.wmsCode).toBe('trial_expired');
  });

  it('402 + limit kodi → blok EMAS, biznes-xato', () => {
    const error = toWmsApiError(wmsError(402, 'limit_users'));
    expect(error.kind).toBe('business');
    expect(error.title).toBe('errors.limitUsers');
  });

  it('403 module_disabled / feature_disabled → module-disabled; kodsiz → forbidden', () => {
    expect(toWmsApiError(wmsError(403, 'module_disabled:PRODUCTION')).title).toBe('errors.moduleDisabled');
    expect(toWmsApiError(wmsError(403, 'feature_disabled:warehouse.batches')).title).toBe(
      'errors.featureDisabled'
    );
    expect(toWmsApiError(wmsError(403, null)).kind).toBe('forbidden');
  });

  it('409 → conflict; eski ekranlar uchun `error.message` saqlanadi', () => {
    const error = toWmsApiError(wmsError(409, null, 'Yozuv o\'zgargan'));
    expect(error.kind).toBe('conflict');
    expect(error.error?.message).toBe('Yozuv o\'zgargan');
  });

  it('401 → unauthorized (authInterceptor refresh qila olishi uchun status saqlanadi)', () => {
    const error = toWmsApiError(wmsError(401, null));
    expect(error.status).toBe(401);
    expect(error.kind).toBe('unauthorized');
  });

  it('konvert bo\'lmasa (RFC 9457) — platforma o\'quvchisi: 400 + errors → validation', () => {
    const error = toWmsApiError(
      new HttpErrorResponse({ status: 400, error: { title: 'x', errors: { name: ['required'] } } })
    );
    expect(error.kind).toBe('validation');
    expect(error.fieldErrors?.['name']).toEqual(['required']);
  });
});
