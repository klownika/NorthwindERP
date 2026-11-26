import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { Category, Product, StockItem, Warehouse } from '../../../core/models/api.models';
import { Loader } from '../../../shared/data/loader';
import { CatalogBrowserComponent } from '../../../shared/catalog/catalog-browser';
import { ComboOption, ComboboxComponent } from '../../../shared/ui/combobox';
import { LoadStateComponent } from '../../../shared/ui/load-state';

/** Catálogo de productos en tarjetas. Cada página llena filas completas según las columnas visibles. */
@Component({
  selector: 'app-catalogo',
  imports: [FormsModule, CatalogBrowserComponent, ComboboxComponent, LoadStateComponent],
  templateUrl: './catalogo.html',
  styleUrl: './catalogo.scss',
})
export class CatalogoComponent {
  private api = inject(InventoryApi);

  readonly load = new Loader();
  readonly products = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly stock = signal<StockItem[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly whOpts = computed<ComboOption[]>(() => this.warehouses().map((w) => ({ value: w.warehouseId, label: w.warehouseName })));

  readonly q = signal(inject(ActivatedRoute).snapshot.queryParamMap.get('q') ?? '');
  readonly wh = signal<number>(0);

  constructor() {
    this.load.run(
      forkJoin({ p: this.api.products(), c: this.api.categories(), s: this.api.stock(), w: this.api.warehouses() }),
      (r) => { this.products.set(r.p); this.categories.set(r.c); this.stock.set(r.s); this.warehouses.set(r.w); },
    );
  }
}
