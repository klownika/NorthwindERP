import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterOutlet } from '@angular/router';
import { SalesApi } from '../../../core/api/sales.api';
import { StaffApi } from '../../../core/api/staff.api';
import { AuthService } from '../../../core/auth/auth.service';
import { Employee, OrderSummary } from '../../../core/models/api.models';
import { fmtDate, money, norm } from '../../../core/util/format';
import { UiError, toUiError } from '../../../core/util/problem';
import { LowStockService } from '../../../layout/low-stock.service';
import { PageService } from '../../../layout/page.service';
import { Loader } from '../../../shared/data/loader';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog';
import { DrawerComponent } from '../../../shared/ui/drawer';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';
import { ToastService } from '../../../shared/ui/toast';

/** Lista de pedidos. El filtro de vendedor va a la API (employeeId); cliente y año se filtran aquí. */
@Component({
  selector: 'app-pedidos',
  imports: [FormsModule, RouterLink, RouterOutlet, ConfirmDialogComponent, DrawerComponent, IconComponent, LoadStateComponent, PagerComponent],
  templateUrl: './pedidos.html',
})
export class PedidosComponent {
  private sales = inject(SalesApi);
  private staff = inject(StaffApi);
  private page = inject(PageService);
  private lowStock = inject(LowStockService);
  private toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly toDelete = signal<OrderSummary | null>(null);
  readonly deleting = signal(false);
  readonly deleteError = signal<UiError | null>(null);
  readonly detailOpen = signal(false);
  readonly load = new Loader();
  readonly orders = signal<OrderSummary[]>([]);
  readonly employees = signal<Employee[]>([]);
  readonly q = signal('');
  readonly emp = signal<number>(0);
  readonly year = signal<string>('');
  readonly money = money;
  readonly fmtDate = fmtDate;
  readonly cols = '88px 120px minmax(170px, 1fr) 150px 130px 64px 112px 96px';

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
    effect(() => {
      this.page.dataVersion();
      untracked(() => this.reload());
    });
  }

  setEmp(v: number): void { this.emp.set(v); this.reload(); }

  askDelete(o: OrderSummary): void {
    this.deleteError.set(null);
    this.toDelete.set(o);
  }

  cancelDelete(): void { this.toDelete.set(null); }

  confirmDelete(): void {
    const o = this.toDelete();
    if (!o) return;
    this.deleting.set(true);
    this.deleteError.set(null);
    this.sales.deleteOrder(o.orderId).subscribe({
      next: () => {
        // Se quita la fila sin recargar para que la tabla se quede en la misma página.
        const current = this.pager.current();
        this.orders.update((rows) => rows.filter((x) => x.orderId !== o.orderId));
        this.pager.page.set(current);
        this.lowStock.refresh();
        this.toast.show(`Pedido #${o.orderId} eliminado.`);
        this.toDelete.set(null);
        this.deleting.set(false);
      },
      error: (e: unknown) => {
        this.deleteError.set(toUiError(e));
        this.deleting.set(false);
      },
    });
  }

  private reload(): void {
    this.load.run(this.sales.orders({ employeeId: this.emp() || null }), (r) => this.orders.set(r));
  }
}
