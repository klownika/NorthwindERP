import { Injectable, computed, inject, signal } from '@angular/core';
import { InventoryApi } from '../core/api/inventory.api';
import { StockItem } from '../core/models/api.models';

/** Productos por reponer (vw_stock_status con lowStockOnly): alimentan el contador y el panel de notificaciones del header. */
@Injectable({ providedIn: 'root' })
export class LowStockService {
  private api = inject(InventoryApi);
  readonly items = signal<StockItem[]>([]);
  readonly count = computed(() => this.items().length);

  refresh(): void {
    this.api.stock({ lowStockOnly: true }).subscribe({
      next: (r) => this.items.set([...r].sort((a, b) => a.productName.localeCompare(b.productName) || a.warehouseName.localeCompare(b.warehouseName))),
      error: () => this.items.set([]),
    });
  }
}
