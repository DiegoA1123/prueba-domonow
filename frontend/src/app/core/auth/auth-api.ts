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
      .pipe(
        map((response) => ({
          accessToken: response.accessToken,
          username: response.username,
          expiresAt: response.expiresAt,
        })),
      );
  }
}
