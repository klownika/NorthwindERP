import { Component, computed, input } from '@angular/core';

export type GlossyIconName = 'home' | 'box' | 'bag' | 'users' | 'chart' | 'warehouse' | 'truck';

/**
 * Lienzo de los PNG (192 px) dividido por el recuadro visible (140 px). El recuadro está centrado
 * y lo que sobresale (asas, monedas, flecha, lupa) cae dentro del lienzo.
 */
const CANVAS_RATIO = 192 / 140;

/**
 * Iconos 3D de módulo (PNG en public/icons). `size` es el lado del recuadro visible: el host
 * ocupa exactamente size × size y la imagen se dibuja centrada y más grande, sin mover el diseño.
 * Uso: <app-gicon name="box" [size]="52" />
 */
@Component({
  selector: 'app-gicon',
  template: `<img [src]="'icons/' + name() + '.png'" alt="" aria-hidden="true" [width]="px()" [height]="px()" />`,
  host: { '[style.width.px]': 'size()', '[style.height.px]': 'size()' },
  styles: `
    :host { position: relative; display: inline-block; flex: 0 0 auto; }
    img { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); max-width: none; pointer-events: none; }
  `,
})
export class GlossyIconComponent {
  readonly name = input.required<GlossyIconName>();
  readonly size = input(48);
  readonly px = computed(() => Math.round(this.size() * CANVAS_RATIO));
}
