import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ReportsApi } from '../../../core/api/reports.api';
import { SalesApi } from '../../../core/api/sales.api';
import { Customer, CustomerSales } from '../../../core/models/api.models';
import { norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Lista de clientes con su número de pedidos (vw_sales_by_customer). */
@Component({
  selector: 'app-clientes',
  imports: [FormsModule, RouterLink, LoadStateComponent, PagerComponent],
  templateUrl: './clientes.html',
})
export class ClientesComponent {
  private sales = inject(SalesApi);
  private reports = inject(ReportsApi);
  readonly load = new Loader();
  readonly customers = signal<Customer[]>([]);
  readonly ranking = signal<CustomerSales[]>([]);
  readonly q = signal('');
  readonly cols = '70px minmax(220px, 1fr) 190px 150px 120px 90px 100px';

  readonly filtered = computed(() => {
    const q = norm(this.q());
    const orders = new Map(this.ranking().map((r) => [r.customerId, r.totalOrders]));
    return this.customers()
      .filter((c) => !q || norm(`${c.customerName} ${c.city} ${c.country}`).includes(q))
      .map((c) => ({ ...c, orders: orders.get(c.customerId) ?? 0 }));
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    this.load.run(forkJoin({ c: this.sales.customers(), r: this.reports.salesByCustomer() }), (x) => {
      this.customers.set(x.c);
      this.ranking.set(x.r);
    });
  }
}
