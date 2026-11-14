import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { MOCK_USERS, MockDb } from '../mock/mock-db';
import { ChangePasswordRequest, CurrentUser, LoginRequest, LoginResponse } from '../models/api.models';

const KEY = 'stockin.session';

/**
 * Sesión del usuario (JWT).
 * El token se guarda en sessionStorage: se borra al cerrar la pestaña.
 *
 * ⚠ MOCK: el login se valida contra MOCK_USERS (admin / Admin123! y vendedor / Vendedor123!).
 *   Cuando se conecte el backend, cada método usa HttpClient con el endpoint indicado en su comentario.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private db = inject(MockDb);
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
    const u = MOCK_USERS.find((x) => x.username === body.username && x.password === body.password);
    if (!u) return this.db.fail(401, 'Unauthorized', 'Invalid username or password.');
    const res: LoginResponse = {
      token: `mock.${btoa(u.username)}.${Date.now()}`, expiresAtUtc: new Date(Date.now() + 8 * 3600_000).toISOString(),
      userId: u.userId, username: u.username, role: u.role, employeeId: u.employeeId, mustChangePassword: u.mustChangePassword,
    };
    return this.db.respond(res).pipe(tap((s) => this.save(s)));
  }

  /** GET /api/auth/me */
  me() {
    const s = this._session();
    return this.db.respond<CurrentUser>({ userId: s?.userId ?? 0, username: s?.username ?? null, role: s?.role ?? null, employeeId: s?.employeeId ?? null });
  }

  /** POST /api/auth/change-password  → 204 */
  changePassword(body: ChangePasswordRequest) {
    const u = MOCK_USERS.find((x) => x.userId === this._session()?.userId);
    if (!u || u.password !== body.currentPassword) return this.db.fail(400, 'Bad Request', 'Current password is incorrect.');
    u.password = body.newPassword;
    return this.db.respond<void>(undefined).pipe(
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
