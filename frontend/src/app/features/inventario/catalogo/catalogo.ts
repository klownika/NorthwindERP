import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { Category, Product, StockItem, Warehouse } from '../../../core/models/api.models';
import { int, money, norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

const TILES: [string, string][] = [
  ['#FFEDD5', '#9A3412'], ['#FEF3C7', '#92400E'], ['#FCE7F3', '#9D174D'], ['#E0F2FE', '#075985'],
  ['#ECFCCB', '#3F6212'], ['#FEE2E2', '#991B1B'], ['#DCFCE7', '#166534'], ['#E0E7FF', '#3730A3'],
];

/** Catálogo de productos en tarjetas, 8 por página. */
@Component({
  selector: 'app-catalogo',
  imports: [FormsModule, LoadStateComponent, PagerComponent],
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

  readonly q = signal(inject(ActivatedRoute).snapshot.queryParamMap.get('q') ?? '');
  readonly cat = signal<number>(0);
  readonly wh = signal<number>(0);

  readonly items = computed(() => {
    const q = norm(this.q());
    const w = this.wh();
    return this.products()
      .filter((p) => (!q || norm(p.productName).includes(q)) && (!this.cat() || p.categoryId === this.cat()))
      .sort((a, b) => a.productName.localeCompare(b.productName))
      .map((p) => {
        const rows = this.stock().filter((s) => s.productId === p.productId && (!w || s.warehouseId === w));
        const qty = rows.reduce((a, s) => a + s.quantity, 0);
        const low = rows.some((s) => s.needsRestock);
        const [tile, ink] = TILES[((p.categoryId ?? 1) - 1 + 8) % 8];
        const cname = p.categoryName ?? '—';
        return {
          ...p, tile, ink, cname, priceTxt: money(p.price),
          abbr: cname.replace(/[^A-Za-z]/g, '').slice(0, 3).toUpperCase(),
          stockTxt: (w ? 'Stock: ' : 'Stock total: ') + int(qty) + ' u.' + (low ? ' · Reponer' : ''), low,
        };
      });
  });
  readonly pager = new Pager(this.items, 8);

  constructor() {
    this.load.run(
      forkJoin({ p: this.api.products(), c: this.api.categories(), s: this.api.stock(), w: this.api.warehouses() }),
      (r) => { this.products.set(r.p); this.categories.set(r.c); this.stock.set(r.s); this.warehouses.set(r.w); },
    );
  }
}
