import { HttpErrorResponse } from '@angular/common/http';

/** Error de la API ya traducido a algo que la UI puede mostrar. */
export interface ApiError {
  readonly status: number;
  readonly code: string | null;
  readonly message: string;
}

export const GENERIC_ERROR_MESSAGE = 'Ocurrió un error inesperado. Intente de nuevo más tarde.';

const FALLBACK_MESSAGES: Partial<Record<number, string>> = {
  0: 'No se pudo conectar con el servidor. Verifique su conexión e intente de nuevo.',
  401: 'Su sesión expiró. Inicie sesión de nuevo.',
  403: 'No tiene permisos para realizar esta acción.',
  429: 'Demasiados intentos. Espere un minuto e intente de nuevo.',
};

interface ProblemDetailsBody {
  readonly detail?: unknown;
  readonly code?: unknown;
}

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'code' in value &&
    'message' in value
  );
}

export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (!(error instanceof HttpErrorResponse)) {
    return { status: -1, code: null, message: GENERIC_ERROR_MESSAGE };
  }

  const body: ProblemDetailsBody =
    typeof error.error === 'object' && error.error !== null ? error.error : {};
  const detail = typeof body.detail === 'string' && body.detail.trim() !== '' ? body.detail : null;

  return {
    status: error.status,
    code: typeof body.code === 'string' ? body.code : null,
    message: detail ?? FALLBACK_MESSAGES[error.status] ?? GENERIC_ERROR_MESSAGE,
  };
}
