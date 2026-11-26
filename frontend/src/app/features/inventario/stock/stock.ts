import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StockItem, Warehouse } from '../../../core/models/api.models';
import { int, norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { ComboOption, ComboboxComponent } from '../../../shared/ui/combobox';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/**
 * Stock por almacén (vw_stock_status) con filtro de "por reponer".
 * Los filtros de almacén (?w=) y "Solo por reponer" (?low=1) viven en la URL: los usan Almacenes y las notificaciones del header.
 */
@Component({
  selector: 'app-stock',
  imports: [FormsModule, ComboboxComponent, LoadStateComponent, PagerComponent],
  templateUrl: './stock.html',
})
export class StockComponent {
  private api = inject(InventoryApi);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  readonly load = new Loader();
  readonly rows = signal<StockItem[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly whOpts = computed<ComboOption[]>(() => this.warehouses().map((w) => ({ value: w.warehouseId, label: w.warehouseName })));
  readonly q = signal('');
  readonly wh = signal<number>(0);
  readonly lowOnly = signal(false);
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
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((m) => {
      this.wh.set(Number(m.get('w')) || 0);
      this.lowOnly.set(m.has('low'));
    });
  }

  setWh(v: number): void { this.setParams({ w: v || null }); }
  toggleLow(): void { this.setParams({ low: this.lowOnly() ? null : 1 }); }

  private setParams(queryParams: Record<string, number | null>): void {
    this.router.navigate([], { relativeTo: this.route, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
  }
}
