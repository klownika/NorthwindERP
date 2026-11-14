import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { SalesApi } from '../../../core/api/sales.api';
import { AuthService } from '../../../core/auth/auth.service';
import { KardexEntry, OrderDetail } from '../../../core/models/api.models';
import { fmtDate, int, money } from '../../../core/util/format';
import { UiError, toUiError } from '../../../core/util/problem';
import { PageService } from '../../../layout/page.service';
import { LowStockService } from '../../../layout/low-stock.service';
import { Loader } from '../../../shared/data/loader';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';

/** Sub-subinterfaz: detalle de un pedido + eliminar (sp_delete_order, solo Admin). */
@Component({
  selector: 'app-pedido-detalle',
  imports: [RouterLink, IconComponent, LoadStateComponent],
  templateUrl: './pedido-detalle.html',
  styleUrl: './pedido-detalle.scss',
})
export class PedidoDetalleComponent {
  private sales = inject(SalesApi);
  private inventory = inject(InventoryApi);
  private router = inject(Router);
  private page = inject(PageService);
  private lowStock = inject(LowStockService);
  readonly auth = inject(AuthService);

  readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));
  readonly load = new Loader();
  readonly detail = signal<OrderDetail | null>(null);
  readonly moves = signal<KardexEntry[]>([]);
  readonly confirming = signal(false);
  readonly deleting = signal(false);
  readonly deleteError = signal<UiError | null>(null);
  /** Mensaje "201 Created" cuando venimos de Nuevo pedido. */
  readonly flash = signal<string | null>((history.state?.created as string | undefined) ?? null);

  readonly money = money;
  readonly int = int;
  readonly cols = 'minmax(220px, 1fr) 160px 100px 120px 140px';

  readonly facts = computed(() => {
    const o = this.detail()?.order;
    if (!o) return [];
    return [
      { label: 'Cliente', value: o.customerName },
      { label: 'Fecha', value: fmtDate(o.orderDate) },
      { label: 'Vendedor', value: o.employeeName },
      { label: 'Transportista', value: o.shipperName ?? 'Sin asignar' },
      { label: 'Total', value: money(o.orderTotal) },
    ];
  });

  constructor() {
    this.page.setDetail(`Pedido #${this.id}`);
    this.load.run(
      forkJoin({ d: this.sales.order(this.id), k: this.inventory.kardex({ type: 'OUTBOUND' }) }),
      (r) => {
        this.detail.set(r.d);
        this.moves.set(r.k.filter((m) => m.orderId === this.id));
      },
    );
  }

  remove(): void {
    this.deleting.set(true);
    this.deleteError.set(null);
    this.sales.deleteOrder(this.id).subscribe({
      next: () => {
        this.lowStock.refresh();
        this.router.navigate(['/ventas/pedidos']);
      },
      error: (e: unknown) => {
        this.deleteError.set(toUiError(e));
        this.deleting.set(false);
        this.confirming.set(false);
      },
    });
  }
}
