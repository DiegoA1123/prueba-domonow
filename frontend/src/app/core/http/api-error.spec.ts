import { HttpErrorResponse } from '@angular/common/http';
import { GENERIC_ERROR_MESSAGE, toApiError } from './api-error';

describe('toApiError', () => {
  it('uses the ProblemDetails detail and code sent by the backend', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { detail: 'El parqueadero P15 ya está ocupado.', code: 'Parking.SpotOccupied' },
    });

    expect(toApiError(error)).toEqual({
      status: 409,
      code: 'Parking.SpotOccupied',
      message: 'El parqueadero P15 ya está ocupado.',
    });
  });

  it('explains that the server is unreachable when status is 0', () => {
    const apiError = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(apiError.code).toBeNull();
    expect(apiError.message).toContain('No se pudo conectar');
  });

  it('explains rate limiting when the 429 body has no detail', () => {
    expect(toApiError(new HttpErrorResponse({ status: 429 })).message).toContain(
      'Demasiados intentos',
    );
  });

  it('explains an expired session for a 401 without detail', () => {
    expect(
      toApiError(new HttpErrorResponse({ status: 401, error: { title: 'Unauthorized' } })).message,
    ).toContain('sesión expiró');
  });

  it('returns a generic message for non-HTTP errors', () => {
    expect(toApiError(new Error('boom'))).toEqual({
      status: -1,
      code: null,
      message: GENERIC_ERROR_MESSAGE,
    });
  });

  it('returns ApiError values unchanged', () => {
    const apiError = {
      status: 422,
      code: 'Parking.SpotOutOfService',
      message: 'Fuera de servicio.',
    };

    expect(toApiError(apiError)).toBe(apiError);
  });
});
