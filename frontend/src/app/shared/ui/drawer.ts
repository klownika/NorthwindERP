import { Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { IconComponent } from './icon';

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])';

/**
 * Panel lateral para ver o editar un registro sin salir de la lista.
 * Se cierra con la X, con Escape o al hacer clic fuera, y vuelve a la ruta `closeTo`.
 */
@Component({
  selector: 'app-drawer',
  imports: [IconComponent],
  host: { '(document:keydown.escape)': 'onEscape()' },
  template: `
    <div class="layer" [hidden]="!open()">
      <button type="button" class="backdrop" tabindex="-1" aria-label="Cerrar panel" (click)="close()"></button>
      <section #panel class="panel" role="dialog" aria-modal="true" [attr.aria-label]="label()" tabindex="-1" (keydown.tab)="trap($any($event))" (keydown.shift.tab)="trap($any($event))">
        <header class="head">
          <h2>{{ label() }}</h2>
          <button type="button" class="iconbtn" aria-label="Cerrar panel" title="Cerrar" (click)="close()"><app-icon name="close" /></button>
        </header>
        <div class="body"><ng-content /></div>
      </section>
    </div>
  `,
  styles: `
    .layer { position: fixed; inset: 0; z-index: 40; display: flex; justify-content: flex-end; }
    .layer[hidden] { display: none; }
    .backdrop { position: absolute; inset: 0; border: 0; padding: 0; background: rgba(28, 25, 23, .45); cursor: pointer; }
    .panel {
      position: relative; display: flex; flex-direction: column; width: min(920px, 100vw); height: 100%; outline: none;
      background: #fff; border-left: 3px solid var(--or-500);
      box-shadow: -18px 0 40px -22px rgba(124, 45, 18, .7); animation: slide .2s ease-out;
    }
    .head { flex: 0 0 auto; display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 12px 22px; background: #fff; border-bottom: 1px solid var(--line); }
    h2 { margin: 0; font-size: 19px; font-weight: 800; color: var(--or-800); }
    .body { flex: 1 1 auto; min-height: 0; overflow-y: auto; padding: 20px 22px 32px; }
    @keyframes slide { from { transform: translateX(32px); opacity: 0; } to { transform: none; opacity: 1; } }
    @media (prefers-reduced-motion: reduce) { .panel { animation: none; } }
    @media (max-width: 560px) { .head { padding: 10px 16px; } .body { padding: 16px 16px 28px; } }
  `,
})
export class DrawerComponent implements OnDestroy {
  private router = inject(Router);
  private panel = viewChild.required<ElementRef<HTMLElement>>('panel');
  private returnFocus: HTMLElement | null = null;

  readonly open = input.required<boolean>();
  readonly label = input.required<string>();
  readonly closeTo = input.required<string>();

  constructor() {
    effect(() => {
      const isOpen = this.open();
      document.body.style.overflow = isOpen ? 'hidden' : '';
      if (isOpen) {
        this.returnFocus = document.activeElement as HTMLElement | null;
        setTimeout(() => this.panel().nativeElement.focus());
      } else if (this.returnFocus) {
        const target = this.returnFocus;
        this.returnFocus = null;
        setTimeout(() => target.isConnected && target.focus());
      }
    });
  }

  ngOnDestroy(): void { document.body.style.overflow = ''; }

  close(): void { this.router.navigate([this.closeTo()]); }
  onEscape(): void { if (this.open()) this.close(); }

  /** Mantiene el foco del teclado dentro del panel mientras está abierto. */
  trap(e: KeyboardEvent): void {
    const panel = this.panel().nativeElement;
    const items = [...panel.querySelectorAll<HTMLElement>(FOCUSABLE)].filter((x) => x.offsetParent !== null);
    if (!items.length) return;
    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;
    if (e.shiftKey && (active === first || active === panel)) { e.preventDefault(); last.focus(); }
    else if (!e.shiftKey && active === last) { e.preventDefault(); first.focus(); }
  }
}
