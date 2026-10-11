# Frontend — Parqueaderos de visitantes · Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aplicación Angular 22 que permite a un operador iniciar sesión, ver los 30 parqueaderos de visitantes por estado, registrar ingresos y salidas contra la API del backend y ver los vehículos que están dentro, con estado 100 % reactivo.

**Architecture:** Hexagonal por feature (`domain` → `application` → `infrastructure` → `ui`) + `core` transversal (config, HTTP, auth, notificaciones). El puerto `ParkingRepository` (abstract class) se enlaza al adaptador HTTP en los providers de la ruta. El estado vive en un `ParkingStore` con signals; HTTP y polling son flujos RxJS que terminan en signals. Componentes standalone, zoneless, OnPush, `input()/output()/model()`. Formularios con **Signal Forms** (estables en Angular 22).

**Tech Stack:** Angular 22.2 (zoneless) · TypeScript 6 (strict) · Angular Material/CDK 22.2.2 · Signal Forms (`@angular/forms/signals`) · RxJS 7.8 · Vitest 5 + jsdom (`@angular/build:unit-test`) · angular-eslint 22.6 · Prettier.

**Spec:** `CLAUDE.md`, `docs/REQUIREMENTS.md` (Frontend), `docs/DOMAIN.md` (lenguaje ubicuo), `docs/ARCHITECTURE.md` §3, `docs/CONVENTIONS.md` §3 (reactividad obligatoria), decisiones de frontend en `docs/PROGRESS.md` (2026-10-11), contrato HTTP del backend en `README.md` § Ejecutar el backend.

## Global Constraints

- Directorio del proyecto: `frontend/`. Rama: `feat/frontend` (Task 1), merge vía PR con aprobación del usuario.
- **npm/ng se ejecutan con la herramienta PowerShell** (en Git Bash el shim de nvm falla con "Could not determine Node.js install directory"). Anteponer en cada comando: `$env:Path = "C:\nvm4w\nodejs;" + $env:Path; $env:NG_CLI_ANALYTICS='false'; Set-Location C:\Users\ribux\OneDrive\Escritorio\prueba-domonow\frontend`.
- **Reactividad obligatoria** (CONVENTIONS §3): estado en `signal`, derivados en `computed`, `input()/output()/model()`, `ChangeDetectionStrategy.OnPush` en todos los componentes, app zoneless, RxJS → signals, Signal Forms, `effect()` solo para efectos de borde, ningún `subscribe()` sin `takeUntilDestroyed()` (para respuestas únicas usar `firstValueFrom`).
- Lenguaje ubicuo del backend en tipos TS: `ParkingSpot`, `ParkingSpotStatus` (`'Available' | 'Occupied' | 'OutOfService'`), `ActiveAssignment`, `checkIn`, `checkOut`, `licensePlate`, `visitorName`, `destinationUnit`, `checkedInAt`.
- Textos de UI en español; código en inglés. Los mensajes de error del backend (`detail`) se muestran tal cual.
- Archivos: `spot-card.ts` (clase `SpotCard`), `parking-board-page.ts` (`ParkingBoardPage`), `parking.store.ts`, `parking.repository.ts`, `parking-http.repository.ts`.
- `strict: true` y `strictTemplates: true` explícitos; prohibido `any`.
- API base URL: `http://localhost:5080` (CORS del backend ya permite `http://localhost:4200`).
- Decisiones del usuario: Angular Material; refresco tras cada acción + polling 30 s pausado con la pestaña oculta; filtro por estado con contadores; lista de vehículos dentro; detalle (placa, visitante, unidad, hora y tiempo transcurrido) en la tarjeta ocupada; salida con hora del servidor (sin campo de hora).
- Antes de cada commit: `npm run format` (Prettier, `printWidth: 100`; los bloques de código de este plan pueden exceder ese ancho y Prettier los reacomoda).
- Cada task termina con `npm test`, `npm run lint` y `npx ng build` en verde. Commits Conventional Commits en inglés con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Tras cada task: entrada en `docs/PROGRESS.md`; si hubo decisión sobre una propuesta de la IA → `AI-USAGE.md`.

## Review Focus

1. **Doble clic / doble envío** en "Registrar ingreso" o "Registrar salida" → un solo request en vuelo por parqueadero (botón deshabilitado mientras `busy`), y el 409 del perdedor se muestra como mensaje, no como fallo genérico. (Task 6 `busy`, Task 8.)
2. **Token vencido a mitad de uso** (60 min) → el siguiente 401 cierra la sesión, lleva a `/login?reason=expired` y muestra el aviso; nunca queda la pantalla "colgada". (Task 3 interceptor, Task 4 login.)
3. **API caída o lenta durante el polling** → se mantiene el último tablero válido con un aviso y botón "Reintentar"; no se vacía la pantalla ni aparecen errores en cascada cada 30 s. (Task 6 `keeps showing the last data…`.)
4. **Placa escrita con minúsculas, guiones, espacios o puntos** (`" abc-123 "`, `"abc.12d"`) → aceptada en el formulario con vista previa "Se registrará como ABC123"; formatos inválidos bloqueados en cliente y, si pasan, el 400 del backend se muestra en el diálogo. (Task 5 `license-plate`, Task 8 diálogo.)
5. **Otro operador ocupó el parqueadero mientras el diálogo estaba abierto** → el 409 se muestra en el diálogo (que sigue abierto) y el tablero se refresca. (Task 6 refresh en `finally`, Task 8 test de error.)

---

## File Structure

```
frontend/
├── angular.json · package.json · tsconfig*.json · eslint.config.js · .prettierrc
└── src/
    ├── index.html · main.ts · styles.scss
    ├── environments/environment.ts
    └── app/
        ├── app.ts · app.html · app.scss · app.config.ts · app.routes.ts
        ├── core/
        │   ├── config/api-base-url.ts
        │   ├── http/api-error.ts (+ .spec) · api-error.interceptor.ts
        │   ├── auth/session.store.ts (+ .spec) · auth-api.ts · auth.interceptor.ts (+ .spec, cubre ambos interceptores)
        │   │        auth.guard.ts · login-page.ts/.html/.scss (+ .spec)
        │   └── notifications/notifier.ts
        ├── shared/
        │   ├── time/format-elapsed.ts (+ .spec)
        │   └── ui/confirm-dialog.ts
        └── features/parking/
            ├── domain/parking.models.ts · license-plate.ts (+ .spec) · parking.repository.ts
            ├── infrastructure/parking-api.dto.ts · parking.mapper.ts (+ .spec) · parking-http.repository.ts (+ .spec)
            ├── application/parking.store.ts (+ .spec)
            └── ui/
                ├── components/spot-card.ts/.html/.scss (+ .spec) · status-filter.ts · active-vehicles-table.ts/.html
                ├── dialogs/check-in-dialog.ts/.html (+ .spec)
                └── pages/parking-board-page.ts/.html/.scss
```

---

### Task 1: Scaffold Angular 22 + Material + ESLint

**Files:** Create `frontend/*` (CLI), `frontend/src/environments/environment.ts`. Modify `tsconfig.json`, `src/index.html`, `src/styles.scss`, `package.json`, `src/app/app.config.ts`.

**Interfaces:** Produces a building app with `ng test`, `ng lint`, `ng build` green; `environment.apiBaseUrl`.

- [ ] **Step 1: Branch and generate** (Bash for git, PowerShell for ng)

```bash
git checkout -b feat/frontend
mv frontend/.prettierrc /tmp/.prettierrc.keep
```
PowerShell (from repo root):
```powershell
npx ng new domonow-parking --directory frontend --skip-git --style=scss --routing --ssr=false --defaults
```
Then (Bash) `mv /tmp/.prettierrc.keep frontend/.prettierrc` and delete the generated `frontend/.editorconfig` if present (root `.editorconfig` governs).

- [ ] **Step 2: Strictness** — in `frontend/tsconfig.json` add to `compilerOptions`: `"strict": true` and to `angularCompilerOptions`: `"strictTemplates": true`.

- [ ] **Step 3: Material + ESLint**

```powershell
npm install @angular/material@22.2.2 @angular/cdk@22.2.2
npx ng add angular-eslint@22.6.0 --skip-confirmation
```

- [ ] **Step 4: Theme, fonts, locale, language**

`src/styles.scss`:
```scss
@use '@angular/material' as mat;

html {
  color-scheme: light;
  @include mat.theme((color: mat.$azure-palette, typography: Roboto, density: 0));
}

:root {
  --status-available: #1a7f37;
  --status-available-bg: #e8f5ec;
  --status-occupied: #b42318;
  --status-occupied-bg: #fdeceb;
  --status-out-of-service: #5f6368;
  --status-out-of-service-bg: #eef0f2;
}

html,
body {
  height: 100%;
}

body {
  margin: 0;
  font-family: Roboto, 'Helvetica Neue', sans-serif;
  background: var(--mat-sys-surface-container-low);
  color: var(--mat-sys-on-surface);
}

.notifier--error {
  --mat-snack-bar-container-color: var(--status-occupied);
  --mat-snack-bar-supporting-text-color: #fff;
  --mat-snack-bar-button-color: #fff;
}
```
`src/index.html`: `<html lang="es">`, `<title>DomoNow · Parqueaderos de visitantes</title>`, and in `<head>`:
```html
<link rel="preconnect" href="https://fonts.gstatic.com" />
<link href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap" rel="stylesheet" />
```

- [ ] **Step 5: Environment** — `src/environments/environment.ts`:
```ts
export const environment = {
  apiBaseUrl: 'http://localhost:5080',
};
```

- [ ] **Step 6: Scripts** — `package.json` scripts: `"test": "ng test --watch=false"`, `"test:watch": "ng test"`, `"lint": "ng lint"`, `"format": "prettier --write \"src/**/*.{ts,html,scss}\""`, `"format:check": "prettier --check \"src/**/*.{ts,html,scss}\""`.

- [ ] **Step 7: Verify**

Run (PowerShell): `npm test`, `npm run lint`, `npx ng build`.
Expected: 2 generated tests pass, lint clean, build succeeds.

- [ ] **Step 8: Commit**

