import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StockItem, Warehouse } from '../../../core/models/api.models';
import { int } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';

/** Almacenes con su responsable y un resumen de su stock. */
@Component({
  selector: 'app-almacenes',
  imports: [RouterLink, IconComponent, LoadStateComponent],
  templateUrl: './almacenes.html',
  styleUrl: './almacenes.scss',
})
export class AlmacenesComponent {
  private api = inject(InventoryApi);
  readonly load = new Loader();
  readonly warehouses = signal<Warehouse[]>([]);
  readonly stock = signal<StockItem[]>([]);

  readonly cards = computed(() =>
    this.warehouses().map((w) => {
      const rows = this.stock().filter((s) => s.warehouseId === w.warehouseId);
      const low = rows.filter((s) => s.needsRestock).length;
      return { ...w, products: rows.length, units: int(rows.reduce((a, s) => a + s.quantity, 0)), low };
    }),
  );

  constructor() {
    this.load.run(forkJoin({ w: this.api.warehouses(), s: this.api.stock() }), (r) => {
      this.warehouses.set(r.w);
      this.stock.set(r.s);
    });
  }
}
