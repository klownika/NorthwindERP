import { Injectable, inject, signal } from '@angular/core';
import { InventoryApi } from '../core/api/inventory.api';

/** Contador de productos por reponer para la alerta del header. */
@Injectable({ providedIn: 'root' })
export class LowStockService {
  private api = inject(InventoryApi);
  readonly count = signal(0);

  refresh(): void {
    this.api.stock({ lowStockOnly: true }).subscribe({ next: (r) => this.count.set(r.length), error: () => this.count.set(0) });
  }
}
