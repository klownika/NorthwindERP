import { Component, input } from '@angular/core';
import { UiError } from '../../core/util/problem';

/** Muestra "Cargando…" o el error de la API en el lugar del contenido. */
@Component({
  selector: 'app-load-state',
  template: `
    @if (error(); as e) {
      <div class="alert err" role="alert">
        <strong>{{ e.title }}</strong>
        @for (m of e.messages; track m) { <span>{{ m }}</span> }
      </div>
    } @else if (loading()) {
      <div class="loading" role="status"><span class="dot"></span>Cargando datos…</div>
    }
  `,
  styles: `
    .loading { display: flex; align-items: center; gap: 10px; padding: 18px; color: var(--ink-3); font-weight: 600; }
    .dot { width: 14px; height: 14px; border-radius: 999px; border: 3px solid #fed7aa; border-top-color: var(--or-600); animation: spin .8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `,
})
export class LoadStateComponent {
  readonly loading = input(false);
  readonly error = input<UiError | null>(null);
}
