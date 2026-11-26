import { NgTemplateOutlet } from '@angular/common';
import { Component, TemplateRef, computed, contentChild, input, model, signal } from '@angular/core';
import { Category, Product, StockItem } from '../../core/models/api.models';
import { int, money, norm } from '../../core/util/format';
import { GridColsDirective } from '../ui/grid-cols';
import { Pager, PagerComponent, fullRows } from '../ui/pager';

const TILES: [string, string][] = [
  ['#FFEDD5', '#9A3412'], ['#FEF3C7', '#92400E'], ['#FCE7F3', '#9D174D'], ['#E0F2FE', '#075985'],
  ['#ECFCCB', '#3F6212'], ['#FEE2E2', '#991B1B'], ['#DCFCE7', '#166534'], ['#E0E7FF', '#3730A3'],
];

/** Producto tal como lo pinta la tarjeta del catálogo. `qty` es el stock del almacén elegido (o el total). */
export interface CatalogItem extends Product {
  tile: string; ink: string; cname: string; abbr: string; priceTxt: string;
  qty: number; low: boolean; blocked: boolean; stockTxt: string;
}

let nextId = 0;

/**
 * Vista de catálogo: búsqueda, chips de categoría, paginación que llena filas completas y grilla de tarjetas.
 * La usan Inventario > Catálogo y el modal de productos de Nuevo pedido.
 *   <app-catalog-browser [products]="..." [categories]="..." [stock]="..." [warehouseId]="wh()" [(query)]="q">
 *     <div filters>…filtros extra…</div>
 *     <ng-template #cardFooter let-p>…controles bajo cada tarjeta…</ng-template>
 *   </app-catalog-browser>
 */
@Component({
  selector: 'app-catalog-browser',
  imports: [NgTemplateOutlet, GridColsDirective, PagerComponent],
  templateUrl: './catalog-browser.html',
  styleUrl: './catalog-browser.scss',
})
export class CatalogBrowserComponent {
  readonly searchId = `catalog-q-${nextId++}`;
  readonly products = input<Product[]>([]);
  readonly categories = input<Category[]>([]);
  readonly stock = input<StockItem[]>([]);
  /** 0 = todos los almacenes (stock total). */
  readonly warehouseId = input(0);
  /** Bloquea las tarjetas sin stock (en el modal de Nuevo pedido). */
  readonly blockEmpty = input(false);
  readonly loading = input(false);
  readonly query = model('');
  readonly cardFooter = contentChild<TemplateRef<{ $implicit: CatalogItem }>>('cardFooter');

  readonly cat = signal(0);
  readonly fullRows = fullRows;

  readonly items = computed<CatalogItem[]>(() => {
    const q = norm(this.query());
    const w = this.warehouseId();
    return this.products()
      .filter((p) => (!q || norm(p.productName).includes(q)) && (!this.cat() || p.categoryId === this.cat()))
      .sort((a, b) => a.productName.localeCompare(b.productName))
      .map((p) => {
        const rows = this.stock().filter((s) => s.productId === p.productId && (!w || s.warehouseId === w));
        const qty = rows.reduce((a, s) => a + s.quantity, 0);
        const low = rows.some((s) => s.needsRestock);
        const blocked = this.blockEmpty() && qty <= 0;
        const [tile, ink] = TILES[((p.categoryId ?? 1) - 1 + 8) % 8];
        const cname = p.categoryName ?? '—';
        return {
          ...p, tile, ink, cname, priceTxt: money(p.price), qty, low, blocked,
          abbr: cname.replace(/[^A-Za-z]/g, '').slice(0, 3).toUpperCase(),
          stockTxt: blocked ? 'Sin stock' : (w ? 'Stock: ' : 'Stock total: ') + int(qty) + ' u.' + (low ? ' · Reponer' : ''),
        };
      });
  });
  readonly pager = new Pager(this.items, 12);
}
