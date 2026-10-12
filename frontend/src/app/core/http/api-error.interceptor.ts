import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { LOGIN_PATH } from '../auth/auth-api';
import { SessionStore } from '../auth/session.store';
import { toApiError } from './api-error';

/** Traduce cualquier error HTTP a ApiError; un 401 fuera del login cierra la sesión y lleva al login. */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      const apiError = toApiError(error);
      if (apiError.status === 401 && !request.url.endsWith(LOGIN_PATH)) {
        session.end();
        void router.navigate(['/login'], { queryParams: { reason: 'expired' } });
      }
      return throwError(() => apiError);
    }),
  );
};