```bash
git add frontend .gitignore
git commit -m "chore(frontend): scaffold Angular 22 app with Material, ESLint and strict mode" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: HTTP error model + API base URL

**Files:** Create `core/config/api-base-url.ts`, `core/http/api-error.ts`, `core/http/api-error.spec.ts`.

**Interfaces:** Produces `API_BASE_URL: InjectionToken<string>`, `interface ApiError { status: number; code: string | null; message: string }`, `toApiError(error: unknown): ApiError`, `isApiError(value: unknown): value is ApiError`, `GENERIC_ERROR_MESSAGE`.

- [ ] **Step 1: Failing test** — `src/app/core/http/api-error.spec.ts`

```ts
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
    expect(toApiError(new HttpErrorResponse({ status: 429 })).message).toContain('Demasiados intentos');
  });

  it('explains an expired session for a 401 without detail', () => {
    expect(toApiError(new HttpErrorResponse({ status: 401, error: { title: 'Unauthorized' } })).message).toContain(
      'sesión expiró',
    );
  });

  it('returns a generic message for non-HTTP errors', () => {
    expect(toApiError(new Error('boom'))).toEqual({ status: -1, code: null, message: GENERIC_ERROR_MESSAGE });
  });

  it('returns ApiError values unchanged', () => {
    const apiError = { status: 422, code: 'Parking.SpotOutOfService', message: 'Fuera de servicio.' };

    expect(toApiError(apiError)).toBe(apiError);
  });
});
```

- [ ] **Step 2: Run → RED** (`npm test`) — fails: module `./api-error` not found.

- [ ] **Step 3: Implement**

`src/app/core/config/api-base-url.ts`:
```ts
import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  factory: () => environment.apiBaseUrl,
});
```

`src/app/core/http/api-error.ts`:
```ts
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
  return typeof value === 'object' && value !== null && 'status' in value && 'code' in value && 'message' in value;
}

export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (!(error instanceof HttpErrorResponse)) {
    return { status: -1, code: null, message: GENERIC_ERROR_MESSAGE };
  }

  const body: ProblemDetailsBody = typeof error.error === 'object' && error.error !== null ? error.error : {};
  const detail = typeof body.detail === 'string' && body.detail.trim() !== '' ? body.detail : null;

  return {
    status: error.status,
    code: typeof body.code === 'string' ? body.code : null,
    message: detail ?? FALLBACK_MESSAGES[error.status] ?? GENERIC_ERROR_MESSAGE,
  };
}
```

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add API error model mapping ProblemDetails to UI messages`.

---

### Task 3: Session, auth API, interceptors, guards

**Files:** Create `core/auth/session.store.ts`, `session.store.spec.ts`, `auth-api.ts`, `auth.interceptor.ts`, `auth.interceptor.spec.ts`, `auth.guard.ts`, `core/http/api-error.interceptor.ts`.

**Interfaces:**
- Consumes `API_BASE_URL`, `toApiError`.
- Produces `interface Session { accessToken: string; username: string; expiresAt: string }`, `SessionStore` (root) with signals `session`, computed `isAuthenticated`, `accessToken`, `username`, methods `start(session)`, `end()`; `SESSION_STORAGE` token; `AuthApi.login(username, password): Observable<Session>`; `LOGIN_PATH = '/api/auth/login'`; `authInterceptor`, `apiErrorInterceptor` (`HttpInterceptorFn`); `authGuard`, `guestGuard` (`CanActivateFn`).

- [ ] **Step 1: Failing tests**

`src/app/core/auth/session.store.spec.ts`:
```ts
import { TestBed } from '@angular/core/testing';
import { Session, SessionStore } from './session.store';

const STORAGE_KEY = 'domonow.session';
const inOneHour = (): string => new Date(Date.now() + 3_600_000).toISOString();
const session = (): Session => ({ accessToken: 'token-123', username: 'portero', expiresAt: inOneHour() });

describe('SessionStore', () => {
  beforeEach(() => sessionStorage.clear());

  it('starts unauthenticated when nothing is stored', () => {
    const store = TestBed.inject(SessionStore);

    expect(store.isAuthenticated()).toBe(false);
    expect(store.accessToken()).toBeNull();
  });

  it('persists the session in sessionStorage when it starts', () => {
    const store = TestBed.inject(SessionStore);
    const started = session();

    store.start(started);
    TestBed.tick();

    expect(store.isAuthenticated()).toBe(true);
    expect(store.username()).toBe('portero');
    expect(JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? 'null')).toEqual(started);
  });

  it('restores a stored session that has not expired', () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session()));

    expect(TestBed.inject(SessionStore).accessToken()).toBe('token-123');
  });

  it('discards a stored session that already expired', () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ ...session(), expiresAt: '2000-01-01T00:00:00Z' }));

    expect(TestBed.inject(SessionStore).isAuthenticated()).toBe(false);
  });

  it('clears the session and the storage when it ends', () => {
    const store = TestBed.inject(SessionStore);
    store.start(session());
    TestBed.tick();

    store.end();
    TestBed.tick();

    expect(store.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull();
  });
});
```

`src/app/core/auth/auth.interceptor.spec.ts` (covers `authInterceptor` + `apiErrorInterceptor`):
```ts
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
    session.start({ accessToken: 'token-123', username: 'portero', expiresAt: new Date(Date.now() + 3_600_000).toISOString() });
  });

  afterEach(() => controller.verify());

  it('adds the bearer token to API requests', () => {
    http.get(`${API}/api/parking-spots`).subscribe();

    expect(controller.expectOne(`${API}/api/parking-spots`).request.headers.get('Authorization')).toBe('Bearer token-123');
  });

  it('does not send the token to other hosts', () => {
    http.get('https://fonts.example.com/font.css').subscribe();

    expect(controller.expectOne('https://fonts.example.com/font.css').request.headers.has('Authorization')).toBe(false);
  });

  it('ends the session and redirects to login when the API answers 401', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const response = firstValueFrom(http.get(`${API}/api/parking-spots`));

    controller.expectOne(`${API}/api/parking-spots`).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toMatchObject({ status: 401 } satisfies Partial<ApiError>);
    expect(session.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { reason: 'expired' } });
  });

  it('keeps the session when the login request itself answers 401', async () => {
    const response = firstValueFrom(http.post(`${API}${LOGIN_PATH}`, {}));

    controller
      .expectOne(`${API}${LOGIN_PATH}`)
      .flush({ detail: 'Usuario o contraseña incorrectos.', code: 'Auth.InvalidCredentials' }, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toMatchObject({ code: 'Auth.InvalidCredentials' });
    expect(session.isAuthenticated()).toBe(true);
  });
});
```

- [ ] **Step 2: Run → RED** (modules missing).

- [ ] **Step 3: Implement**

`src/app/core/auth/session.store.ts`:
```ts
import { computed, effect, Injectable, InjectionToken, inject, signal } from '@angular/core';

export interface Session {
  readonly accessToken: string;
  readonly username: string;
  readonly expiresAt: string;
}

export const SESSION_STORAGE = new InjectionToken<Storage>('SESSION_STORAGE', {
  factory: () => sessionStorage,
});

const STORAGE_KEY = 'domonow.session';

@Injectable({ providedIn: 'root' })
export class SessionStore {
  readonly #storage = inject(SESSION_STORAGE);
  readonly #session = signal<Session | null>(this.#restore());

  readonly session = this.#session.asReadonly();
  readonly isAuthenticated = computed(() => this.#session() !== null);
  readonly accessToken = computed(() => this.#session()?.accessToken ?? null);
  readonly username = computed(() => this.#session()?.username ?? null);

  constructor() {
    // Efecto de borde: refleja la sesión en sessionStorage (sobrevive a recargar la página, no a cerrar la pestaña).
    effect(() => {
      const current = this.#session();
      if (current) {
        this.#storage.setItem(STORAGE_KEY, JSON.stringify(current));
      } else {
        this.#storage.removeItem(STORAGE_KEY);
      }
    });
  }

  start(session: Session): void {
    this.#session.set(session);
  }

  end(): void {
    this.#session.set(null);
  }

  #restore(): Session | null {
    try {
      const parsed: unknown = JSON.parse(this.#storage.getItem(STORAGE_KEY) ?? 'null');
      if (!isSession(parsed) || Date.parse(parsed.expiresAt) <= Date.now()) {
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }
}

function isSession(value: unknown): value is Session {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate['accessToken'] === 'string' &&
    typeof candidate['username'] === 'string' &&
    typeof candidate['expiresAt'] === 'string'
  );
}
```

`src/app/core/auth/auth-api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../config/api-base-url';
import { Session } from './session.store';

export const LOGIN_PATH = '/api/auth/login';

interface LoginResponseDto {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly username: string;
  readonly role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApi {
  readonly #http = inject(HttpClient);
  readonly #baseUrl = inject(API_BASE_URL);

  login(username: string, password: string): Observable<Session> {
    return this.#http
      .post<LoginResponseDto>(`${this.#baseUrl}${LOGIN_PATH}`, { username, password })
      .pipe(map((response) => ({ accessToken: response.accessToken, username: response.username, expiresAt: response.expiresAt })));
  }
}
```

`src/app/core/auth/auth.interceptor.ts`:
```ts
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
```

`src/app/core/http/api-error.interceptor.ts`:
```ts
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
```

`src/app/core/auth/auth.guard.ts`:
```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

export const authGuard: CanActivateFn = () =>
  inject(SessionStore).isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);

export const guestGuard: CanActivateFn = () =>
  inject(SessionStore).isAuthenticated() ? inject(Router).createUrlTree(['/']) : true;
```

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add reactive session store, JWT interceptors and route guards`.

---

### Task 4: Login page, app shell, routing, providers

**Files:** Create `core/auth/login-page.ts/.html/.scss`, `login-page.spec.ts`, `core/notifications/notifier.ts`. Modify `app.ts/.html/.scss`, `app.routes.ts`, `app.config.ts`, delete generated `app.spec.ts` (replaced by feature tests).

**Interfaces:** Consumes `AuthApi`, `SessionStore`, guards, interceptors, `toApiError`. Produces route `/login`, shell with toolbar + logout, `Notifier { success(message): void; error(message): void }`, `appConfig` with zoneless, router, HttpClient + interceptors, `LOCALE_ID = 'es-CO'`.

