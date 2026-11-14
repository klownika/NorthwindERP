import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StaffApi } from '../../../core/api/staff.api';
import { Product, Supplier } from '../../../core/models/api.models';
import { norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Proveedores (Suppliers) y cuántos productos surten. */
@Component({
  selector: 'app-proveedores',
  imports: [FormsModule, LoadStateComponent, PagerComponent],
  templateUrl: './proveedores.html',
})
export class ProveedoresComponent {
  private staff = inject(StaffApi);
  private inventory = inject(InventoryApi);
  readonly load = new Loader();
  readonly suppliers = signal<Supplier[]>([]);
  readonly products = signal<Product[]>([]);
  readonly q = signal('');
  readonly cols = 'minmax(220px, 1fr) 200px 140px 120px 150px 110px';

  readonly filtered = computed(() => {
    const q = norm(this.q());
    const count = new Map<number, number>();
    this.products().forEach((p) => p.supplierId && count.set(p.supplierId, (count.get(p.supplierId) ?? 0) + 1));
    return this.suppliers()
      .filter((s) => !q || norm(`${s.supplierName} ${s.city} ${s.country}`).includes(q))
      .map((s) => ({ ...s, products: count.get(s.supplierId) ?? 0 }));
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    this.load.run(forkJoin({ s: this.staff.suppliers(), p: this.inventory.products() }), (r) => {
      this.suppliers.set(r.s);
      this.products.set(r.p);
    });
  }
}
