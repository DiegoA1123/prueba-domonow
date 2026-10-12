import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { API_BASE_URL } from '../config/api-base-url';
import { ApiError } from '../http/api-error';
import { apiErrorInterceptor } from '../http/api-error.interceptor';
import { LOGIN_PATH } from './auth-api';
import { authInterceptor } from './auth.interceptor';
import { SessionStore } from './session.store';

const API = 'http://api.test';

describe('auth and API error interceptors', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let session: SessionStore;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: API },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionStore);
    session.start({
      accessToken: 'token-123',
      username: 'portero',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    });
  });

  afterEach(() => controller.verify());

  it('adds the bearer token to API requests', () => {
    http.get(`${API}/api/parking-spots`).subscribe();

    expect(
      controller.expectOne(`${API}/api/parking-spots`).request.headers.get('Authorization'),
    ).toBe('Bearer token-123');
  });

  it('does not send the token to other hosts', () => {
    http.get('https://fonts.example.com/font.css').subscribe();

    expect(
      controller
        .expectOne('https://fonts.example.com/font.css')
        .request.headers.has('Authorization'),
    ).toBe(false);
  });

  it('ends the session and redirects to login when the API answers 401', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const response = firstValueFrom(http.get(`${API}/api/parking-spots`));

    controller
      .expectOne(`${API}/api/parking-spots`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toMatchObject({ status: 401 } satisfies Partial<ApiError>);
    expect(session.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { reason: 'expired' } });
  });

  it('keeps the session when the login request itself answers 401', async () => {
    const response = firstValueFrom(http.post(`${API}${LOGIN_PATH}`, {}));

    controller
      .expectOne(`${API}${LOGIN_PATH}`)
      .flush(
        { detail: 'Usuario o contraseña incorrectos.', code: 'Auth.InvalidCredentials' },
        { status: 401, statusText: 'Unauthorized' },
      );

    await expect(response).rejects.toMatchObject({ code: 'Auth.InvalidCredentials' });
    expect(session.isAuthenticated()).toBe(true);
  });
});
