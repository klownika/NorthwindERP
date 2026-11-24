import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Category, KardexEntry, KardexType, Product, StockItem, Warehouse } from '../models/api.models';
import { toParams } from '../util/params';

/** Módulo Inventario: ProductsController + InventoryController. */
@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private http = inject(HttpClient);

  /** GET /api/products?search=&categoryId= */
  products(search?: string, categoryId?: number | null) {
    return this.http.get<Product[]>('/api/products', { params: toParams({ search, categoryId }) });
  }

  /** GET /api/categories */
  categories() {
    return this.http.get<Category[]>('/api/categories');
  }

  /** GET /api/inventory/stock  (vw_stock_status) */
  stock(f: { warehouseId?: number | null; productId?: number | null; lowStockOnly?: boolean } = {}) {
    return this.http.get<StockItem[]>('/api/inventory/stock', { params: toParams(f) });
  }

  /** GET /api/inventory/kardex  (InventoryTransactions). Se pide el máximo porque la API devuelve 500 por defecto. */
  kardex(f: { productId?: number | null; warehouseId?: number | null; type?: KardexType | null; take?: number } = {}) {
    return this.http.get<KardexEntry[]>('/api/inventory/kardex', { params: toParams({ ...f, take: f.take ?? 1000 }) });
  }

  /** GET /api/inventory/warehouses */
  warehouses() {
    return this.http.get<Warehouse[]>('/api/inventory/warehouses');
  }
}
