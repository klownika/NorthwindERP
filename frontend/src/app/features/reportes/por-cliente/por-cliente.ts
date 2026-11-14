import { Component, computed, inject, signal } from '@angular/core';
import { ReportsApi } from '../../../core/api/reports.api';
import { CustomerSales } from '../../../core/models/api.models';
import { int, money, pct } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { DonutComponent, DonutPart } from '../../../shared/ui/donut';
import { Kpi, KpiGridComponent } from '../../../shared/ui/kpi-grid';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';
import { PALETTE } from '../../../shared/ui/palette';

/** vw_sales_by_customer: KPIs, top 10, dona de concentración y tabla paginada. */
@Component({
  selector: 'app-por-cliente',
  imports: [KpiGridComponent, DonutComponent, LoadStateComponent, PagerComponent],
  templateUrl: './por-cliente.html',
  styleUrl: './por-cliente.scss',
})
export class PorClienteComponent {
  private api = inject(ReportsApi);
  readonly load = new Loader();
  readonly rows = signal<CustomerSales[]>([]);
  readonly money = money;
  readonly int = int;
  readonly cols = '60px minmax(220px, 1fr) 120px 90px 90px 130px 200px';

  readonly ranked = computed(() => {
    const r = [...this.rows()].sort((a, b) => b.totalSales - a.totalSales);
    const max = r[0]?.totalSales ?? 1;
    return r.map((x, i) => ({ ...x, rank: i + 1, pct: pct(x.totalSales, max) }));
  });
  readonly total = computed(() => this.rows().reduce((a, r) => a + r.totalSales, 0));
  readonly kpis = computed<Kpi[]>(() => [
    { label: 'Ventas totales', value: money(this.total()) },
    { label: 'Clientes con compras', value: int(this.rows().length) },
    { label: 'Venta promedio por cliente', value: money(this.rows().length ? this.total() / this.rows().length : 0) },
  ]);
  readonly top10 = computed(() => this.ranked().slice(0, 10));
  readonly parts = computed<DonutPart[]>(() => {
    const r = this.ranked();
    const top = r.slice(0, 5).map((x, i) => ({ label: x.customerName, value: x.totalSales, color: PALETTE[i] }));
    const rest = r.slice(5).reduce((a, x) => a + x.totalSales, 0);
    return rest > 0 ? [...top, { label: `Resto (${r.length - 5} clientes)`, value: rest, color: PALETTE[6] }] : top;
  });
  readonly top5Share = computed(() => (this.total() ? (this.parts().slice(0, 5).reduce((a, p) => a + p.value, 0) / this.total()) * 100 : 0).toFixed(0) + '%');
  readonly pager = new Pager(this.ranked, 10);

  constructor() {
    this.load.run(this.api.salesByCustomer(), (r) => this.rows.set(r));
  }
}
