import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsApi } from '../../../core/api/reports.api';
import { EmployeeYearSales } from '../../../core/models/api.models';
import { money, pct } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { ColumnChartComponent, ColumnGroup, LegendItem } from '../../../shared/ui/column-chart';
import { Kpi, KpiGridComponent } from '../../../shared/ui/kpi-grid';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { YEAR_COLORS } from '../../../shared/ui/palette';

/** Ventas por empleado y año: columnas agrupadas por año + ranking del año elegido. */
@Component({
  selector: 'app-anio-empleados',
  imports: [FormsModule, KpiGridComponent, ColumnChartComponent, LoadStateComponent],
  template: `
    <div class="stack">
      <div class="fgroup" style="max-width: 200px">
        <label for="rye" class="lbl">Año</label>
        <select id="rye" class="field" [ngModel]="year()" (ngModelChange)="year.set(+$event)">
          @for (y of years(); track y) { <option [ngValue]="y">{{ y }}</option> }
        </select>
      </div>
      <app-load-state [loading]="load.loading()" [error]="load.error()" />
      <app-kpi-grid [items]="kpis()" />
      <app-column-chart title="Ventas por empleado y año" [groups]="groups()" [legend]="legend()" [height]="250" [minWidth]="720" [gap]="14" />
      <h2 class="section-title">Ranking del año</h2>
      <section class="card" style="padding: 20px; display: flex; flex-direction: column; gap: 16px">
        @for (r of ranking(); track r.employeeId) {
          <div style="display: grid; grid-template-columns: 180px minmax(0, 1fr) 140px; gap: 14px; align-items: center">
            <div style="display: flex; flex-direction: column"><span class="b" style="font-size: 14px">{{ r.employeeName }}</span>
              <span class="muted" style="font-size: 12px">{{ r.totalOrders }} pedidos · ticket {{ money(r.avgOrderValue) }}</span></div>
            <div class="track" style="height: 14px"><div class="bar" [style.width]="r.pct"></div></div>
            <div class="r b" style="font-size: 14px">{{ money(r.totalSales) }}</div>
          </div>
        } @empty { @if (!load.loading()) { <div class="empty">Sin ventas en este año.</div> } }
      </section>
    </div>
  `,
})
export class AnioEmpleadosComponent {
  private api = inject(ReportsApi);
  readonly load = new Loader();
  readonly all = signal<EmployeeYearSales[]>([]);
  readonly year = signal<number>(0);
  readonly money = money;

  readonly years = computed(() => [...new Set(this.all().map((r) => r.salesYear))].sort());
  readonly ofYear = computed(() => this.all().filter((r) => r.salesYear === this.year()).sort((a, b) => b.totalSales - a.totalSales));
  readonly ranking = computed(() => {
    const max = this.ofYear()[0]?.totalSales ?? 1;
    return this.ofYear().map((r) => ({ ...r, pct: pct(r.totalSales, max) }));
  });
  readonly kpis = computed<Kpi[]>(() => {
    const r = this.ofYear();
    const total = r.reduce((a, x) => a + x.totalSales, 0);
    const orders = r.reduce((a, x) => a + x.totalOrders, 0);
    return [
      { label: `Ventas ${this.year() || ''}`, value: money(total) },
      { label: 'Mejor vendedor', value: r[0]?.employeeName ?? '—' },
      { label: 'Ticket promedio', value: money(orders ? total / orders : 0) },
    ];
  });
  readonly legend = computed<LegendItem[]>(() => this.years().map((y, i) => ({ label: String(y), color: YEAR_COLORS[i % YEAR_COLORS.length] })));
  readonly groups = computed<ColumnGroup[]>(() => {
    const byEmp = new Map<number, { name: string; sales: Map<number, number> }>();
    this.all().forEach((r) => {
      const e = byEmp.get(r.employeeId) ?? { name: r.employeeName, sales: new Map<number, number>() };
      e.sales.set(r.salesYear, r.totalSales);
      byEmp.set(r.employeeId, e);
    });
    return [...byEmp.values()].map((e) => {
      const [first, last] = e.name.split(' ');
      return {
        label: last ? `${first} ${last[0]}.` : first,
        bars: this.years().map((y, i) => ({
          value: e.sales.get(y) ?? 0, color: YEAR_COLORS[i % YEAR_COLORS.length], title: `${e.name} · ${y}: ${money(e.sales.get(y) ?? 0)}`,
        })),
      };
    });
  });

  constructor() {
    this.load.run(this.api.salesByEmployeeYear(), (r) => {
      this.all.set(r);
      const ys = [...new Set(r.map((x) => x.salesYear))].sort();
      this.year.set(ys[ys.length - 1] ?? 0);
    });
  }
}
