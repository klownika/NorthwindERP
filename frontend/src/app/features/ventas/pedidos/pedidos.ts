import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SalesApi } from '../../../core/api/sales.api';
import { StaffApi } from '../../../core/api/staff.api';
import { Employee, OrderSummary } from '../../../core/models/api.models';
import { fmtDate, money, norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Lista de pedidos. El filtro de vendedor va a la API (employeeId); cliente y año se filtran aquí. */
@Component({
  selector: 'app-pedidos',
  imports: [FormsModule, RouterLink, LoadStateComponent, PagerComponent],
  templateUrl: './pedidos.html',
})
export class PedidosComponent {
  private sales = inject(SalesApi);
  private staff = inject(StaffApi);

  readonly load = new Loader();
  readonly orders = signal<OrderSummary[]>([]);
  readonly employees = signal<Employee[]>([]);
  readonly q = signal('');
  readonly emp = signal<number>(0);
  readonly year = signal<string>('');
  readonly money = money;
  readonly fmtDate = fmtDate;
  readonly cols = '100px 120px minmax(220px, 1fr) 170px 150px 80px 130px';

  readonly years = computed(() => [...new Set(this.orders().map((o) => o.orderDate.slice(0, 4)))].sort());
  readonly filtered = computed(() => {
    const q = norm(this.q());
    return this.orders()
      .filter((o) => (!q || norm(o.customerName).includes(q)) && (!this.year() || o.orderDate.startsWith(this.year())))
      .sort((a, b) => b.orderDate.localeCompare(a.orderDate) || b.orderId - a.orderId);
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    this.staff.employees().subscribe({ next: (e) => this.employees.set(e) });
    this.reload();
  }

  setEmp(v: number): void { this.emp.set(v); this.reload(); }

  private reload(): void {
    this.load.run(this.sales.orders({ employeeId: this.emp() || null }), (r) => this.orders.set(r));
  }
}
