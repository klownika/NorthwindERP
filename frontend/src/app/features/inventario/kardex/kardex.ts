import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { InventoryApi } from '../../../core/api/inventory.api';
import { KardexEntry, KardexType, Warehouse } from '../../../core/models/api.models';
import { fmtDateTime, int, norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { ComboOption, ComboboxComponent } from '../../../shared/ui/combobox';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/**
 * Kardex (InventoryTransactions). Los filtros de almacén y tipo van a la API.
 * El almacén vive en la URL (?w=): Almacenes > Ver Kardex llega filtrado y el sidebar (sin ?w) muestra todos.
 */
@Component({
  selector: 'app-kardex',
  imports: [FormsModule, ComboboxComponent, RouterLink, LoadStateComponent, PagerComponent],
  templateUrl: './kardex.html',
})
export class KardexComponent {
  private api = inject(InventoryApi);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  readonly load = new Loader();
  readonly rows = signal<KardexEntry[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly whOpts = computed<ComboOption[]>(() => this.warehouses().map((w) => ({ value: w.warehouseId, label: w.warehouseName })));
  readonly q = signal('');
  readonly wh = signal<number>(0);
  readonly type = signal<KardexType | null>(null);
  readonly types: { v: KardexType | null; label: string }[] = [
    { v: null, label: 'Todos' }, { v: 'INBOUND', label: 'INBOUND' }, { v: 'OUTBOUND', label: 'OUTBOUND' },
  ];
  readonly int = int;
  readonly fmt = fmtDateTime;
  readonly cols = '70px 150px minmax(180px, 1fr) 110px 90px 160px 170px 100px 210px';

  readonly filtered = computed(() => {
    const q = norm(this.q());
    return this.rows().filter((k) => !q || norm(k.productName).includes(q));
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    this.api.warehouses().subscribe({ next: (w) => this.warehouses.set(w) });
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((m) => {
      this.wh.set(Number(m.get('w')) || 0);
      this.reload();
    });
  }

  setWh(v: number): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { w: v || null }, queryParamsHandling: 'merge', replaceUrl: true });
  }
  setType(v: KardexType | null): void { this.type.set(v); this.reload(); }

  private reload(): void {
    this.load.run(this.api.kardex({ warehouseId: this.wh() || null, type: this.type() }), (r) =>
      this.rows.set([...r].sort((a, b) => b.transactionId - a.transactionId)));
  }
}
