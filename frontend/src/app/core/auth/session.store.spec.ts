import { TestBed } from '@angular/core/testing';
import { Session, SessionStore } from './session.store';

const STORAGE_KEY = 'domonow.session';
const inOneHour = (): string => new Date(Date.now() + 3_600_000).toISOString();
const session = (): Session => ({
  accessToken: 'token-123',
  username: 'portero',
  expiresAt: inOneHour(),
});

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
    sessionStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ ...session(), expiresAt: '2000-01-01T00:00:00Z' }),
    );

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
