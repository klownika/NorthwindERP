import { Component, computed, input } from '@angular/core';

export interface ColumnBar { value: number; color: string; title: string; }
export interface ColumnGroup { label: string; bars: ColumnBar[]; }
export interface LegendItem { label: string; color: string; }

/**
 * Columnas verticales (simples o agrupadas) hechas con CSS.
 * Cada grupo puede tener 1..n barras (por ejemplo, una por año).
 */
@Component({
  selector: 'app-column-chart',
  template: `
    <section class="card wrap">
      <div class="head">
        <h2 class="section-title">{{ title() }}</h2>
        @if (legend().length) {
          <div class="legend">
            @for (l of legend(); track l.label) { <span class="li"><span class="sw" [style.background]="l.color"></span>{{ l.label }}</span> }
          </div>
        }
      </div>
      <div class="scroll">
        <div class="plot" [style.height.px]="height()" [style.min-width.px]="minWidth()" [style.gap.px]="gap()">
          @for (g of groups(); track g.label) {
            <div class="group">
              @for (b of g.bars; track $index) {
                <div class="col" [title]="b.title" [style.height]="h(b.value)"
                     [style.background]="'linear-gradient(90deg, rgba(255,255,255,.42), rgba(255,255,255,0) 55%), ' + b.color"></div>
              }
            </div>
          }
        </div>
        <div class="labels" [style.min-width.px]="minWidth()" [style.gap.px]="gap()">
          @for (g of groups(); track g.label) { <span [class.vertical]="verticalLabels()">{{ g.label }}</span> }
        </div>
      </div>
    </section>
  `,
  styles: `
    .wrap { padding: 20px; display: flex; flex-direction: column; gap: 12px; }
    .head { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 10px; }
    .legend { display: flex; gap: 14px; }
    .li { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; font-weight: 700; }
    .sw { width: 14px; height: 14px; border-radius: 4px; }
    .scroll { overflow-x: auto; }
    .plot { display: flex; align-items: flex-end; padding: 0 6px; border-bottom: 2px solid var(--line);
      background: repeating-linear-gradient(0deg, transparent 0 59px, #fbe7d6 59px 60px); }
    .group { flex: 1 1 0; height: 100%; display: flex; align-items: flex-end; gap: 3px; }
    .col { flex: 1 1 0; min-height: 3px; border-radius: 7px 7px 0 0; box-shadow: inset 0 1px 0 rgba(255,255,255,.6); }
    .labels { display: flex; padding: 8px 6px 0; }
    .labels span { flex: 1 1 0; text-align: center; font-size: 12px; font-weight: 600; color: var(--ink-2); overflow: hidden; }
    .labels span.vertical { height: 74px; font-size: 11px; writing-mode: vertical-rl; transform: rotate(180deg); text-align: left; color: var(--ink-3); }
  `,
})
export class ColumnChartComponent {
  readonly title = input.required<string>();
  readonly groups = input.required<ColumnGroup[]>();
  readonly legend = input<LegendItem[]>([]);
  readonly height = input(240);
  readonly minWidth = input(620);
  readonly gap = input(10);
  readonly verticalLabels = input(false);

  private readonly max = computed(() => Math.max(1, ...this.groups().flatMap((g) => g.bars.map((b) => b.value))));
  h(v: number): string { return ((v / this.max()) * 100).toFixed(1) + '%'; }
}
