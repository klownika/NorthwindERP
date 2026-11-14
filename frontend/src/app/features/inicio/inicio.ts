import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../core/api/inventory.api';
import { ReportsApi } from '../../core/api/reports.api';
import { SalesApi } from '../../core/api/sales.api';
import { CustomerSales, OrderSummary, StockItem } from '../../core/models/api.models';
import { fmtDate, int, money, pct } from '../../core/util/format';
import { Loader } from '../../shared/data/loader';
import { LoadStateComponent } from '../../shared/ui/load-state';

/** Inicio: KPIs, últimos pedidos y top 5 clientes. */
@Component({
  selector: 'app-inicio',
  imports: [RouterLink, LoadStateComponent],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
})
export class InicioComponent {
  private sales = inject(SalesApi);
  private reports = inject(ReportsApi);
  private inventory = inject(InventoryApi);

  readonly load = new Loader();
  readonly orders = signal<OrderSummary[]>([]);
  readonly ranking = signal<CustomerSales[]>([]);
  readonly low = signal<StockItem[]>([]);
  readonly customersCount = signal(0);

  readonly fmtDate = fmtDate;
  readonly money = money;

  readonly totalSales = computed(() => this.ranking().reduce((a, r) => a + r.totalSales, 0));
  readonly recent = computed(() =>
    [...this.orders()].sort((a, b) => b.orderDate.localeCompare(a.orderDate) || b.orderId - a.orderId).slice(0, 5),
  );
  readonly top = computed(() => {
    const r = [...this.ranking()].sort((a, b) => b.totalSales - a.totalSales).slice(0, 5);
    const max = r[0]?.totalSales ?? 1;
    return r.map((x) => ({ ...x, pct: pct(x.totalSales, max) }));
  });
  readonly kpis = computed(() => [
    { label: 'Ventas totales', value: money(this.totalSales()), hint: 'Ver ventas por cliente', link: '/reportes/por-cliente', q: null },
    { label: 'Pedidos', value: int(this.orders().length), hint: 'Ver pedidos', link: '/ventas/pedidos', q: null },
    { label: 'Clientes con compras', value: `${int(this.ranking().length)} / ${int(this.customersCount())}`, hint: 'Ver clientes', link: '/ventas/clientes', q: null },
    {
      label: 'Productos por reponer', value: int(this.low().length),
      hint: this.low().length ? 'Ver stock bajo' : 'Todo el stock sobre el mínimo', link: '/inventario/stock', q: { low: 1 },
    },
  ]);

  constructor() {
    this.load.run(
      forkJoin({
        orders: this.sales.orders(),
        ranking: this.reports.salesByCustomer(),
        low: this.inventory.stock({ lowStockOnly: true }),
        customers: this.sales.customers(),
      }),
      (r) => {
        this.orders.set(r.orders);
        this.ranking.set(r.ranking);
        this.low.set(r.low);
        this.customersCount.set(r.customers.length);
      },
    );
  }
}
