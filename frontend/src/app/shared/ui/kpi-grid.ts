import { Component, input } from '@angular/core';

export interface Kpi { label: string; value: string; hint?: string; }

/** Fila de tarjetas con una cifra grande en naranja. */
@Component({
  selector: 'app-kpi-grid',
  template: `
    <div class="grid-auto" style="--min: 220px">
      @for (k of items(); track k.label) {
        <div class="card accent kpi">
          <span class="lab">{{ k.label }}</span>
          <span class="kpi-v val">{{ k.value }}</span>
          @if (k.hint) { <span class="hint">{{ k.hint }}</span> }
        </div>
      }
    </div>
  `,
  styles: `
    .kpi { padding: 16px 18px; display: flex; flex-direction: column; gap: 6px; }
    .lab { font-size: 13px; font-weight: 700; color: var(--ink-4); }
    .val { font-size: 26px; font-weight: 800; }
    .hint { font-size: 12px; color: var(--or-800); font-weight: 600; }
  `,
})
export class KpiGridComponent {
  readonly items = input.required<Kpi[]>();
}