- [ ] **Step 1: Failing test** — `src/app/core/auth/login-page.spec.ts`

```ts
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AuthApi } from './auth-api';
import { LoginPage } from './login-page';
import { SessionStore } from './session.store';

describe('LoginPage', () => {
  const login = vi.fn();

  beforeEach(() => {
    sessionStorage.clear();
    login.mockReset();
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthApi, useValue: { login } }],
    });
  });

  async function submitWith(username: string, password: string): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const [userInput, passwordInput] = Array.from(element.querySelectorAll('input'));
    userInput.value = username;
    userInput.dispatchEvent(new Event('input'));
    passwordInput.value = password;
    passwordInput.dispatchEvent(new Event('input'));
    element.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    return element;
  }

  it('does not call the API while fields are empty', async () => {
    await submitWith('', '');

    expect(login).not.toHaveBeenCalled();
  });

  it('shows the backend message when credentials are wrong', async () => {
    login.mockReturnValue(throwError(() => ({ status: 401, code: 'Auth.InvalidCredentials', message: 'Usuario o contraseña incorrectos.' })));

    const element = await submitWith('portero', 'wrong');

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Usuario o contraseña incorrectos.');
  });

  it('starts the session and goes to the board on success', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    login.mockReturnValue(of({ accessToken: 't', username: 'portero', expiresAt: new Date(Date.now() + 60_000).toISOString() }));

    await submitWith('portero', 'secret');

    expect(TestBed.inject(SessionStore).username()).toBe('portero');
    expect(navigate).toHaveBeenCalledWith(['/']);
  });
});
```

- [ ] **Step 2: Run → RED.**

- [ ] **Step 3: Implement**

`src/app/core/auth/login-page.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom, map } from 'rxjs';
import { toApiError } from '../http/api-error';
import { AuthApi } from './auth-api';
import { SessionStore } from './session.store';

interface Credentials {
  username: string;
  password: string;
}

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  readonly #authApi = inject(AuthApi);
  readonly #session = inject(SessionStore);
  readonly #router = inject(Router);

  protected readonly credentials = signal<Credentials>({ username: '', password: '' });
  protected readonly loginForm = form(this.credentials, (path) => {
    required(path.username, { message: 'Ingrese su usuario.' });
    required(path.password, { message: 'Ingrese su contraseña.' });
  });
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly sessionExpired = toSignal(
    inject(ActivatedRoute).queryParamMap.pipe(map((params) => params.get('reason') === 'expired')),
    { initialValue: false },
  );

  protected async login(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.loginForm, async () => {
      this.submitting.set(true);
      this.serverError.set(null);
      try {
        const { username, password } = this.credentials();
        this.#session.start(await firstValueFrom(this.#authApi.login(username.trim(), password)));
        await this.#router.navigate(['/']);
      } catch (error) {
        this.serverError.set(toApiError(error).message);
      } finally {
        this.submitting.set(false);
      }
      return undefined;
    });
  }
}
```

`src/app/core/auth/login-page.html`:
```html
<section class="login">
  <mat-card class="login__card" appearance="outlined">
    <mat-card-header>
      <mat-card-title>DomoNow</mat-card-title>
      <mat-card-subtitle>Parqueaderos de visitantes</mat-card-subtitle>
    </mat-card-header>

    <form (submit)="login($event)" novalidate>
      <mat-card-content class="login__content">
        @if (sessionExpired()) {
          <p class="login__notice" role="status">Su sesión expiró. Inicie sesión de nuevo.</p>
        }

        <mat-form-field appearance="outline">
          <mat-label>Usuario</mat-label>
          <input matInput [formField]="loginForm.username" autocomplete="username" />
          @for (error of loginForm.username().errors(); track $index) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Contraseña</mat-label>
          <input matInput type="password" [formField]="loginForm.password" autocomplete="current-password" />
          @for (error of loginForm.password().errors(); track $index) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>

        @if (serverError(); as message) {
          <p class="login__error" role="alert">{{ message }}</p>
        }
      </mat-card-content>

      <mat-card-actions class="login__actions">
        <button mat-flat-button type="submit" [disabled]="submitting()">Ingresar</button>
      </mat-card-actions>
      @if (submitting()) {
        <mat-progress-bar mode="indeterminate" />
      }
    </form>
  </mat-card>
</section>
```

`src/app/core/auth/login-page.scss`:
```scss
.login {
  display: grid;
  place-items: center;
  min-height: calc(100vh - 64px);
  padding: 1rem;
}

.login__card {
  width: min(100%, 24rem);
}

.login__content {
  display: grid;
  gap: 0.25rem;
  padding-top: 1rem;
}

.login__actions {
  justify-content: flex-end;
}

.login__notice,
.login__error {
  margin: 0 0 0.5rem;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
}

.login__notice {
  background: var(--mat-sys-secondary-container);
}

.login__error {
  background: var(--status-occupied-bg);
  color: var(--status-occupied);
}
```

`src/app/core/notifications/notifier.ts`:
```ts
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class Notifier {
  readonly #snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.#snackBar.open(message, 'Cerrar', { duration: 4000 });
  }

  error(message: string): void {
    this.#snackBar.open(message, 'Cerrar', { duration: 6000, panelClass: 'notifier--error', politeness: 'assertive' });
  }
}
```

`src/app/app.config.ts`:
```ts
import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeEsCo from '@angular/common/locales/es-CO';
import {
  ApplicationConfig,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { apiErrorInterceptor } from './core/http/api-error.interceptor';

registerLocaleData(localeEsCo);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, apiErrorInterceptor])),
    { provide: LOCALE_ID, useValue: 'es-CO' },
  ],
};
```

`src/app/app.routes.ts` (board route added in Task 8):
```ts
import { Routes } from '@angular/router';
import { guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Ingresar · DomoNow',
    loadComponent: () => import('./core/auth/login-page').then((m) => m.LoginPage),
  },
  { path: '**', redirectTo: 'login' },
];
```

`src/app/app.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterOutlet } from '@angular/router';
import { SessionStore } from './core/auth/session.store';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatToolbarModule, MatButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly session = inject(SessionStore);
  readonly #router = inject(Router);

  protected async logout(): Promise<void> {
    this.session.end();
    await this.#router.navigate(['/login']);
  }
}
```

`src/app/app.html`:
```html
<mat-toolbar class="app-toolbar">
  <span class="app-toolbar__brand">DomoNow · Parqueaderos</span>
  <span class="app-toolbar__spacer"></span>
  @if (session.username(); as username) {
    <span class="app-toolbar__user">{{ username }}</span>
    <button mat-button type="button" (click)="logout()">Cerrar sesión</button>
  }
</mat-toolbar>
<main class="app-content">
  <router-outlet />
</main>
```

`src/app/app.scss`:
```scss
.app-toolbar {
  position: sticky;
  top: 0;
  z-index: 2;
  gap: 0.75rem;
}

.app-toolbar__brand {
  font-weight: 500;
}

.app-toolbar__spacer {
  flex: 1;
}

.app-toolbar__user {
  font-size: 0.875rem;
  opacity: 0.8;
}
```

Delete `src/app/app.spec.ts` (generated "Hello" test no longer valid).

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add signal-forms login page, app shell and HTTP providers`.

---

### Task 5: Parking domain, DTO mapping and HTTP adapter

**Files:** Create `features/parking/domain/parking.models.ts`, `license-plate.ts`, `license-plate.spec.ts`, `parking.repository.ts`; `infrastructure/parking-api.dto.ts`, `parking.mapper.ts`, `parking.mapper.spec.ts`, `parking-http.repository.ts`, `parking-http.repository.spec.ts`.

**Interfaces:** Produces `ParkingSpotStatus`, `SpotFilter`, `ParkingSpot`, `ActiveAssignment`, `CheckInRequest`, `STATUS_LABELS`, `canCheckIn(spot)`, `canCheckOut(spot)`, `normalizePlate(raw)`, `isValidPlate(raw)`, abstract `ParkingRepository { getSpots(); getActiveAssignments(); checkIn(request): Observable<ActiveAssignment>; checkOut(assignmentId): Observable<void> }`, `ParkingHttpRepository`.

- [ ] **Step 1: Failing tests**

`domain/license-plate.spec.ts`:
```ts
import { isValidPlate, normalizePlate } from './license-plate';

describe('license plate rules (UX mirror of the backend value object)', () => {
  it.each([
    [' abc-123 ', 'ABC123'],
    ['abc.12d', 'ABC12D'],
    ['ABC 123', 'ABC123'],
  ])('normalizes %j to %s', (raw, expected) => {
    expect(normalizePlate(raw)).toBe(expected);
    expect(isValidPlate(raw)).toBe(true);
  });

  it.each(['', 'AB-12', 'ABCD123', '123ABC', 'ABC1234'])('rejects %j', (raw) => {
    expect(isValidPlate(raw)).toBe(false);
  });
});
```

`infrastructure/parking.mapper.spec.ts`:
```ts
import { toActiveAssignment, toParkingSpot } from './parking.mapper';

describe('parking mapper', () => {
  const assignmentDto = {
    assignmentId: 'a-1',
    parkingSpotId: 's-2',
    spotCode: 'P02',
    licensePlate: 'ABC123',
    visitorName: 'Carlos Pérez',
    destinationUnit: '302',
    checkedInAt: '2026-10-11T15:00:00Z',
  };

  it('converts the check-in time to a Date', () => {
    expect(toActiveAssignment(assignmentDto).checkedInAt).toEqual(new Date('2026-10-11T15:00:00Z'));
  });

  it('maps an occupied spot with its active assignment', () => {
    const spot = toParkingSpot({ id: 's-2', code: 'P02', status: 'Occupied', isAvailable: false, activeAssignment: assignmentDto });

    expect(spot).toEqual({ id: 's-2', code: 'P02', status: 'Occupied', activeAssignment: toActiveAssignment(assignmentDto) });
  });

  it('maps a spot without assignment to null', () => {
    expect(toParkingSpot({ id: 's-1', code: 'P01', status: 'Available', isAvailable: true, activeAssignment: null }).activeAssignment).toBeNull();
  });
});
```

`infrastructure/parking-http.repository.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-base-url';
import { ParkingHttpRepository } from './parking-http.repository';

