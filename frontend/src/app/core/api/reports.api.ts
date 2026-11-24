import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CountrySales, CustomerSales, CustomerYearSales, EmployeeYearSales } from '../models/api.models';
import { toParams } from '../util/params';

/** Módulo Reportes: ReportsController (vistas + consultas con JOIN/GROUP BY). */
@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private http = inject(HttpClient);

  /** GET /api/reports/sales-by-customer  (vw_sales_by_customer) */
  salesByCustomer() {
    return this.http.get<CustomerSales[]>('/api/reports/sales-by-customer');
  }

  /** GET /api/reports/sales-by-country  (vw_sales_by_country) */
  salesByCountry() {
    return this.http.get<CountrySales[]>('/api/reports/sales-by-country');
  }

  /** GET /api/reports/sales-by-customer-year?year= */
  salesByCustomerYear(year?: number | null) {
    return this.http.get<CustomerYearSales[]>('/api/reports/sales-by-customer-year', { params: toParams({ year }) });
  }

  /** GET /api/reports/sales-by-employee-year?year= */
  salesByEmployeeYear(year?: number | null) {
    return this.http.get<EmployeeYearSales[]>('/api/reports/sales-by-employee-year', { params: toParams({ year }) });
  }
}
