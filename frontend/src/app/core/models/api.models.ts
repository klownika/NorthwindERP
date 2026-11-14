/**
 * Modelos de la API de NorthwindERP (C# · ASP.NET Core).
 * Cada interfaz copia un DTO del backend (carpeta NorthwindERP.API/DTOs).
 * ASP.NET serializa en camelCase, por eso las propiedades van en minúscula.
 */

// ----- Auth -----
export interface LoginRequest { username: string; password: string; }
export interface LoginResponse {
  token: string; expiresAtUtc: string; userId: number; username: string;
  role: string | null; employeeId: number; mustChangePassword: boolean;
}
export interface CurrentUser { userId: number; username: string | null; role: string | null; employeeId: number | null; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }

// ----- Inventario -----
export interface Product {
  productId: number; productName: string; categoryId: number | null; categoryName: string | null;
  supplierId: number | null; supplierName: string | null; unit: string | null; price: number;
}
export interface Category { categoryId: number; categoryName: string; description: string | null; }
export interface StockItem {
  warehouseId: number; warehouseName: string; productId: number; productName: string;
  quantity: number; minStockLevel: number; needsRestock: boolean; kardexOk: boolean;
}
export type KardexType = 'INBOUND' | 'OUTBOUND';
export interface KardexEntry {
  transactionId: number; transactionDate: string; warehouseId: number; warehouseName: string;
  productId: number; productName: string; transactionType: KardexType; quantity: number;
  employeeId: number; employeeName: string; orderId: number | null; notes: string | null;
}
export interface Warehouse { warehouseId: number; warehouseName: string; location: string; managerEmployeeId: number; managerName: string; }

// ----- Logística / personal -----
export interface Supplier {
  supplierId: number; supplierName: string; contactName: string | null; address: string | null;
  city: string | null; country: string | null; phone: string | null;
}
export interface Employee { employeeId: number; firstName: string; lastName: string; fullName: string; birthDate: string | null; notes: string | null; }
export interface Shipper { shipperId: number; shipperName: string; phone: string | null; }

// ----- Ventas -----
export interface Customer {
  customerId: number; customerName: string; contactName: string | null; address: string | null;
  city: string | null; postalCode: string | null; country: string | null;
}
export interface UpdateCustomerContact { address?: string | null; city?: string | null; postalCode?: string | null; country?: string | null; }
export interface NewCustomer {
  customerName: string; contactName?: string | null; address?: string | null;
  city?: string | null; postalCode?: string | null; country?: string | null;
}
export interface OrderItemRequest { productId: number; quantity: number; }
export interface CreateOrderRequest {
  customerId: number | null; newCustomer: NewCustomer | null;
  warehouseId: number; shipperId: number | null; items: OrderItemRequest[];
}
export interface CreateOrderResponse { orderId: number; customerId: number; orderTotal: number; }
export interface OrderSummary {
  orderId: number; orderDate: string; customerId: number; customerName: string;
  employeeName: string; shipperName: string | null; lineCount: number; orderTotal: number;
}
export interface OrderLine { productId: number; productName: string; categoryName: string | null; quantity: number; unitPrice: number; lineTotal: number; }
export interface OrderDetail { order: OrderSummary; lines: OrderLine[]; }
export interface CustomerOrderHistoryLine {
  orderId: number; orderDate: string; shipperName: string | null; employeeName: string;
  productName: string; quantity: number; unitPrice: number; lineTotal: number;
}

// ----- Reportes -----
export interface CustomerSales { customerId: number; customerName: string; country: string | null; totalOrders: number; totalUnits: number; totalSales: number; }
export interface CountrySales { country: string | null; totalCustomers: number; totalOrders: number; totalSales: number; }
export interface CustomerYearSales { customerId: number; customerName: string; salesYear: number; totalOrders: number; totalSales: number; avgLineTotal: number; }
export interface EmployeeYearSales { employeeId: number; employeeName: string; salesYear: number; totalOrders: number; totalSales: number; avgOrderValue: number; }

// ----- Errores (ProblemDetails de ASP.NET) -----
export interface ProblemDetails { title?: string; detail?: string; status?: number; errors?: Record<string, string[]>; }
