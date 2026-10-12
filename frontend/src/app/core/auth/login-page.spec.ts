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
    login.mockReturnValue(
      throwError(() => ({
        status: 401,
        code: 'Auth.InvalidCredentials',
        message: 'Usuario o contraseña incorrectos.',
      })),
    );

    const element = await submitWith('portero', 'wrong');

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Usuario o contraseña incorrectos.',
    );
  });

  it('starts the session and goes to the board on success', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    login.mockReturnValue(
      of({
        accessToken: 't',
        username: 'portero',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
      }),
    );

    await submitWith('portero', 'secret');

    expect(TestBed.inject(SessionStore).username()).toBe('portero');
    expect(navigate).toHaveBeenCalledWith(['/']);
  });
});