describe('ParkingHttpRepository', () => {
  let repository: ParkingHttpRepository;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ParkingHttpRepository, provideHttpClient(), provideHttpClientTesting(), { provide: API_BASE_URL, useValue: 'http://api.test' }],
    });
    repository = TestBed.inject(ParkingHttpRepository);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('posts the check-in contract expected by the API', async () => {
    const request = { parkingSpotId: 's-1', licensePlate: 'abc-123', visitorName: 'Ana', destinationUnit: '101' };
    const result = firstValueFrom(repository.checkIn(request));

    const call = controller.expectOne('http://api.test/api/parking-assignments');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(request);
    call.flush({ assignmentId: 'a-1', parkingSpotId: 's-1', spotCode: 'P01', licensePlate: 'ABC123', visitorName: 'Ana', destinationUnit: '101', checkedInAt: '2026-10-11T15:00:00Z' });

    expect((await result).licensePlate).toBe('ABC123');
  });

  it('checks out with PUT and an empty JSON body so the server sets the time', async () => {
    const result = firstValueFrom(repository.checkOut('a-1'));

    const call = controller.expectOne('http://api.test/api/parking-assignments/a-1/checkout');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush({});

    await expect(result).resolves.toBeUndefined();
  });
});
```

- [ ] **Step 2: Run → RED.**

- [ ] **Step 3: Implement**

`domain/parking.models.ts`:
```ts
export type ParkingSpotStatus = 'Available' | 'Occupied' | 'OutOfService';
export type SpotFilter = 'All' | ParkingSpotStatus;
export type SpotCounts = Readonly<Record<SpotFilter, number>>;

export interface ActiveAssignment {
  readonly assignmentId: string;
  readonly parkingSpotId: string;
  readonly spotCode: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
  readonly checkedInAt: Date;
}

export interface ParkingSpot {
  readonly id: string;
  readonly code: string;
  readonly status: ParkingSpotStatus;
  readonly activeAssignment: ActiveAssignment | null;
}

export interface CheckInRequest {
  readonly parkingSpotId: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
}

export const STATUS_LABELS: Readonly<Record<ParkingSpotStatus, string>> = {
  Available: 'Disponible',
  Occupied: 'Ocupado',
  OutOfService: 'Fuera de servicio',
};

/** Reglas de presentación: la autoridad sigue siendo el agregado del backend. */
export function canCheckIn(spot: ParkingSpot): boolean {
  return spot.status === 'Available';
}

export function canCheckOut(spot: ParkingSpot): boolean {
  return spot.status === 'Occupied' && spot.activeAssignment !== null;
}
```

`domain/license-plate.ts`:
```ts
// Espejo de UX del value object LicensePlate del backend (que sigue siendo la autoridad).
const PLATE_FORMAT = /^[A-Z]{3}(\d{3}|\d{2}[A-Z])$/;

export function normalizePlate(raw: string): string {
  return raw.replace(/[^a-z0-9]/gi, '').toUpperCase();
}

export function isValidPlate(raw: string): boolean {
  return PLATE_FORMAT.test(normalizePlate(raw));
}
```

`domain/parking.repository.ts`:
```ts
import { Observable } from 'rxjs';
import { ActiveAssignment, CheckInRequest, ParkingSpot } from './parking.models';

/** Puerto de salida del feature. La implementación HTTP se enlaza en los providers de la ruta. */
export abstract class ParkingRepository {
  abstract getSpots(): Observable<ParkingSpot[]>;
  abstract getActiveAssignments(): Observable<ActiveAssignment[]>;
  abstract checkIn(request: CheckInRequest): Observable<ActiveAssignment>;
  abstract checkOut(assignmentId: string): Observable<void>;
}
```

`infrastructure/parking-api.dto.ts`:
```ts
import { ParkingSpotStatus } from '../domain/parking.models';

export interface ActiveAssignmentDto {
  readonly assignmentId: string;
  readonly parkingSpotId: string;
  readonly spotCode: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
  readonly checkedInAt: string;
}

export interface ParkingSpotDto {
  readonly id: string;
  readonly code: string;
  readonly status: ParkingSpotStatus;
  readonly isAvailable: boolean;
  readonly activeAssignment: ActiveAssignmentDto | null;
}

/** La respuesta del check-in tiene la misma forma que una asignación activa. */
export type CheckInResponseDto = ActiveAssignmentDto;
```

`infrastructure/parking.mapper.ts`:
```ts
import { ActiveAssignment, ParkingSpot } from '../domain/parking.models';
import { ActiveAssignmentDto, ParkingSpotDto } from './parking-api.dto';

export function toActiveAssignment(dto: ActiveAssignmentDto): ActiveAssignment {
  return {
    assignmentId: dto.assignmentId,
    parkingSpotId: dto.parkingSpotId,
    spotCode: dto.spotCode,
    licensePlate: dto.licensePlate,
    visitorName: dto.visitorName,
    destinationUnit: dto.destinationUnit,
    checkedInAt: new Date(dto.checkedInAt),
  };
}

export function toParkingSpot(dto: ParkingSpotDto): ParkingSpot {
  return {
    id: dto.id,
    code: dto.code,
    status: dto.status,
    activeAssignment: dto.activeAssignment ? toActiveAssignment(dto.activeAssignment) : null,
  };
}
```

`infrastructure/parking-http.repository.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-base-url';
import { ActiveAssignment, CheckInRequest, ParkingSpot } from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';
import { ActiveAssignmentDto, CheckInResponseDto, ParkingSpotDto } from './parking-api.dto';
import { toActiveAssignment, toParkingSpot } from './parking.mapper';

@Injectable()
export class ParkingHttpRepository extends ParkingRepository {
  readonly #http = inject(HttpClient);
  readonly #baseUrl = inject(API_BASE_URL);

  getSpots(): Observable<ParkingSpot[]> {
    return this.#http
      .get<ParkingSpotDto[]>(`${this.#baseUrl}/api/parking-spots`)
      .pipe(map((spots) => spots.map(toParkingSpot)));
  }

  getActiveAssignments(): Observable<ActiveAssignment[]> {
    return this.#http
      .get<ActiveAssignmentDto[]>(`${this.#baseUrl}/api/parking-assignments/active`)
      .pipe(map((assignments) => assignments.map(toActiveAssignment)));
  }

  checkIn(request: CheckInRequest): Observable<ActiveAssignment> {
    return this.#http
      .post<CheckInResponseDto>(`${this.#baseUrl}/api/parking-assignments`, request)
      .pipe(map(toActiveAssignment));
  }

  checkOut(assignmentId: string): Observable<void> {
    // Cuerpo vacío: la hora de salida la pone el servidor.
    return this.#http
      .put<unknown>(`${this.#baseUrl}/api/parking-assignments/${encodeURIComponent(assignmentId)}/checkout`, {})
      .pipe(map(() => undefined));
  }
}
```

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add parking domain model, repository port and HTTP adapter`.

---

### Task 6: Reactive `ParkingStore` (state, polling, actions)

**Files:** Create `features/parking/application/parking.store.ts`, `parking.store.spec.ts`.

**Interfaces:** Consumes `ParkingRepository`, domain types, `toApiError`, `ApiError`. Produces `POLL_INTERVAL_MS = 30_000`, `type ActionResult = { ok: true } | { ok: false; error: ApiError }`, `ParkingStore` (component-scoped) with readonly signals `spots`, `activeAssignments`, `loadState` (`'loading' | 'ready' | 'error'`), `loadError`, `busySpotIds`, `lastUpdated`, `now`; writable `filter: WritableSignal<SpotFilter>`; computed `counts: SpotCounts`, `visibleSpots`, `isInitialLoading`; methods `refresh()`, `checkIn(request): Promise<ActionResult>`, `checkOut(spotId, assignmentId): Promise<ActionResult>`.

- [ ] **Step 1: Failing test** — `parking.store.spec.ts`

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ActiveAssignment, CheckInRequest, ParkingSpot, ParkingSpotStatus } from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';
import { POLL_INTERVAL_MS, ParkingStore } from './parking.store';

const assignment = (spotId: string, plate: string): ActiveAssignment => ({
  assignmentId: `a-${spotId}`,
  parkingSpotId: spotId,
  spotCode: spotId.toUpperCase(),
  licensePlate: plate,
  visitorName: 'Visitante',
  destinationUnit: '302',
  checkedInAt: new Date('2026-10-11T15:00:00Z'),
});

const spot = (id: string, status: ParkingSpotStatus): ParkingSpot => ({
  id,
  code: id.toUpperCase(),
  status,
  activeAssignment: status === 'Occupied' ? assignment(id, 'XYZ987') : null,
});

class FakeParkingRepository extends ParkingRepository {
  spots: ParkingSpot[] = [spot('p01', 'Available'), spot('p02', 'Occupied'), spot('p04', 'OutOfService')];
  spotsResponse: () => Observable<ParkingSpot[]> = () => of(this.spots);
  checkInResponse: Observable<ActiveAssignment> = of(assignment('p01', 'ABC123'));
  getSpotsCalls = 0;

  getSpots(): Observable<ParkingSpot[]> {
    this.getSpotsCalls++;
    return this.spotsResponse();
  }

  getActiveAssignments(): Observable<ActiveAssignment[]> {
    return of(this.spots.flatMap((s) => (s.activeAssignment ? [s.activeAssignment] : [])));
  }

  checkIn(): Observable<ActiveAssignment> {
    return this.checkInResponse;
  }

  checkOut(): Observable<void> {
    return of(undefined);
  }
}

