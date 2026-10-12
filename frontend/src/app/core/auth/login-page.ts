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
  imports: [
    FormField,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
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
