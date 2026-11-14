import { Component, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { StaffApi } from '../../../core/api/staff.api';
import { AuthService } from '../../../core/auth/auth.service';
import { Employee, Warehouse } from '../../../core/models/api.models';
import { fmtDate } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Empleados en tarjetas, con etiqueta de almacén a cargo y de tu cuenta. */
@Component({
  selector: 'app-empleados',
  imports: [LoadStateComponent, PagerComponent],
  templateUrl: './empleados.html',
  styleUrl: './empleados.scss',
})
export class EmpleadosComponent {
  private staff = inject(StaffApi);
  private inventory = inject(InventoryApi);
  private auth = inject(AuthService);
  readonly load = new Loader();
  readonly employees = signal<Employee[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);

  readonly cards = computed(() => {
    const me = this.auth.session();
    return this.employees().map((e) => {
      const tags = this.warehouses().filter((w) => w.managerEmployeeId === e.employeeId).map((w) => 'Responsable: ' + w.warehouseName);
      if (me?.employeeId === e.employeeId) tags.push(`Cuenta: ${me.username} (${me.role})`);
      const notes = e.notes ?? '';
      return {
        ...e, tags, birth: fmtDate(e.birthDate), initials: (e.firstName[0] ?? '') + (e.lastName[0] ?? ''),
        notesShort: notes.length > 170 ? notes.slice(0, 167) + '…' : notes,
      };
    });
  });
  readonly pager = new Pager(this.cards, 6);

  constructor() {
    this.load.run(forkJoin({ e: this.staff.employees(), w: this.inventory.warehouses() }), (r) => {
      this.employees.set(r.e);
      this.warehouses.set(r.w);
    });
  }
}