describe('ParkingStore', () => {
  let repository: FakeParkingRepository;
  let store: ParkingStore;
  let visibility: DocumentVisibilityState;
  const request: CheckInRequest = { parkingSpotId: 'p01', licensePlate: 'abc-123', visitorName: 'Ana', destinationUnit: '101' };

  beforeEach(() => {
    vi.useFakeTimers();
    visibility = 'visible';
    Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => visibility });
    repository = new FakeParkingRepository();
    TestBed.configureTestingModule({ providers: [ParkingStore, { provide: ParkingRepository, useValue: repository }] });
    store = TestBed.inject(ParkingStore);
  });

  afterEach(() => {
    vi.useRealTimers();
    Reflect.deleteProperty(document, 'visibilityState');
  });

  const setVisibility = (state: DocumentVisibilityState): void => {
    visibility = state;
    document.dispatchEvent(new Event('visibilitychange'));
  };

  it('loads spots and vehicles inside as soon as it starts', async () => {
    await vi.advanceTimersByTimeAsync(0);

    expect(store.spots()).toHaveLength(3);
    expect(store.activeAssignments().map((a) => a.licensePlate)).toEqual(['XYZ987']);
    expect(store.loadState()).toBe('ready');
    expect(store.lastUpdated()).not.toBeNull();
  });

  it('derives counts and the visible spots from the selected filter', async () => {
    await vi.advanceTimersByTimeAsync(0);

    store.filter.set('Occupied');

    expect(store.counts()).toEqual({ All: 3, Available: 1, Occupied: 1, OutOfService: 1 });
    expect(store.visibleSpots().map((s) => s.code)).toEqual(['P02']);
  });

  it('polls again every 30 seconds', async () => {
    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);

    expect(repository.getSpotsCalls).toBe(2);
  });

  it('pauses polling while the tab is hidden and reloads when it is visible again', async () => {
    await vi.advanceTimersByTimeAsync(0);
    setVisibility('hidden');
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS * 3);
    expect(repository.getSpotsCalls).toBe(1);

    setVisibility('visible');
    await vi.advanceTimersByTimeAsync(0);

    expect(repository.getSpotsCalls).toBe(2);
  });

  it('keeps showing the last data with an error when a refresh fails', async () => {
    await vi.advanceTimersByTimeAsync(0);
    repository.spotsResponse = () => throwError(() => new HttpErrorResponse({ status: 0 }));

    store.refresh();
    await vi.advanceTimersByTimeAsync(0);

    expect(store.spots()).toHaveLength(3);
    expect(store.loadState()).toBe('ready');
    expect(store.loadError()?.message).toContain('No se pudo conectar');
  });

  it('marks the spot busy during check-in and refreshes afterwards', async () => {
    await vi.advanceTimersByTimeAsync(0);
    const response = new Subject<ActiveAssignment>();
    repository.checkInResponse = response;

    const pending = store.checkIn(request);
    expect(store.busySpotIds().has('p01')).toBe(true);

    response.next(assignment('p01', 'ABC123'));
    response.complete();
    await expect(pending).resolves.toEqual({ ok: true });
    await vi.advanceTimersByTimeAsync(0);

    expect(store.busySpotIds().has('p01')).toBe(false);
    expect(repository.getSpotsCalls).toBe(2);
  });

  it('returns the backend error when check-in is rejected', async () => {
    repository.checkInResponse = throwError(
      () => new HttpErrorResponse({ status: 409, error: { detail: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.', code: 'Parking.VehicleAlreadyInside' } }),
    );

    const result = await store.checkIn(request);

    expect(result).toEqual({
      ok: false,
      error: { status: 409, code: 'Parking.VehicleAlreadyInside', message: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.' },
    });
    expect(store.busySpotIds().size).toBe(0);
  });
});
```

- [ ] **Step 2: Run → RED.**

- [ ] **Step 3: Implement** — `parking.store.ts`

```ts
import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  distinctUntilChanged,
  firstValueFrom,
  forkJoin,
  fromEvent,
  map,
  merge,
  of,
  startWith,
  switchMap,
  timer,
} from 'rxjs';
import { ApiError, toApiError } from '../../../core/http/api-error';
import { ActiveAssignment, CheckInRequest, ParkingSpot, ParkingSpotStatus, SpotCounts, SpotFilter } from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';

export const POLL_INTERVAL_MS = 30_000;

export type ActionResult = { readonly ok: true } | { readonly ok: false; readonly error: ApiError };
type LoadState = 'loading' | 'ready' | 'error';
type Snapshot = { readonly ok: true; readonly spots: ParkingSpot[]; readonly active: ActiveAssignment[] } | { readonly ok: false; readonly error: ApiError };

/**
 * Estado reactivo del tablero. Se provee a nivel de página: vive y hace polling solo mientras la página existe.
 * Las cargas son un flujo RxJS (polling visible + refrescos manuales) cuyo último resultado se vuelca en signals.
 */
@Injectable()
export class ParkingStore {
  readonly #repository = inject(ParkingRepository);
  readonly #document = inject(DOCUMENT);
  readonly #refreshRequests = new Subject<void>();

  readonly #spots = signal<readonly ParkingSpot[]>([]);
  readonly #activeAssignments = signal<readonly ActiveAssignment[]>([]);
  readonly #loadState = signal<LoadState>('loading');
  readonly #loadError = signal<ApiError | null>(null);
  readonly #busySpotIds = signal<ReadonlySet<string>>(new Set());
  readonly #lastUpdated = signal<Date | null>(null);
  readonly #now = signal(Date.now());

  readonly filter = signal<SpotFilter>('All');
  readonly spots = this.#spots.asReadonly();
  readonly activeAssignments = this.#activeAssignments.asReadonly();
  readonly loadState = this.#loadState.asReadonly();
  readonly loadError = this.#loadError.asReadonly();
  readonly busySpotIds = this.#busySpotIds.asReadonly();
  readonly lastUpdated = this.#lastUpdated.asReadonly();
  readonly now = this.#now.asReadonly();

  readonly counts = computed<SpotCounts>(() => {
    const spots = this.#spots();
    const countOf = (status: ParkingSpotStatus): number => spots.filter((spot) => spot.status === status).length;
    return { All: spots.length, Available: countOf('Available'), Occupied: countOf('Occupied'), OutOfService: countOf('OutOfService') };
  });

  readonly visibleSpots = computed(() => {
    const filter = this.filter();
    const spots = this.#spots();
    return filter === 'All' ? spots : spots.filter((spot) => spot.status === filter);
  });

  readonly isInitialLoading = computed(() => this.#loadState() === 'loading' && this.#spots().length === 0);

  constructor() {
    const isVisible = (): boolean => this.#document.visibilityState === 'visible';
    const polling$ = fromEvent(this.#document, 'visibilitychange').pipe(
      map(isVisible),
      startWith(isVisible()),
      distinctUntilChanged(),
      switchMap((visible) => (visible ? timer(0, POLL_INTERVAL_MS) : EMPTY)),
    );

    merge(polling$, this.#refreshRequests)
      .pipe(
        // switchMap: una carga nueva cancela la anterior, nunca se pinta una respuesta vieja.
        switchMap(() => this.#loadSnapshot()),
        takeUntilDestroyed(),
      )
      .subscribe((snapshot) => this.#apply(snapshot));
  }

  refresh(): void {
    this.#refreshRequests.next();
  }

  checkIn(request: CheckInRequest): Promise<ActionResult> {
    return this.#runForSpot(request.parkingSpotId, this.#repository.checkIn(request));
  }

  checkOut(spotId: string, assignmentId: string): Promise<ActionResult> {
    return this.#runForSpot(spotId, this.#repository.checkOut(assignmentId));
  }

  #loadSnapshot(): Observable<Snapshot> {
    return forkJoin({ spots: this.#repository.getSpots(), active: this.#repository.getActiveAssignments() }).pipe(
      map(({ spots, active }): Snapshot => ({ ok: true, spots, active })),
      catchError((error: unknown) => of<Snapshot>({ ok: false, error: toApiError(error) })),
    );
  }

  #apply(snapshot: Snapshot): void {
    this.#now.set(Date.now());
    if (snapshot.ok) {
      this.#spots.set(snapshot.spots);
      this.#activeAssignments.set(snapshot.active);
      this.#loadError.set(null);
      this.#loadState.set('ready');
      this.#lastUpdated.set(new Date());
      return;
    }
    // Se conserva el último tablero válido; solo se marca error si nunca hubo datos.
    this.#loadError.set(snapshot.error);
    this.#loadState.set(this.#spots().length === 0 ? 'error' : 'ready');
  }

  async #runForSpot(spotId: string, action$: Observable<unknown>): Promise<ActionResult> {
    this.#busySpotIds.update((ids) => new Set(ids).add(spotId));
    try {
      await firstValueFrom(action$, { defaultValue: undefined });
      return { ok: true };
    } catch (error) {
      return { ok: false, error: toApiError(error) };
    } finally {
      this.#busySpotIds.update((ids) => {
        const next = new Set(ids);
        next.delete(spotId);
        return next;
      });
      // Tras éxito o error (p. ej. 409 porque otro operador lo ocupó) se refresca el tablero.
      this.refresh();
    }
  }
}
```

- [ ] **Step 4: Run → GREEN** (repeat `npm test` twice to check fake-timer stability), lint, build. **Step 5: Commit** `feat(frontend): add reactive parking store with visibility-aware polling`.

---

### Task 7: Presentational components

**Files:** Create `shared/time/format-elapsed.ts` (+ spec), `shared/ui/confirm-dialog.ts`, `features/parking/ui/components/spot-card.ts/.html/.scss` (+ spec), `status-filter.ts`, `active-vehicles-table.ts/.html`.

**Interfaces:** Produces `formatElapsed(since: Date, now: number): string`; `ConfirmDialog` + `ConfirmDialogData { title; message; confirmText }`; `SpotCard` (inputs `spot` required, `now` required, `busy` default false; outputs `checkIn`, `checkOut` emitting `ParkingSpot`); `StatusFilter` (input `counts: SpotCounts` required; `selected = model<SpotFilter>('All')`); `ActiveVehiclesTable` (inputs `assignments` required, `now` required).

- [ ] **Step 1: Failing tests**

`shared/time/format-elapsed.spec.ts`:
```ts
import { formatElapsed } from './format-elapsed';

describe('formatElapsed', () => {
  const since = new Date('2026-10-11T15:00:00Z');
  const after = (minutes: number): number => since.getTime() + minutes * 60_000;

  it.each([
    [0, 'hace menos de 1 min'],
    [25, 'hace 25 min'],
    [120, 'hace 2 h'],
    [125, 'hace 2 h 5 min'],
  ])('after %i minutes says %s', (minutes, expected) => {
    expect(formatElapsed(since, after(minutes))).toBe(expected);
  });

  it('never shows negative times when clocks drift', () => {
    expect(formatElapsed(since, after(-3))).toBe('hace menos de 1 min');
  });
});
```

`features/parking/ui/components/spot-card.spec.ts`:
```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ParkingSpot } from '../../domain/parking.models';
import { SpotCard } from './spot-card';

