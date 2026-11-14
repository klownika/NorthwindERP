import { Injectable, inject } from '@angular/core';
import { MockDb } from '../mock/mock-db';
import { Employee, Shipper, Supplier } from '../models/api.models';

/**
 * Personal y logística: LogisticsControllers.cs (empleados, transportistas, proveedores).
 * ⚠ MOCK: lee de MockDb mientras el backend no existe.
 */
@Injectable({ providedIn: 'root' })
export class StaffApi {
  private db = inject(MockDb);

  /** GET /api/employees */
  employees() { return this.db.respond<Employee[]>(this.db.employees); }

  /** GET /api/shippers */
  shippers() { return this.db.respond<Shipper[]>(this.db.shippers); }

  /** GET /api/suppliers */
  suppliers() { return this.db.respond<Supplier[]>(this.db.suppliers); }
}
