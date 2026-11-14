import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StockItem, Warehouse } from '../../../core/models/api.models';
import { int, norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Stock por almacén (vw_stock_status) con filtro de "por reponer". */
@Component({
  selector: 'app-stock',
  imports: [FormsModule, LoadStateComponent, PagerComponent],
  templateUrl: './stock.html',
})
export class StockComponent {
  private api = inject(InventoryApi);
  private route = inject(ActivatedRoute);

  readonly load = new Loader();
  readonly rows = signal<StockItem[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly q = signal('');
  readonly wh = signal<number>(Number(this.route.snapshot.queryParamMap.get('w') ?? 0));
  readonly lowOnly = signal(this.route.snapshot.queryParamMap.has('low'));
  readonly int = int;

  readonly filtered = computed(() => {
    const q = norm(this.q());
    return this.rows().filter((s) =>
      (!this.wh() || s.warehouseId === this.wh()) && (!this.lowOnly() || s.needsRestock) && (!q || norm(s.productName).includes(q)));
  });
  readonly pager = new Pager(this.filtered, 10);
  readonly cols = '200px minmax(0, 1fr) 110px 110px 120px 130px';

  constructor() {
    this.load.run(forkJoin({ s: this.api.stock(), w: this.api.warehouses() }), (r) => {
      this.rows.set(r.s);
      this.warehouses.set(r.w);
    });
  }
}
