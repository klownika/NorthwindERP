import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon';
import { LowStockService } from '../low-stock.service';

/**
 * Ícono de alerta del header con contador y panel desplegable de notificaciones:
 * una por cada producto por reponer. Cada una lleva a Stock con "Solo por reponer" activo (?low=1).
 * Se cierra al hacer clic fuera, con Esc o al elegir una notificación.
 */
@Component({
  selector: 'app-notifications',
  imports: [RouterLink, IconComponent],
  host: { '(document:keydown.escape)': 'onEscape()', '(document:pointerdown)': 'onDocPointer($event)' },
  template: `
    <button #btn type="button" class="iconbtn" aria-controls="notif-panel" [attr.aria-expanded]="open()" (click)="toggle()"
            [attr.aria-label]="'Notificaciones: ' + (lowStock.count() ? lowStock.count() + ' productos por reponer' : 'sin productos por reponer')">
      <app-icon name="alert" />
      @if (lowStock.count()) { <span class="badge" aria-hidden="true">{{ lowStock.count() }}</span> }
    </button>
    @if (open()) {
      <section id="notif-panel" class="panel" aria-labelledby="notif-title">
        <header class="head">
          <h2 id="notif-title">Notificaciones</h2>
          @if (lowStock.count()) { <span class="count">{{ lowStock.count() }}</span> }
        </header>
        @if (lowStock.items().length) {
          <ul class="list">
            @for (n of lowStock.items(); track n.warehouseId + '-' + n.productId) {
              <li>
                <a class="item" routerLink="/inventario/stock" [queryParams]="{ low: 1 }" (click)="open.set(false)">
                  <span class="dot" aria-hidden="true"></span>
                  <span class="txt">
                    <strong>{{ n.productName }} necesita reponer</strong>
                    <span>{{ n.warehouseName }} · stock {{ n.quantity }} de mínimo {{ n.minStockLevel }}</span>
                  </span>
                </a>
              </li>
            }
          </ul>
        } @else {
          <p class="none">No hay productos por reponer</p>
        }
      </section>
    }
  `,
  styles: `
    :host { position: relative; display: inline-flex; }
    .badge {
      position: absolute; top: -6px; right: -6px; min-width: 20px; height: 20px; padding: 0 6px; border-radius: 999px;
      background: #b91c1c; color: #fff; font-size: 11px; font-weight: 800; display: inline-flex; align-items: center; justify-content: center;
    }
    .panel {
      position: absolute; top: calc(100% + 10px); right: 0; z-index: 20; width: min(360px, calc(100vw - 32px));
      max-height: min(440px, calc(100dvh - 100px)); display: flex; flex-direction: column; overflow: hidden;
      background: #fff; border: 1px solid var(--line); border-radius: 16px; box-shadow: 0 18px 40px -20px rgba(124, 45, 18, .5);
    }
    .head {
      flex: 0 0 auto; display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 12px 16px;
      border-bottom: 1px solid var(--line-2);
    }
    h2 { margin: 0; font-size: 16px; font-weight: 800; color: var(--ink); }
    .count {
      min-width: 24px; height: 24px; padding: 0 8px; border-radius: 999px; display: inline-flex; align-items: center; justify-content: center;
      background: var(--bad-bg); color: var(--bad-ink); font-size: 12px; font-weight: 800;
    }
    .list { flex: 1 1 auto; min-height: 0; margin: 0; padding: 6px; list-style: none; overflow-y: auto; scrollbar-width: thin; }
    .item { display: flex; align-items: flex-start; gap: 10px; padding: 10px 12px; border-radius: 12px; text-decoration: none; color: var(--ink); }
    .item:hover, .item:focus-visible { background: #fff1e4; color: var(--ink); }
    .dot { flex: 0 0 auto; width: 8px; height: 8px; margin-top: 6px; border-radius: 999px; background: #b91c1c; }
    .txt { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .txt strong { font-size: 14px; font-weight: 700; overflow-wrap: anywhere; }
    .txt span { font-size: 12px; color: var(--ink-3); }
    .none { margin: 0; padding: 24px 16px; text-align: center; font-size: 14px; color: var(--ink-3); }
    /* En pantallas chicas el panel se alinea al borde derecho de las acciones del header (que es position: relative). */
    @media (max-width: 560px) { :host { position: static; } }
  `,
})
export class NotificationsComponent {
  private host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private btn = viewChild.required<ElementRef<HTMLButtonElement>>('btn');
  readonly lowStock = inject(LowStockService);
  readonly open = signal(false);

  /** Al abrir se vuelve a consultar el stock para mostrar datos al día. */
  toggle(): void {
    if (!this.open()) this.lowStock.refresh();
    this.open.update((v) => !v);
  }

  onEscape(): void {
    if (!this.open()) return;
    this.open.set(false);
    this.btn().nativeElement.focus();
  }

  onDocPointer(e: PointerEvent): void {
    if (this.open() && !this.host.contains(e.target as Node)) this.open.set(false);
  }
}
