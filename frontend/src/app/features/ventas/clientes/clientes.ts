import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterOutlet } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ReportsApi } from '../../../core/api/reports.api';
import { SalesApi } from '../../../core/api/sales.api';
import { Customer, CustomerSales } from '../../../core/models/api.models';
import { norm } from '../../../core/util/format';
import { PageService } from '../../../layout/page.service';
import { Loader } from '../../../shared/data/loader';
import { DrawerComponent } from '../../../shared/ui/drawer';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Lista de clientes con su número de pedidos (vw_sales_by_customer). */
@Component({
  selector: 'app-clientes',
  imports: [FormsModule, RouterLink, RouterOutlet, DrawerComponent, IconComponent, LoadStateComponent, PagerComponent],
  templateUrl: './clientes.html',
})
export class ClientesComponent {
  private sales = inject(SalesApi);
  private reports = inject(ReportsApi);
  private page = inject(PageService);
  readonly detailOpen = signal(false);
  readonly load = new Loader();
  readonly customers = signal<Customer[]>([]);
  readonly ranking = signal<CustomerSales[]>([]);
  readonly q = signal('');
  readonly cols = '60px minmax(200px, 1fr) 170px 130px 110px 80px 96px';

  readonly filtered = computed(() => {
    const q = norm(this.q());
    const orders = new Map(this.ranking().map((r) => [r.customerId, r.totalOrders]));
    return this.customers()
      .filter((c) => !q || norm(`${c.customerName} ${c.city} ${c.country}`).includes(q))
      .map((c) => ({ ...c, orders: orders.get(c.customerId) ?? 0 }));
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    effect(() => {
      this.page.dataVersion();
      untracked(() => this.reload());
    });
  }

  private reload(): void {
    this.load.run(forkJoin({ c: this.sales.customers(), r: this.reports.salesByCustomer() }), (x) => {
      this.customers.set(x.c);
      this.ranking.set(x.r);
    });
  }
}
