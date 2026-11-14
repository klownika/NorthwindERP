import { Component, computed, input } from '@angular/core';

export interface DonutPart { label: string; value: number; color: string; }

/** Dona hecha con conic-gradient, con brillo "plástico" DORfic y leyenda. */
@Component({
  selector: 'app-donut',
  template: `
    <section class="card wrap">
      <h2 class="section-title">{{ title() }}</h2>
      <div class="row">
        <div class="ring" role="img" [attr.aria-label]="ariaLabel()" [style.background]="gradient()">
          <div class="gloss" aria-hidden="true"></div>
          <div class="hole">
            <span class="kpi-v center">{{ centerValue() }}</span>
            <span class="clabel">{{ centerLabel() }}</span>
          </div>
        </div>
        <div class="legend">
          @for (p of rows(); track p.label) {
            <div class="li"><span class="sw" [style.background]="p.color"></span><span class="name">{{ p.label }}</span><strong>{{ p.pct }}</strong></div>
          }
        </div>
      </div>
    </section>
  `,
  styles: `
    .wrap { padding: 20px; display: flex; flex-direction: column; gap: 16px; }
    .row { display: flex; flex-wrap: wrap; gap: 22px; align-items: center; }
    .ring { position: relative; flex: 0 0 200px; width: 200px; height: 200px; border-radius: 999px; box-shadow: 0 16px 30px -18px rgba(194,65,12,.9); }
    .gloss { position: absolute; inset: 0; border-radius: 999px; background: linear-gradient(180deg, rgba(255,255,255,.38), rgba(255,255,255,0) 46%); }
    .hole { position: absolute; inset: 36px; border-radius: 999px; background: radial-gradient(circle at 50% 30%, #fff, #fff1e4);
      box-shadow: inset 0 2px 8px rgba(194,65,12,.18); display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2px; }
    .center { font-size: 28px; font-weight: 800; }
    .clabel { font-size: 12px; color: var(--ink-3); text-align: center; max-width: 110px; }
    .legend { flex: 1 1 220px; min-width: 0; display: flex; flex-direction: column; gap: 9px; }
    .li { display: flex; align-items: center; gap: 10px; font-size: 14px; }
    .sw { flex: 0 0 auto; width: 14px; height: 14px; border-radius: 4px; box-shadow: inset 0 1px 0 rgba(255,255,255,.5); }
    .name { flex: 1 1 auto; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  `,
})
export class DonutComponent {
  readonly title = input.required<string>();
  readonly parts = input.required<DonutPart[]>();
  readonly centerValue = input('');
  readonly centerLabel = input('');
  readonly ariaLabel = input('Gráfico de dona');

  private readonly total = computed(() => this.parts().reduce((a, p) => a + p.value, 0) || 1);
  readonly rows = computed(() => this.parts().map((p) => ({ ...p, pct: ((p.value / this.total()) * 100).toFixed(1) + '%' })));
  readonly gradient = computed(() => {
    let acc = 0;
    const stops = this.parts().map((p) => {
      const start = acc;
      acc += (p.value / this.total()) * 100;
      return `${p.color} ${start.toFixed(2)}% ${Math.min(100, acc).toFixed(2)}%`;
    });
    return `conic-gradient(${stops.join(', ')})`;
  });
}
