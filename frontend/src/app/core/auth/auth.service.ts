import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { ChangePasswordRequest, CurrentUser, LoginRequest, LoginResponse } from '../models/api.models';

const KEY = 'stockin.session';

/**
 * Sesión del usuario (JWT).
 * El token se guarda en sessionStorage: se borra al cerrar la pestaña.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly _session = signal<LoginResponse | null>(this.restore());

  readonly session = this._session.asReadonly();
  readonly isLoggedIn = computed(() => {
    const s = this._session();
    return !!s && new Date(s.expiresAtUtc).getTime() > Date.now();
  });
  readonly isAdmin = computed(() => this._session()?.role === 'Admin');
  readonly token = computed(() => this._session()?.token ?? null);

  /** POST /api/auth/login */
  login(body: LoginRequest) {
    return this.http.post<LoginResponse>('/api/auth/login', body).pipe(tap((s) => this.save(s)));
  }

  /** GET /api/auth/me */
  me() {
    return this.http.get<CurrentUser>('/api/auth/me');
  }

  /** POST /api/auth/change-password  → 204 */
  changePassword(body: ChangePasswordRequest) {
    return this.http.post<void>('/api/auth/change-password', body).pipe(
      tap(() => {
        const s = this._session();
        if (s) this.save({ ...s, mustChangePassword: false });
      }),
    );
  }

  logout(): void {
    sessionStorage.removeItem(KEY);
    this._session.set(null);
    this.router.navigate(['/login']);
  }

  private save(s: LoginResponse): void {
    sessionStorage.setItem(KEY, JSON.stringify(s));
    this._session.set(s);
  }

  private restore(): LoginResponse | null {
    try {
      const raw = sessionStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as LoginResponse) : null;
    } catch {
      return null;
    }
  }
}
