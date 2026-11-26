import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StaffApi } from '../../../core/api/staff.api';
import { Product, Supplier } from '../../../core/models/api.models';
import { norm } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { ComboOption, ComboboxComponent } from '../../../shared/ui/combobox';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Proveedores (Suppliers) y cuántos productos surten. Búsqueda por texto + filtros de país y ciudad. */
@Component({
  selector: 'app-proveedores',
  imports: [FormsModule, ComboboxComponent, LoadStateComponent, PagerComponent],
  templateUrl: './proveedores.html',
})
export class ProveedoresComponent {
  private staff = inject(StaffApi);
  private inventory = inject(InventoryApi);
  readonly load = new Loader();
  readonly suppliers = signal<Supplier[]>([]);
  readonly products = signal<Product[]>([]);
  readonly q = signal('');
  readonly country = signal('');
  readonly city = signal('');
  readonly cols = 'minmax(220px, 1fr) 200px 140px 120px 150px 110px';

  /** Opciones sacadas de todos los proveedores (no solo de la página visible). */
  readonly countryOpts = computed<ComboOption[]>(() => distinct(this.suppliers().map((s) => s.country)));
  readonly cityOpts = computed<ComboOption[]>(() =>
    distinct(this.suppliers().filter((s) => s.country === this.country()).map((s) => s.city)));
  readonly hasFilters = computed(() => !!(this.q().trim() || this.country() || this.city()));

  readonly filtered = computed(() => {
    const q = norm(this.q());
    const country = this.country();
    const city = this.city();
    const count = new Map<number, number>();
    this.products().forEach((p) => p.supplierId && count.set(p.supplierId, (count.get(p.supplierId) ?? 0) + 1));
    return this.suppliers()
      .filter((s) => (!q || norm(`${s.supplierName} ${s.city} ${s.country}`).includes(q))
        && (!country || s.country === country) && (!city || s.city === city))
      .map((s) => ({ ...s, products: count.get(s.supplierId) ?? 0 }));
  });
  readonly pager = new Pager(this.filtered, 10);

  constructor() {
    this.load.run(forkJoin({ s: this.staff.suppliers(), p: this.inventory.products() }), (r) => {
      this.suppliers.set(r.s);
      this.products.set(r.p);
    });
  }

  /** Al cambiar el país, la ciudad elegida deja de valer. */
  setCountry(v: string): void {
    this.country.set(v);
    this.city.set('');
  }

  clearFilters(): void {
    this.q.set('');
    this.country.set('');
    this.city.set('');
  }
}

/** Valores únicos, sin vacíos y ordenados, como opciones del combobox. */
function distinct(values: (string | null)[]): ComboOption[] {
  return [...new Set(values.filter((v): v is string => !!v))].sort((a, b) => a.localeCompare(b)).map((v) => ({ value: v, label: v }));
}
