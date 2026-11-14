import { Injectable, inject } from '@angular/core';
import { MockDb, RawOrder } from '../mock/mock-db';
import {
  CreateOrderRequest, CreateOrderResponse, Customer, CustomerOrderHistoryLine,
  OrderDetail, OrderLine, OrderSummary, UpdateCustomerContact,
} from '../models/api.models';

/**
 * Módulo Ventas: OrdersController + CustomersController.
 * ⚠ MOCK: el backend de pedidos y clientes ya existe, pero la conexión la hará otra persona del equipo.
 *   Mientras tanto todo se resuelve en memoria con MockDb (los cambios se pierden al recargar la página).
 */
@Injectable({ providedIn: 'root' })
export class SalesApi {
  private db = inject(MockDb);

  private lines(o: RawOrder): OrderLine[] {
    return o.lines.map((l) => {
      const p = this.db.productById(l.productId)!;
      return { productId: p.productId, productName: p.productName, categoryName: p.categoryName, quantity: l.quantity, unitPrice: p.price, lineTotal: p.price * l.quantity };
    });
  }

  private summary(o: RawOrder): OrderSummary {
    return {
      orderId: o.orderId, orderDate: o.orderDate, customerId: o.customerId,
      customerName: this.db.customerById(o.customerId)?.customerName ?? '—', employeeName: this.db.employeeName(o.employeeId),
      shipperName: this.db.shippers.find((s) => s.shipperId === o.shipperId)?.shipperName ?? null,
      lineCount: o.lines.length, orderTotal: this.lines(o).reduce((a, l) => a + l.lineTotal, 0),
    };
  }

  /** GET /api/orders  (vw_order_details_extended agrupada) */
  orders(f: { customerId?: number | null; employeeId?: number | null; from?: string; to?: string; take?: number } = {}) {
    const rows = this.db.orders.filter((o) =>
      (!f.customerId || o.customerId === f.customerId) && (!f.employeeId || o.employeeId === f.employeeId) &&
      (!f.from || o.orderDate >= f.from) && (!f.to || o.orderDate <= f.to));
    return this.db.respond<OrderSummary[]>(rows.slice(-(f.take ?? 1000)).map((o) => this.summary(o)));
  }

  /** GET /api/orders/{id} */
  order(id: number) {
    const o = this.db.orders.find((x) => x.orderId === id);
    if (!o) return this.db.fail(404, 'Not Found', `Order ${id} not found.`);
    return this.db.respond<OrderDetail>({ order: this.summary(o), lines: this.lines(o) });
  }

  /** POST /api/orders/with-customer  → INSERT Customers (si es nuevo) + CALL sp_register_order */
  createOrder(body: CreateOrderRequest) {
    if (!body.items.length) return this.db.fail(400, 'Bad Request', 'The order needs at least one item.');
    // Todo o nada: se valida el stock de todas las líneas antes de tocar nada.
    for (const it of body.items) {
      const row = this.db.stock.find((s) => s.warehouseId === body.warehouseId && s.productId === it.productId);
      if (!row || row.quantity < it.quantity) {
        const name = this.db.productById(it.productId)?.productName ?? it.productId;
        return this.db.fail(409, 'Conflict', `Insufficient stock for product ${name}.`);
      }
    }
    let customerId = body.customerId;
    if (!customerId && body.newCustomer) {
      customerId = Math.max(...this.db.customers.map((c) => c.customerId)) + 1;
      const n = body.newCustomer;
      this.db.customers.push({
        customerId, customerName: n.customerName, contactName: n.contactName ?? null, address: n.address ?? null,
        city: n.city ?? null, postalCode: n.postalCode ?? null, country: n.country ?? null,
      });
    }
    if (!customerId || !this.db.customerById(customerId)) return this.db.fail(404, 'Not Found', 'Customer not found.');

    const employeeId = this.db.session().employeeId;
    const orderId = Math.max(...this.db.orders.map((o) => o.orderId)) + 1;
    this.db.orders.push({
      orderId, orderDate: new Date().toISOString().slice(0, 19), customerId, employeeId, shipperId: body.shipperId,
      warehouseId: body.warehouseId, moved: true, lines: body.items.map((i) => ({ ...i })),
    });
    for (const it of body.items) {
      this.db.stock.find((s) => s.warehouseId === body.warehouseId && s.productId === it.productId)!.quantity -= it.quantity;
      this.db.addKardex(body.warehouseId, it.productId, 'OUTBOUND', it.quantity, employeeId, orderId, `Sale - order ${orderId}`);
    }
    const total = this.summary(this.db.orders[this.db.orders.length - 1]).orderTotal;
    return this.db.respond<CreateOrderResponse>({ orderId, customerId, orderTotal: total });
  }

  /** DELETE /api/orders/{id}  → CALL sp_delete_order (solo Admin) */
  deleteOrder(id: number) {
    if (this.db.session().role !== 'Admin') return this.db.fail(403, 'Forbidden', 'Only an Admin can delete orders.');
    const idx = this.db.orders.findIndex((o) => o.orderId === id);
    if (idx < 0) return this.db.fail(404, 'Not Found', `Order ${id} not found.`);
    const o = this.db.orders[idx];
    if (o.moved && o.warehouseId) {
      for (const l of o.lines) {
        this.db.stock.find((s) => s.warehouseId === o.warehouseId && s.productId === l.productId)!.quantity += l.quantity;
        this.db.addKardex(o.warehouseId, l.productId, 'INBOUND', l.quantity, this.db.session().employeeId, id, `Return - order ${id} deleted`);
      }
    }
    this.db.orders.splice(idx, 1);
    return this.db.respond<void>(undefined);
  }

  /** GET /api/orders/customer/{id}  → get_customer_order_history() */
  customerHistory(customerId: number) {
    const rows: CustomerOrderHistoryLine[] = this.db.orders.filter((o) => o.customerId === customerId).flatMap((o) =>
      this.lines(o).map((l) => ({
        orderId: o.orderId, orderDate: o.orderDate, employeeName: this.db.employeeName(o.employeeId),
        shipperName: this.db.shippers.find((s) => s.shipperId === o.shipperId)?.shipperName ?? null,
        productName: l.productName, quantity: l.quantity, unitPrice: l.unitPrice, lineTotal: l.lineTotal,
      })));
    return this.db.respond<CustomerOrderHistoryLine[]>(rows);
  }

  /** GET /api/customers */
  customers() {
    return this.db.respond<Customer[]>(this.db.customers);
  }

  /** GET /api/customers/{id} */
  customer(id: number) {
    const c = this.db.customerById(id);
    return c ? this.db.respond<Customer>(c) : this.db.fail(404, 'Not Found', `Customer ${id} not found.`);
  }

  /** PUT /api/customers/{id}  (solo dirección) */
  updateCustomer(id: number, body: UpdateCustomerContact) {
    const c = this.db.customerById(id);
    if (!c) return this.db.fail(404, 'Not Found', `Customer ${id} not found.`);
    if (body.address == null && body.city == null && body.postalCode == null && body.country == null) {
      return this.db.fail(400, 'Bad Request', 'Send at least one address field to update.');
    }
    Object.assign(c, {
      address: body.address ?? c.address, city: body.city ?? c.city,
      postalCode: body.postalCode ?? c.postalCode, country: body.country ?? c.country,
    });
    return this.db.respond<Customer>(c);
  }
}
