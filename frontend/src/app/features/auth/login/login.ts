import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { UiError, toUiError } from '../../../core/util/problem';
import { IconComponent } from '../../../shared/ui/icon';

/** Login → POST /api/auth/login. Guarda el JWT y entra a la app. */
@Component({
  selector: 'app-login',
  imports: [FormsModule, IconComponent],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  readonly username = signal('');
  readonly password = signal('');
  readonly showPw = signal(false);
  readonly busy = signal(false);
  readonly error = signal<UiError | null>(null);

  submit(): void {
    if (!this.username().trim() || !this.password()) {
      this.error.set({ title: '400 BAD REQUEST', messages: ['Ingresa usuario y contraseña.'] });
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.auth.login({ username: this.username().trim(), password: this.password() }).subscribe({
      next: (s) => this.router.navigate([s.mustChangePassword ? '/cuenta' : '/']),
      error: (e: unknown) => {
        const ui = toUiError(e);
        this.error.set(ui.title.startsWith('401') ? { title: '401 UNAUTHORIZED', messages: ['Invalid username or password.'] } : ui);
        this.busy.set(false);
      },
    });
  }
}
