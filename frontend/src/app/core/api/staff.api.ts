import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Employee, Shipper, Supplier } from '../models/api.models';

/** Personal y logística: EmployeesController, ShippersController y SuppliersController. */
@Injectable({ providedIn: 'root' })
export class StaffApi {
  private http = inject(HttpClient);

  /** GET /api/employees */
  employees() { return this.http.get<Employee[]>('/api/employees'); }

  /** GET /api/shippers */
  shippers() { return this.http.get<Shipper[]>('/api/shippers'); }

  /** GET /api/suppliers */
  suppliers() { return this.http.get<Supplier[]>('/api/suppliers'); }
}
