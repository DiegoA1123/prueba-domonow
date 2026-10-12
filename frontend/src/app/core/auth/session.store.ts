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
