import { Component, computed, inject, signal } from '@angular/core';
import { ReportsApi } from '../../../core/api/reports.api';
import { CountrySales } from '../../../core/models/api.models';
import { money, pct } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { ColumnChartComponent, ColumnGroup } from '../../../shared/ui/column-chart';
import { DonutComponent, DonutPart } from '../../../shared/ui/donut';
import { Kpi, KpiGridComponent } from '../../../shared/ui/kpi-grid';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { PALETTE } from '../../../shared/ui/palette';

/** vw_sales_by_country: KPIs, dona, columnas y ranking. */
@Component({
  selector: 'app-por-pais',
  imports: [KpiGridComponent, DonutComponent, ColumnChartComponent, LoadStateComponent],
  templateUrl: './por-pais.html',
})
export class PorPaisComponent {
  private api = inject(ReportsApi);
  readonly load = new Loader();
  readonly rows = signal<CountrySales[]>([]);
  readonly money = money;

  readonly sorted = computed(() => [...this.rows()].sort((a, b) => b.totalSales - a.totalSales));
  readonly total = computed(() => this.rows().reduce((a, r) => a + r.totalSales, 0));
  readonly kpis = computed<Kpi[]>(() => {
    const s = this.sorted();
    const top3 = s.slice(0, 3).reduce((a, x) => a + x.totalSales, 0);
    return [
      { label: 'Países con ventas', value: String(s.length) },
      { label: 'País líder', value: s[0] ? `${s[0].country} · ${money(s[0].totalSales)}` : '—' },
      { label: 'Top 3 países', value: (this.total() ? (top3 / this.total()) * 100 : 0).toFixed(1) + '% de las ventas' },
    ];
  });
  readonly parts = computed<DonutPart[]>(() => {
    const s = this.sorted();
    const top = s.slice(0, 6).map((x, i) => ({ label: x.country ?? '—', value: x.totalSales, color: PALETTE[i] }));
    const rest = s.slice(6).reduce((a, x) => a + x.totalSales, 0);
    return rest > 0 ? [...top, { label: `Otros (${s.length - 6} países)`, value: rest, color: PALETTE[6] }] : top;
  });
  readonly columns = computed<ColumnGroup[]>(() =>
    this.sorted().map((x, i) => ({
      label: x.country ?? '—',
      bars: [{ value: x.totalSales, title: `${x.country}: ${money(x.totalSales)}`, color: i < 3 ? '#C2410C' : i < 8 ? '#F97316' : '#FDBA74' }],
    })),
  );
  readonly ranking = computed(() => {
    const max = this.sorted()[0]?.totalSales ?? 1;
    return this.sorted().map((x) => ({ ...x, pct: pct(x.totalSales, max) }));
  });

  constructor() {
    this.load.run(this.api.salesByCountry(), (r) => this.rows.set(r));
  }
}