const NOW = new Date('2026-10-11T17:05:00Z').getTime();
const occupied: ParkingSpot = {
  id: 's-2',
  code: 'P02',
  status: 'Occupied',
  activeAssignment: {
    assignmentId: 'a-1', parkingSpotId: 's-2', spotCode: 'P02', licensePlate: 'ABC123',
    visitorName: 'Carlos Pérez', destinationUnit: '302', checkedInAt: new Date('2026-10-11T15:00:00Z'),
  },
};
const available: ParkingSpot = { id: 's-1', code: 'P01', status: 'Available', activeAssignment: null };
const outOfService: ParkingSpot = { id: 's-4', code: 'P04', status: 'OutOfService', activeAssignment: null };

describe('SpotCard', () => {
  async function render(spot: ParkingSpot): Promise<ComponentFixture<SpotCard>> {
    const fixture = TestBed.createComponent(SpotCard);
    fixture.componentRef.setInput('spot', spot);
    fixture.componentRef.setInput('now', NOW);
    await fixture.whenStable();
    return fixture;
  }

  it('shows plate, visitor, unit and elapsed time of an occupied spot', async () => {
    const text = (await render(occupied)).nativeElement.textContent as string;

    expect(text).toContain('ABC123');
    expect(text).toContain('Carlos Pérez');
    expect(text).toContain('302');
    expect(text).toContain('hace 2 h 5 min');
  });

  it('emits checkIn when registering an entry on an available spot', async () => {
    const fixture = await render(available);
    const emitted = vi.fn();
    fixture.componentInstance.checkIn.subscribe(emitted);

    (fixture.nativeElement as HTMLElement).querySelector('button')?.click();

    expect(emitted).toHaveBeenCalledWith(available);
  });

  it('disables the action while the spot is busy', async () => {
    const fixture = await render(occupied);
    fixture.componentRef.setInput('busy', true);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('button')?.disabled).toBe(true);
  });

  it('offers no action on an out-of-service spot', async () => {
    expect((await render(outOfService)).nativeElement.querySelector('button')).toBeNull();
  });
});
```

- [ ] **Step 2: Run → RED.**

- [ ] **Step 3: Implement**

`shared/time/format-elapsed.ts`:
```ts
const MINUTE_MS = 60_000;
const HOUR_MS = 60 * MINUTE_MS;

export function formatElapsed(since: Date, now: number): string {
  const elapsed = Math.max(0, now - since.getTime());
  if (elapsed < MINUTE_MS) {
    return 'hace menos de 1 min';
  }
  if (elapsed < HOUR_MS) {
    return `hace ${Math.floor(elapsed / MINUTE_MS)} min`;
  }
  const hours = Math.floor(elapsed / HOUR_MS);
  const minutes = Math.floor((elapsed % HOUR_MS) / MINUTE_MS);
  return minutes === 0 ? `hace ${hours} h` : `hace ${hours} h ${minutes} min`;
}
```

`shared/ui/confirm-dialog.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmText: string;
}

@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
      <button mat-flat-button type="button" [mat-dialog-close]="true" cdkFocusInitial>{{ data.confirmText }}</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
```

`features/parking/ui/components/spot-card.ts`:
```ts
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { formatElapsed } from '../../../../shared/time/format-elapsed';
import { ParkingSpot, ParkingSpotStatus, STATUS_LABELS, canCheckIn, canCheckOut } from '../../domain/parking.models';

const STATUS_MODIFIERS: Readonly<Record<ParkingSpotStatus, string>> = {
  Available: 'available',
  Occupied: 'occupied',
  OutOfService: 'out-of-service',
};

@Component({
  selector: 'app-spot-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule],
  templateUrl: './spot-card.html',
  styleUrl: './spot-card.scss',
  host: { '[class]': 'hostClass()' },
})
export class SpotCard {
  readonly spot = input.required<ParkingSpot>();
  readonly now = input.required<number>();
  readonly busy = input(false);
  readonly checkIn = output<ParkingSpot>();
  readonly checkOut = output<ParkingSpot>();

  protected readonly hostClass = computed(() => `spot-card spot-card--${STATUS_MODIFIERS[this.spot().status]}`);
  protected readonly statusLabel = computed(() => STATUS_LABELS[this.spot().status]);
  protected readonly assignment = computed(() => this.spot().activeAssignment);
  protected readonly elapsed = computed(() => {
    const assignment = this.assignment();
    return assignment ? formatElapsed(assignment.checkedInAt, this.now()) : null;
  });
  protected readonly canCheckIn = computed(() => canCheckIn(this.spot()));
  protected readonly canCheckOut = computed(() => canCheckOut(this.spot()));
}
```

`spot-card.html`:
```html
<article class="spot-card__body" [attr.aria-label]="spot().code + ', ' + statusLabel()">
  <header class="spot-card__header">
    <span class="spot-card__code">{{ spot().code }}</span>
    <span class="spot-card__status"><span class="spot-card__dot" aria-hidden="true"></span>{{ statusLabel() }}</span>
  </header>

  @if (assignment(); as current) {
    <dl class="spot-card__details">
      <div><dt>Placa</dt><dd class="spot-card__plate">{{ current.licensePlate }}</dd></div>
      <div><dt>Visitante</dt><dd>{{ current.visitorName }}</dd></div>
      <div><dt>Unidad</dt><dd>{{ current.destinationUnit }}</dd></div>
      <div><dt>Ingreso</dt><dd>{{ current.checkedInAt | date: 'shortTime' }} · {{ elapsed() }}</dd></div>
    </dl>
  }

  <footer class="spot-card__actions">
    @if (canCheckIn()) {
      <button mat-flat-button type="button" [disabled]="busy()" (click)="checkIn.emit(spot())">Registrar ingreso</button>
    } @else if (canCheckOut()) {
      <button mat-stroked-button type="button" [disabled]="busy()" (click)="checkOut.emit(spot())">Registrar salida</button>
    } @else {
      <span class="spot-card__hint">No disponible para visitantes</span>
    }
  </footer>
</article>
```

`spot-card.scss`:
```scss
:host {
  --spot-color: var(--status-available);
  --spot-bg: var(--status-available-bg);
  display: block;
}

:host(.spot-card--occupied) {
  --spot-color: var(--status-occupied);
  --spot-bg: var(--status-occupied-bg);
}

:host(.spot-card--out-of-service) {
  --spot-color: var(--status-out-of-service);
  --spot-bg: var(--status-out-of-service-bg);
}

.spot-card__body {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  height: 100%;
  box-sizing: border-box;
  padding: 1rem;
  border-radius: 12px;
  border-left: 6px solid var(--spot-color);
  background: var(--mat-sys-surface);
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
}

.spot-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.spot-card__code {
  font-size: 1.25rem;
  font-weight: 700;
}

.spot-card__status {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.125rem 0.625rem;
  border-radius: 999px;
  background: var(--spot-bg);
  color: var(--spot-color);
  font-size: 0.8125rem;
  font-weight: 500;
}

.spot-card__dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  background: currentColor;
}

.spot-card__details {
  display: grid;
  gap: 0.25rem;
  margin: 0;
  font-size: 0.875rem;

  div {
    display: flex;
    justify-content: space-between;
    gap: 0.5rem;
  }

  dt {
    color: var(--mat-sys-on-surface-variant);
  }

  dd {
    margin: 0;
    text-align: right;
  }
}

.spot-card__plate {
  font-weight: 700;
  letter-spacing: 0.05em;
}

.spot-card__actions {
  margin-top: auto;
  display: flex;
  justify-content: flex-end;
}

.spot-card__hint {
  font-size: 0.8125rem;
  color: var(--mat-sys-on-surface-variant);
}
```

`status-filter.ts`:
```ts
import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { SpotCounts, SpotFilter } from '../../domain/parking.models';

const FILTER_OPTIONS: readonly { readonly value: SpotFilter; readonly label: string }[] = [
  { value: 'All', label: 'Todos' },
  { value: 'Available', label: 'Disponibles' },
  { value: 'Occupied', label: 'Ocupados' },
  { value: 'OutOfService', label: 'Fuera de servicio' },
];

@Component({
  selector: 'app-status-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonToggleModule],
  template: `
    <mat-button-toggle-group
      [value]="selected()"
      (change)="selected.set($event.value)"
      hideSingleSelectionIndicator
      aria-label="Filtrar parqueaderos por estado"
    >
      @for (option of options; track option.value) {
        <mat-button-toggle [value]="option.value">{{ option.label }} ({{ counts()[option.value] }})</mat-button-toggle>
      }
    </mat-button-toggle-group>
  `,
})
export class StatusFilter {
  readonly counts = input.required<SpotCounts>();
  readonly selected = model<SpotFilter>('All');
  protected readonly options = FILTER_OPTIONS;
}
```

`active-vehicles-table.ts`:
```ts
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { formatElapsed } from '../../../../shared/time/format-elapsed';
import { ActiveAssignment } from '../../domain/parking.models';

@Component({
  selector: 'app-active-vehicles-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatTableModule],
  templateUrl: './active-vehicles-table.html',
})
export class ActiveVehiclesTable {
  readonly assignments = input.required<readonly ActiveAssignment[]>();
  readonly now = input.required<number>();

