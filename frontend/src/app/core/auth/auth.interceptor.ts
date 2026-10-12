import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from '../config/api-base-url';
import { SessionStore } from './session.store';

/** Añade el JWT solo a las peticiones dirigidas a nuestra API (nunca a terceros). */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(SessionStore).accessToken();
  if (!token || !request.url.startsWith(inject(API_BASE_URL))) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
