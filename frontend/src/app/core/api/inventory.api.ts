import { Injectable, inject } from '@angular/core';
import { Category, KardexEntry, KardexType, Product, StockItem, Warehouse } from '../models/api.models';
import { MockDb } from '../mock/mock-db';
import { norm } from '../util/format';

/**
 * Módulo Inventario: ProductsController + InventoryController.
 * ⚠ MOCK: lee de MockDb mientras el backend no existe. Cada método indica el endpoint real
 *   que debe llamar con HttpClient (ver `core/util/params.ts` para armar los query params).
 */
@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private db = inject(MockDb);

  /** GET /api/products?search=&categoryId= */
  products(search?: string, categoryId?: number | null) {
    const q = norm(search);
    return this.db.respond<Product[]>(
      this.db.products.filter((p) => (!q || norm(p.productName).includes(q)) && (!categoryId || p.categoryId === categoryId)),
    );
  }

  /** GET /api/categories */
  categories() {
    return this.db.respond<Category[]>(this.db.categories);
  }

  /** GET /api/inventory/stock  (vw_stock_status) */
  stock(f: { warehouseId?: number | null; productId?: number | null; lowStockOnly?: boolean } = {}) {
    return this.db.respond<StockItem[]>(
      this.db.stockItems().filter((s) =>
        (!f.warehouseId || s.warehouseId === f.warehouseId) && (!f.productId || s.productId === f.productId) && (!f.lowStockOnly || s.needsRestock)),
    );
  }

  /** GET /api/inventory/kardex  (InventoryTransactions) */
  kardex(f: { productId?: number | null; warehouseId?: number | null; type?: KardexType | null; take?: number } = {}) {
    const rows = this.db.kardex.filter((k) =>
      (!f.productId || k.productId === f.productId) && (!f.warehouseId || k.warehouseId === f.warehouseId) && (!f.type || k.transactionType === f.type));
    return this.db.respond<KardexEntry[]>(rows.slice(-(f.take ?? 1000)));
  }

  /** GET /api/inventory/warehouses */
  warehouses() {
    return this.db.respond<Warehouse[]>(this.db.warehouses);
  }
}