  protected readonly columns = ['spotCode', 'licensePlate', 'visitorName', 'destinationUnit', 'checkedInAt'];
  protected readonly rows = computed(() =>
    this.assignments().map((assignment) => ({ ...assignment, elapsed: formatElapsed(assignment.checkedInAt, this.now()) })),
  );
}
```

`active-vehicles-table.html`:
```html
@if (rows().length === 0) {
  <p class="active-table__empty">No hay vehículos de visitantes dentro de la propiedad.</p>
} @else {
  <table mat-table [dataSource]="rows()" class="active-table">
    <ng-container matColumnDef="spotCode">
      <th mat-header-cell *matHeaderCellDef>Parqueadero</th>
      <td mat-cell *matCellDef="let row">{{ row.spotCode }}</td>
    </ng-container>
    <ng-container matColumnDef="licensePlate">
      <th mat-header-cell *matHeaderCellDef>Placa</th>
      <td mat-cell *matCellDef="let row"><strong>{{ row.licensePlate }}</strong></td>
    </ng-container>
    <ng-container matColumnDef="visitorName">
      <th mat-header-cell *matHeaderCellDef>Visitante</th>
      <td mat-cell *matCellDef="let row">{{ row.visitorName }}</td>
    </ng-container>
    <ng-container matColumnDef="destinationUnit">
      <th mat-header-cell *matHeaderCellDef>Unidad</th>
      <td mat-cell *matCellDef="let row">{{ row.destinationUnit }}</td>
    </ng-container>
    <ng-container matColumnDef="checkedInAt">
      <th mat-header-cell *matHeaderCellDef>Ingreso</th>
      <td mat-cell *matCellDef="let row">{{ row.checkedInAt | date: 'shortTime' }} · {{ row.elapsed }}</td>
    </ng-container>
    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns"></tr>
  </table>
}
```

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add spot card, status filter, active vehicles table and confirm dialog`.

---

### Task 8: Check-in dialog, board page, board route

**Files:** Create `features/parking/ui/dialogs/check-in-dialog.ts/.html/.scss` (+ spec), `ui/pages/parking-board-page.ts/.html/.scss`. Modify `app.routes.ts`.

**Interfaces:** Consumes `ParkingStore`, `ParkingRepository`/`ParkingHttpRepository`, components from Task 7, `Notifier`, `isValidPlate`, `normalizePlate`. Produces `CheckInDialog` (data `CheckInDialogData { spot }`, closes with `true` on success), `ParkingBoardPage` (provides `ParkingStore`), route `''` guarded by `authGuard` with `{ provide: ParkingRepository, useClass: ParkingHttpRepository }`.

- [ ] **Step 1: Failing test** — `dialogs/check-in-dialog.spec.ts`

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { vi } from 'vitest';
import { ParkingStore } from '../../application/parking.store';
import { ParkingSpot } from '../../domain/parking.models';
import { CheckInDialog } from './check-in-dialog';

const spot: ParkingSpot = { id: 's-1', code: 'P01', status: 'Available', activeAssignment: null };

describe('CheckInDialog', () => {
  const checkIn = vi.fn();
  const close = vi.fn();
  let fixture: ComponentFixture<CheckInDialog>;
  let element: HTMLElement;

  beforeEach(async () => {
    checkIn.mockReset();
    close.mockReset();
    TestBed.configureTestingModule({
      imports: [CheckInDialog],
      providers: [
        { provide: ParkingStore, useValue: { checkIn } },
        { provide: MatDialogRef, useValue: { close } },
        { provide: MAT_DIALOG_DATA, useValue: { spot } },
      ],
    });
    fixture = TestBed.createComponent(CheckInDialog);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  async function fill(plate: string, visitor: string, unit: string): Promise<void> {
    const [plateInput, visitorInput, unitInput] = Array.from(element.querySelectorAll('input'));
    for (const [input, value] of [[plateInput, plate], [visitorInput, visitor], [unitInput, unit]] as const) {
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    await fixture.whenStable();
  }

  async function submitForm(): Promise<void> {
    element.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('does not send anything while the form is invalid', async () => {
    await fill('12-AB', '', '');
    await submitForm();

    expect(checkIn).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Formato válido: ABC123');
  });

  it('previews the normalized plate while typing', async () => {
    await fill(' abc-123 ', '', '');

    expect(element.textContent).toContain('Se registrará como ABC123');
  });

  it('registers the entry and closes the dialog on success', async () => {
    checkIn.mockResolvedValue({ ok: true });
    await fill('abc-123', '  Carlos Pérez ', '302');

    await submitForm();

    expect(checkIn).toHaveBeenCalledWith({ parkingSpotId: 's-1', licensePlate: 'abc-123', visitorName: 'Carlos Pérez', destinationUnit: '302' });
    expect(close).toHaveBeenCalledWith(true);
  });

  it('stays open and shows the backend message when the entry is rejected', async () => {
    checkIn.mockResolvedValue({ ok: false, error: { status: 409, code: 'Parking.VehicleAlreadyInside', message: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.' } });
    await fill('abc-123', 'Carlos Pérez', '302');

    await submitForm();

    expect(close).not.toHaveBeenCalled();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('ya se encuentra dentro');
  });
});
```

- [ ] **Step 2: Run → RED.**

- [ ] **Step 3: Implement**

`dialogs/check-in-dialog.ts`:
```ts
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormField, form, maxLength, minLength, pattern, required, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ParkingStore } from '../../application/parking.store';
import { isValidPlate, normalizePlate } from '../../domain/license-plate';
import { ParkingSpot } from '../../domain/parking.models';

export interface CheckInDialogData {
  readonly spot: ParkingSpot;
}

interface CheckInFormModel {
  licensePlate: string;
  visitorName: string;
  destinationUnit: string;
}

// Acepta lo que el backend normaliza: mayúsculas/minúsculas y un separador opcional (espacio, guion o punto).
const PLATE_INPUT = /^\s*[a-zA-Z]{3}[\s.-]?(\d{3}|\d{2}[a-zA-Z])\s*$/;

@Component({
  selector: 'app-check-in-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  templateUrl: './check-in-dialog.html',
  styleUrl: './check-in-dialog.scss',
})
export class CheckInDialog {
  readonly #store = inject(ParkingStore);
  readonly #dialogRef = inject<MatDialogRef<CheckInDialog, boolean>>(MatDialogRef);
  protected readonly spot = inject<CheckInDialogData>(MAT_DIALOG_DATA).spot;

  protected readonly model = signal<CheckInFormModel>({ licensePlate: '', visitorName: '', destinationUnit: '' });
  protected readonly checkInForm = form(this.model, (path) => {
    required(path.licensePlate, { message: 'La placa es obligatoria.' });
    pattern(path.licensePlate, PLATE_INPUT, { message: 'Formato válido: ABC123 (carro) o ABC12D (moto).' });
    required(path.visitorName, { message: 'El nombre del visitante es obligatorio.' });
    minLength(path.visitorName, 2, { message: 'Mínimo 2 caracteres.' });
    maxLength(path.visitorName, 100, { message: 'Máximo 100 caracteres.' });
    required(path.destinationUnit, { message: 'La unidad que visita es obligatoria.' });
    maxLength(path.destinationUnit, 20, { message: 'Máximo 20 caracteres.' });
  });

  protected readonly normalizedPlate = computed(() => {
    const raw = this.model().licensePlate;
    return isValidPlate(raw) ? normalizePlate(raw) : null;
  });
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.checkInForm, async () => {
      this.submitting.set(true);
      this.serverError.set(null);
      const { licensePlate, visitorName, destinationUnit } = this.model();
      const result = await this.#store.checkIn({
        parkingSpotId: this.spot.id,
        licensePlate,
        visitorName: visitorName.trim(),
        destinationUnit: destinationUnit.trim(),
      });
      this.submitting.set(false);
      if (result.ok) {
        this.#dialogRef.close(true);
      } else {
        this.serverError.set(result.error.message);
      }
      return undefined;
    });
  }
}
```

`dialogs/check-in-dialog.html`:
```html
<form (submit)="save($event)" novalidate>
  <h2 mat-dialog-title>Registrar ingreso · {{ spot.code }}</h2>

  <mat-dialog-content class="check-in">
    <mat-form-field appearance="outline">
      <mat-label>Placa</mat-label>
      <input matInput [formField]="checkInForm.licensePlate" autocomplete="off" cdkFocusInitial />
      @if (normalizedPlate(); as plate) {
        <mat-hint>Se registrará como {{ plate }}</mat-hint>
      }
      @for (error of checkInForm.licensePlate().errors(); track $index) {
        <mat-error>{{ error.message }}</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Nombre del visitante</mat-label>
      <input matInput [formField]="checkInForm.visitorName" autocomplete="off" />
      @for (error of checkInForm.visitorName().errors(); track $index) {
        <mat-error>{{ error.message }}</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Unidad que visita</mat-label>
      <input matInput [formField]="checkInForm.destinationUnit" autocomplete="off" />
      @for (error of checkInForm.destinationUnit().errors(); track $index) {
        <mat-error>{{ error.message }}</mat-error>
      }
    </mat-form-field>

    @if (serverError(); as message) {
      <p class="check-in__error" role="alert">{{ message }}</p>
    }
  </mat-dialog-content>

  <mat-dialog-actions align="end">
    <button mat-button type="button" mat-dialog-close [disabled]="submitting()">Cancelar</button>
    <button mat-flat-button type="submit" [disabled]="submitting()">Registrar ingreso</button>
  </mat-dialog-actions>
  @if (submitting()) {
    <mat-progress-bar mode="indeterminate" />
  }
</form>
```

`dialogs/check-in-dialog.scss`:
```scss
.check-in {
  display: grid;
  gap: 0.25rem;
  min-width: min(22rem, 80vw);
  padding-top: 0.5rem;
}

.check-in__error {
  margin: 0;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  background: var(--status-occupied-bg);
  color: var(--status-occupied);
}
```

`pages/parking-board-page.ts`:
```ts
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Injector, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';
import { Notifier } from '../../../../core/notifications/notifier';
import { ConfirmDialog, ConfirmDialogData } from '../../../../shared/ui/confirm-dialog';
import { ParkingStore } from '../../application/parking.store';
import { ParkingSpot } from '../../domain/parking.models';
import { ActiveVehiclesTable } from '../components/active-vehicles-table';
import { SpotCard } from '../components/spot-card';
import { StatusFilter } from '../components/status-filter';
import { CheckInDialog, CheckInDialogData } from '../dialogs/check-in-dialog';

@Component({
  selector: 'app-parking-board-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkingStore],
  imports: [ActiveVehiclesTable, DatePipe, MatButtonModule, MatProgressBarModule, SpotCard, StatusFilter],
  templateUrl: './parking-board-page.html',
  styleUrl: './parking-board-page.scss',
})
export class ParkingBoardPage {
  protected readonly store = inject(ParkingStore);
  readonly #dialog = inject(MatDialog);
  readonly #injector = inject(Injector);
  readonly #notifier = inject(Notifier);

  protected async openCheckIn(spot: ParkingSpot): Promise<void> {
    // El injector de la página permite que el diálogo use el mismo ParkingStore.
    const dialogRef = this.#dialog.open<CheckInDialog, CheckInDialogData, boolean>(CheckInDialog, {
      data: { spot },
      injector: this.#injector,
    });
    if (await firstValueFrom(dialogRef.afterClosed())) {
      this.#notifier.success(`Ingreso registrado en ${spot.code}.`);
    }
  }

  protected async confirmCheckOut(spot: ParkingSpot): Promise<void> {
    const assignment = spot.activeAssignment;
    if (!assignment) {
      return;
    }
    const confirmed = await firstValueFrom(
      this.#dialog
        .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
          data: {
            title: `Registrar salida · ${spot.code}`,
            message: `¿Confirmar la salida del vehículo ${assignment.licensePlate}?`,
            confirmText: 'Registrar salida',
          },
        })
        .afterClosed(),
    );
    if (!confirmed) {
      return;
    }
    const result = await this.store.checkOut(spot.id, assignment.assignmentId);
    if (result.ok) {
      this.#notifier.success(`Salida registrada: ${assignment.licensePlate} liberó ${spot.code}.`);
    } else {
      this.#notifier.error(result.error.message);
    }
  }
}
```

`pages/parking-board-page.html`:
```html
<section class="board">
  <header class="board__header">
    <div>
      <h1 class="board__title">Parqueaderos de visitantes</h1>
      @if (store.lastUpdated(); as updated) {
        <p class="board__subtitle">Actualizado a las {{ updated | date: 'mediumTime' }} · se actualiza cada 30 s</p>
      }
    </div>
    <button mat-stroked-button type="button" (click)="store.refresh()">Actualizar</button>
  </header>

  <app-status-filter [counts]="store.counts()" [(selected)]="store.filter" />

  @if (store.isInitialLoading()) {
    <mat-progress-bar mode="indeterminate" aria-label="Cargando parqueaderos" />
  }

  @if (store.loadError(); as error) {
    <div class="board__error" role="alert">
      <span>{{ error.message }}</span>
      <button mat-button type="button" (click)="store.refresh()">Reintentar</button>
    </div>
  }

  <div class="board__grid">
    @for (spot of store.visibleSpots(); track spot.id) {
      <app-spot-card
        [spot]="spot"
        [now]="store.now()"
        [busy]="store.busySpotIds().has(spot.id)"
        (checkIn)="openCheckIn($event)"
        (checkOut)="confirmCheckOut($event)"
      />
    } @empty {
      @if (!store.isInitialLoading()) {
        <p class="board__empty">No hay parqueaderos con este estado.</p>
      }
    }
  </div>

  <section class="board__active" aria-labelledby="active-vehicles-title">
    <h2 id="active-vehicles-title" class="board__section-title">Vehículos dentro ({{ store.activeAssignments().length }})</h2>
    <app-active-vehicles-table [assignments]="store.activeAssignments()" [now]="store.now()" />
  </section>
