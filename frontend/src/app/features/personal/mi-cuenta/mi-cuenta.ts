import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { CurrentUser } from '../../../core/models/api.models';
import { toUiError } from '../../../core/util/problem';

/** Mi cuenta: datos del token (GET /api/auth/me) y cambio de contraseña. Se abre desde el avatar. */
@Component({
  selector: 'app-mi-cuenta',
  imports: [FormsModule],
  templateUrl: './mi-cuenta.html',
  styleUrl: './mi-cuenta.scss',
})
export class MiCuentaComponent {
  readonly auth = inject(AuthService);
  readonly me = signal<CurrentUser | null>(null);
  readonly cur = signal('');
  readonly n1 = signal('');
  readonly n2 = signal('');
  readonly busy = signal(false);
  readonly msg = signal<{ ok: boolean; title: string; text: string } | null>(null);

  readonly facts = computed(() => {
    const s = this.auth.session();
    const m = this.me();
    return [
      { label: 'Usuario', value: m?.username ?? s?.username ?? '—' },
      { label: 'Rol', value: m?.role ?? s?.role ?? '—' },
      { label: 'Empleado (ID)', value: String(m?.employeeId ?? s?.employeeId ?? '—') },
      { label: 'UserAccountID', value: String(m?.userId ?? s?.userId ?? '—') },
      { label: 'MustChangePassword', value: s?.mustChangePassword ? 'true' : 'false' },
      { label: 'Token expira', value: s ? new Date(s.expiresAtUtc).toLocaleString() : '—' },
    ];
  });

  constructor() {
    this.auth.me().subscribe({ next: (m) => this.me.set(m) });
  }

  submit(): void {
    const fail = (title: string, text: string) => this.msg.set({ ok: false, title, text });
    if (this.n1().length < 8) return fail('400 Bad Request', 'The field NewPassword must be a string with a minimum length of 8.');
    if (this.n1() === this.cur()) return fail('400 Bad Request', 'New password must be different from the current one.');
    if (this.n1() !== this.n2()) return fail('Formulario', 'Las contraseñas nuevas no coinciden.');
    this.busy.set(true);
    this.auth.changePassword({ currentPassword: this.cur(), newPassword: this.n1() }).subscribe({
      next: () => {
        this.cur.set(''); this.n1.set(''); this.n2.set('');
        this.msg.set({ ok: true, title: '204 No Content', text: 'Contraseña cambiada.' });
        this.busy.set(false);
      },
      error: (e: unknown) => {
        const ui = toUiError(e);
        fail(ui.title, ui.messages.join(' '));
        this.busy.set(false);
      },
    });
  }
}
