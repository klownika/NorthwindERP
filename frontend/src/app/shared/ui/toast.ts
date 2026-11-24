import { Component, Injectable, inject, signal } from '@angular/core';
import { IconComponent } from './icon';

/** Mensaje breve de confirmación: this.toast.show('Pedido eliminado.'). */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly message = signal<string | null>(null);
  private timer?: ReturnType<typeof setTimeout>;

  show(text: string): void {
    clearTimeout(this.timer);
    this.message.set(text);
    this.timer = setTimeout(() => this.message.set(null), 5000);
  }

  dismiss(): void {
    clearTimeout(this.timer);
    this.message.set(null);
  }
}

@Component({
  selector: 'app-toast-outlet',
  imports: [IconComponent],
  template: `
    <div class="holder" role="status" aria-live="polite">
      @if (toast.message(); as m) {
        <div class="toast">
          <span class="ok"><app-icon name="check" [size]="16" [stroke]="3" /></span>
          <span class="txt">{{ m }}</span>
          <button type="button" aria-label="Cerrar aviso" (click)="toast.dismiss()"><app-icon name="close" [size]="16" /></button>
        </div>
      }
    </div>
  `,
  styles: `
    .holder { position: fixed; right: 20px; bottom: 20px; z-index: 60; max-width: min(420px, calc(100vw - 32px)); }
    .toast {
      display: flex; align-items: center; gap: 10px; padding: 10px 10px 10px 14px; border-radius: 12px; background: var(--ink); color: #fff;
      font-size: 14px; font-weight: 600; box-shadow: 0 14px 30px -12px rgba(28, 25, 23, .6); animation: rise .18s ease-out;
    }
    .ok { flex: 0 0 auto; display: inline-flex; align-items: center; justify-content: center; width: 26px; height: 26px; border-radius: 999px; background: var(--ok-bg); color: var(--ok-ink); }
    .txt { flex: 1 1 auto; }
    button { flex: 0 0 auto; display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; border: 0; border-radius: 8px; background: transparent; color: #fff; cursor: pointer; }
    button:hover { background: rgba(255, 255, 255, .16); }
    @keyframes rise { from { transform: translateY(8px); opacity: 0; } to { transform: none; opacity: 1; } }
    @media (prefers-reduced-motion: reduce) { .toast { animation: none; } }
    @media (max-width: 560px) { .holder { left: 16px; right: 16px; bottom: 16px; max-width: none; } }
  `,
})
export class ToastOutletComponent {
  readonly toast = inject(ToastService);
}