</section>
```

`pages/parking-board-page.scss`:
```scss
.board {
  display: grid;
  gap: 1.25rem;
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem 1rem 3rem;
}

.board__header {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: flex-end;
  gap: 1rem;
}

.board__title {
  margin: 0;
  font-size: 1.75rem;
}

.board__subtitle {
  margin: 0.25rem 0 0;
  color: var(--mat-sys-on-surface-variant);
  font-size: 0.875rem;
}

.board__error {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  background: var(--status-occupied-bg);
  color: var(--status-occupied);
}

.board__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr));
  gap: 1rem;
}

.board__empty {
  grid-column: 1 / -1;
  color: var(--mat-sys-on-surface-variant);
}

.board__section-title {
  margin: 0.5rem 0 0.75rem;
  font-size: 1.25rem;
}

.board__active {
  overflow-x: auto;
}
```

`app.routes.ts` — final:
```ts
import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { ParkingRepository } from './features/parking/domain/parking.repository';
import { ParkingHttpRepository } from './features/parking/infrastructure/parking-http.repository';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Ingresar · DomoNow',
    loadComponent: () => import('./core/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    title: 'Parqueaderos de visitantes · DomoNow',
    // Hexagonal: aquí se enlaza el puerto del feature con su adaptador HTTP.
    providers: [{ provide: ParkingRepository, useClass: ParkingHttpRepository }],
    loadComponent: () => import('./features/parking/ui/pages/parking-board-page').then((m) => m.ParkingBoardPage),
  },
  { path: '**', redirectTo: '' },
];
```

- [ ] **Step 4: Run → GREEN**, lint, build. **Step 5: Commit** `feat(frontend): add parking board page with check-in dialog and checkout confirmation`.

---

### Task 9: Verificación real en el navegador (API + Angular)

**Files:** Create `.claude/launch.json`. Fixes found here go in their own TDD commits.

- [ ] **Step 1: launch.json**

```json
{
  "version": "0.0.1",
  "configurations": [
    {
      "name": "api",
      "runtimeExecutable": "C:\\Program Files\\dotnet\\dotnet.exe",
      "runtimeArgs": ["run", "--project", "backend/src/DomoNow.Parking.Api", "--launch-profile", "http"],
      "port": 5080
    },
    {
      "name": "web",
      "runtimeExecutable": "C:\\nvm4w\\nodejs\\npm.cmd",
      "runtimeArgs": ["--prefix", "frontend", "start"],
      "port": 4200
    }
  ]
}
```

- [ ] **Step 2: Start both** with `preview_start` (`api`, then `web`) and open `http://localhost:4200`.

- [ ] **Step 3: Walk the flows** (read the page / screenshots; check console for errors):
  1. `/` without session → redirects to `/login`.
  2. Wrong password → "Usuario o contraseña incorrectos." Correct password (from `postgres-dev-credentials.txt`, typed by the agent only into the local dev app) → board with 30 spots; P04 and P17 gray.
  3. Filter "Ocupados" / "Fuera de servicio" → counts and grid change.
  4. Check-in `abc-123` on a free spot → preview "Se registrará como ABC123" → toast; card turns red with plate, visitor, unit, time; vehicle appears in "Vehículos dentro".
  5. Same plate on another spot → dialog stays open with "El vehículo ABC123 ya se encuentra dentro de la propiedad."
  6. "Registrar salida" → confirm → toast; spot green again; vehicle leaves the table.
  7. Remove the session token from sessionStorage's JWT (edit to an invalid value) and click "Actualizar" → redirected to login with "Su sesión expiró".
  8. Stop the API → banner "No se pudo conectar…" while the last board stays visible; restart → "Reintentar" recovers.
- [ ] **Step 4:** Save 3 screenshots (board, check-in dialog with error, login) for the PR. Stop servers.
- [ ] **Step 5:** Any bug → failing test first → fix → commit `fix(frontend): …`. Ledger each.

---

### Task 10: Docs, final review, PR

- [ ] **Step 1: README** — fill "Ejecutar el frontend":

```markdown
## Ejecutar el frontend

Requiere la API corriendo en `http://localhost:5080`.

```bash
cd frontend
npm install
npm start
```

Abrir `http://localhost:4200` e ingresar con el operador de prueba configurado en `SeedOperator`.

| Comando | Qué hace |
|---------|----------|
| `npm test` | Pruebas unitarias y de componentes (Vitest) |
| `npm run lint` | angular-eslint |
| `npm run build` | Build de producción |
```

Update the README stack table (Frontend row: "Angular 22 (zoneless) · Signals · Signal Forms · Angular Material") and add the frontend test count to the tests table.

- [ ] **Step 2:** `docs/ARCHITECTURE.md` §3 — sync the frontend tree with the real file names (`spot-card.ts`, `parking-board-page.ts`, `dialogs/`), mention Signal Forms and visibility-aware polling. `CLAUDE.md` §5 — verified frontend commands (PowerShell note).
- [ ] **Step 3:** `docs/PROGRESS.md` entry + board; `AI-USAGE.md` real cases from this phase.
- [ ] **Step 4: Final verification** — backend `dotnet test` (still 82/82) + frontend `npm test`, `npm run lint`, `npx ng build`.
- [ ] **Step 5:** Commit `docs: document frontend setup and architecture`, final whole-branch review (fresh reviewer), fix Critical/Important with TDD, push `feat/frontend` and open PR against `main` (merge only with user approval).

---

## Coverage map (spec → task)

| Requirement | Task(s) |
|-------------|---------|
| Pantalla "Parqueaderos de visitantes" con tarjetas por estado | 7 (SpotCard), 8 (page) |
| Identificar rápido Disponibles / Ocupados (con placa) / Fuera de servicio | 7 (colores, chip de estado, placa), 7 (filtro con contadores) |
| Registrar ingreso desde un disponible (placa, visitante, unidad) | 8 (CheckInDialog + Signal Forms) |
| Consumir API, actualizar, mostrar errores del backend | 5 (adapter), 6 (store refresh), 2 (ApiError), 8 (diálogo/toasts) |
| Registrar salida desde un ocupado, vuelve a disponible | 7 (botón), 8 (confirmación + store.checkOut), 6 (refresh) |
| Usuario autorizado (JWT) | 3 (session/interceptors/guards), 4 (login) |
| Vehículos dentro (endpoint /active) | 5, 6, 7 (tabla), 8 |
| Reactividad en todo el front | 3, 4, 6, 7, 8 (signals, computed, model, RxJS→signals, Signal Forms) |
| Arquitectura hexagonal frontend | 5 (port/adapter), 8 (binding en ruta) |
| Tests y calidad | 2–8 (TDD), 9 (verificación real), 10 (review) |
