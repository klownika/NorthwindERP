import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  CreateOrderRequest, CreateOrderResponse, Customer, CustomerOrderHistoryLine,
  OrderDetail, OrderSummary, UpdateCustomerContact,
} from '../models/api.models';
import { toParams } from '../util/params';

/** Módulo Ventas: OrdersController + CustomersController. */
@Injectable({ providedIn: 'root' })
export class SalesApi {
  private http = inject(HttpClient);

  /** GET /api/orders  (vw_order_details_extended agrupada). Se pide el máximo porque la API devuelve 200 por defecto. */
  orders(f: { customerId?: number | null; employeeId?: number | null; from?: string; to?: string; take?: number } = {}) {
    return this.http.get<OrderSummary[]>('/api/orders', { params: toParams({ ...f, take: f.take ?? 1000 }) });
  }

  /** GET /api/orders/{id} */
  order(id: number) {
    return this.http.get<OrderDetail>(`/api/orders/${id}`);
  }

  /** POST /api/orders/with-customer  → INSERT Customers (si es nuevo) + CALL sp_register_order */
  createOrder(body: CreateOrderRequest) {
    return this.http.post<CreateOrderResponse>('/api/orders/with-customer', body);
  }

  /** DELETE /api/orders/{id}  → CALL sp_delete_order (solo Admin) */
  deleteOrder(id: number) {
    return this.http.delete<void>(`/api/orders/${id}`);
  }

  /** GET /api/orders/customer/{id}  → get_customer_order_history() */
  customerHistory(customerId: number) {
    return this.http.get<CustomerOrderHistoryLine[]>(`/api/orders/customer/${customerId}`);
  }

  /** GET /api/customers */
  customers() {
    return this.http.get<Customer[]>('/api/customers');
  }

  /** GET /api/customers/{id} */
  customer(id: number) {
    return this.http.get<Customer>(`/api/customers/${id}`);
  }

  /** PUT /api/customers/{id}  (solo dirección) */
  updateCustomer(id: number, body: UpdateCustomerContact) {
    return this.http.put<Customer>(`/api/customers/${id}`, body);
  }
}
