import { DestroyRef, Directive, ElementRef, inject, input } from '@angular/core';

/** Un solo globo para toda la app, colgado de <body> para que nada lo recorte. */
let tip: HTMLDivElement | null = null;
let owner: TooltipDirective | null = null;
const GAP = 8;
const MARGIN = 8;

function tipEl(): HTMLDivElement {
  if (!tip) {
    tip = document.createElement('div');
    tip.className = 'app-tooltip';
    tip.id = 'app-tooltip';
    tip.setAttribute('role', 'tooltip');
    tip.hidden = true;
    document.body.appendChild(tip);
  }
  return tip;
}

/**
 * Tooltip al pasar el mouse, enfocar con teclado o tocar el elemento.
 *   <div [appTooltip]="['Perú', '$ 1,234.00', '0.3% del total']" tabindex="0"></div>
 * La primera línea va en negrita. Se ubica encima del ancla (o al costado si arriba no hay lugar),
 * sin taparla y sin salirse de la pantalla. `appTooltipAnchor` permite ubicarlo respecto de otro elemento.
 */
@Directive({
  selector: '[appTooltip]',
  host: {
    '(pointerenter)': 'onEnter($event)', '(pointerleave)': 'hide()', '(pointerdown)': 'onDown($event)',
    '(focus)': 'show()', '(blur)': 'hide()', '(keydown.escape)': 'hide()',
  },
})
export class TooltipDirective {
  private el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  readonly appTooltip = input<string | string[] | null>(null);
  readonly appTooltipAnchor = input<HTMLElement | null>(null);

  private readonly onOutside = (e: Event) => { if (!this.el.contains(e.target as Node)) this.hide(); };
  private readonly onScroll = () => this.hide();

  constructor() {
    inject(DestroyRef).onDestroy(() => this.hide());
  }

  onEnter(e: PointerEvent): void { if (e.pointerType !== 'touch') this.show(); }

  /** En pantallas táctiles, tocar muestra el globo y tocar fuera lo oculta. */
  onDown(e: PointerEvent): void { if (e.pointerType === 'touch') this.show(); }

  show(): void {
    const v = this.appTooltip();
    const lines = (Array.isArray(v) ? v : v ? [v] : []).filter(Boolean);
    if (!lines.length) return;
    const t = tipEl();
    t.replaceChildren(...lines.map((text, i) => {
      const s = document.createElement(i === 0 ? 'strong' : 'span');
      s.textContent = text;
      return s;
    }));
    owner = this;
    t.hidden = false;
    this.place(t);
    this.el.setAttribute('aria-describedby', t.id);
    document.addEventListener('pointerdown', this.onOutside, true);
    window.addEventListener('scroll', this.onScroll, true);
  }

  hide(): void {
    document.removeEventListener('pointerdown', this.onOutside, true);
    window.removeEventListener('scroll', this.onScroll, true);
    this.el.removeAttribute('aria-describedby');
    if (owner !== this || !tip) return;
    owner = null;
    tip.hidden = true;
  }

  private place(t: HTMLDivElement): void {
    const a = (this.appTooltipAnchor() ?? this.el).getBoundingClientRect();
    const w = t.offsetWidth;
    const h = t.offsetHeight;
    const vw = document.documentElement.clientWidth;
    const vh = window.innerHeight;
    const clamp = (v: number, max: number) => Math.max(MARGIN, Math.min(v, max - MARGIN));
    let left: number;
    let top: number;
    if (a.top - GAP - h >= MARGIN) {
      // Encima del ancla, centrado.
      top = a.top - GAP - h;
      left = clamp(a.left + a.width / 2 - w / 2, vw - w);
    } else {
      // Sin lugar arriba: al costado (derecha si cabe, si no izquierda), a la altura del borde superior.
      top = clamp(a.top, vh - h);
      left = a.right + GAP + w <= vw - MARGIN ? a.right + GAP : clamp(a.left - GAP - w, vw - w);
    }
    t.style.left = `${left}px`;
    t.style.top = `${top}px`;
  }
}
