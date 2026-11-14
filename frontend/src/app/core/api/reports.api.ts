import { Injectable, inject } from '@angular/core';
import { MockDb } from '../mock/mock-db';
import { CountrySales, CustomerSales, CustomerYearSales, EmployeeYearSales } from '../models/api.models';

/**
 * Módulo Reportes: ReportsController (vistas + consultas con JOIN/GROUP BY).
 * ⚠ MOCK: agrega los pedidos de MockDb en memoria mientras el backend no existe.
 */
@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private db = inject(MockDb);

  /** Líneas de todos los pedidos con su total y año. */
  private flat() {
    return this.db.orders.flatMap((o) =>
      o.lines.map((l) => {
        const price = this.db.productById(l.productId)?.price ?? 0;
        return { o, qty: l.quantity, total: price * l.quantity, year: +o.orderDate.slice(0, 4) };
      }),
    );
  }

  /** GET /api/reports/sales-by-customer  (vw_sales_by_customer) */
  salesByCustomer() {
    const map = new Map<number, CustomerSales>();
    for (const x of this.flat()) {
      const c = this.db.customerById(x.o.customerId)!;
      const r = map.get(c.customerId) ?? { customerId: c.customerId, customerName: c.customerName, country: c.country, totalOrders: 0, totalUnits: 0, totalSales: 0 };
      r.totalUnits += x.qty; r.totalSales += x.total;
      map.set(c.customerId, r);
    }
    for (const r of map.values()) r.totalOrders = this.db.orders.filter((o) => o.customerId === r.customerId).length;
    return this.db.respond<CustomerSales[]>([...map.values()]);
  }

  /** GET /api/reports/sales-by-country  (vw_sales_by_country) */
  salesByCountry() {
    const map = new Map<string, CountrySales>();
    for (const x of this.flat()) {
      const country = this.db.customerById(x.o.customerId)?.country ?? null;
      const r = map.get(country ?? '') ?? { country, totalCustomers: 0, totalOrders: 0, totalSales: 0 };
      r.totalSales += x.total;
      map.set(country ?? '', r);
    }
    for (const r of map.values()) {
      const orders = this.db.orders.filter((o) => this.db.customerById(o.customerId)?.country === r.country);
      r.totalOrders = orders.length;
      r.totalCustomers = new Set(orders.map((o) => o.customerId)).size;
    }
    return this.db.respond<CountrySales[]>([...map.values()]);
  }

  /** GET /api/reports/sales-by-customer-year?year= */
  salesByCustomerYear(year?: number | null) {
    const map = new Map<string, CustomerYearSales & { lines: number }>();
    for (const x of this.flat().filter((f) => !year || f.year === year)) {
      const c = this.db.customerById(x.o.customerId)!;
      const key = `${c.customerId}-${x.year}`;
      const r = map.get(key) ?? { customerId: c.customerId, customerName: c.customerName, salesYear: x.year, totalOrders: 0, totalSales: 0, avgLineTotal: 0, lines: 0 };
      r.totalSales += x.total; r.lines++;
      map.set(key, r);
    }
    const rows = [...map.values()].map(({ lines, ...r }) => ({
      ...r,
      totalOrders: this.db.orders.filter((o) => o.customerId === r.customerId && +o.orderDate.slice(0, 4) === r.salesYear).length,
      avgLineTotal: r.totalSales / lines,
    }));
    return this.db.respond<CustomerYearSales[]>(rows);
  }

  /** GET /api/reports/sales-by-employee-year?year= */
  salesByEmployeeYear(year?: number | null) {
    const map = new Map<string, EmployeeYearSales>();
    for (const x of this.flat().filter((f) => !year || f.year === year)) {
      const key = `${x.o.employeeId}-${x.year}`;
      const r = map.get(key) ?? { employeeId: x.o.employeeId, employeeName: this.db.employeeName(x.o.employeeId), salesYear: x.year, totalOrders: 0, totalSales: 0, avgOrderValue: 0 };
      r.totalSales += x.total;
      map.set(key, r);
    }
    for (const r of map.values()) {
      r.totalOrders = this.db.orders.filter((o) => o.employeeId === r.employeeId && +o.orderDate.slice(0, 4) === r.salesYear).length;
      r.avgOrderValue = r.totalSales / r.totalOrders;
    }
    return this.db.respond<EmployeeYearSales[]>([...map.values()]);
  }
}
