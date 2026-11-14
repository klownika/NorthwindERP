import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import {
  Category, Customer, Employee, KardexEntry, Product, Shipper, StockItem, Supplier, Warehouse,
} from '../models/api.models';

/**
 * ============================================================================
 *  DATOS SIMULADOS (mock) — SOLO MIENTRAS EL BACKEND NO ESTÉ LISTO
 * ----------------------------------------------------------------------------
 *  Los servicios de `core/api/*` leen y escriben aquí en memoria. Cuando el
 *  backend de cada módulo exista, se reemplaza el cuerpo de cada método por la
 *  llamada `HttpClient` indicada en su comentario (`GET /api/...`) y este
 *  archivo (junto con la carpeta `core/mock`) se puede borrar.
 * ============================================================================
 */

export interface RawOrder {
  orderId: number; orderDate: string; customerId: number; employeeId: number; shipperId: number | null;
  warehouseId: number | null; moved: boolean; lines: { productId: number; quantity: number }[];
}

/** Usuarios de prueba para el login simulado. */
export const MOCK_USERS = [
  { userId: 1, username: 'admin', password: 'Admin123!', role: 'Admin', employeeId: 1, mustChangePassword: false },
  { userId: 2, username: 'vendedor', password: 'Vendedor123!', role: 'Vendedor', employeeId: 3, mustChangePassword: false },
];

const SESSION_KEY = 'stockin.session';

@Injectable({ providedIn: 'root' })
export class MockDb {
  readonly categories: Category[] = [
    { categoryId: 1, categoryName: 'Bebidas', description: 'Refrescos, café, té, cervezas' },
    { categoryId: 2, categoryName: 'Condimentos', description: 'Salsas, aderezos y especias' },
    { categoryId: 3, categoryName: 'Repostería', description: 'Postres y dulces' },
    { categoryId: 4, categoryName: 'Lácteos', description: 'Quesos' },
    { categoryId: 5, categoryName: 'Cereales', description: 'Panes y cereales' },
    { categoryId: 6, categoryName: 'Carnes', description: 'Carnes preparadas' },
    { categoryId: 7, categoryName: 'Frutas y verduras', description: 'Fruta seca y productos del campo' },
    { categoryId: 8, categoryName: 'Mariscos', description: 'Algas y pescado' },
  ];

  readonly suppliers: Supplier[] = [
    { supplierId: 1, supplierName: 'Exotic Liquids', contactName: 'Charlotte Cooper', address: '49 Gilbert St.', city: 'London', country: 'UK', phone: '(171) 555-2222' },
    { supplierId: 2, supplierName: 'New Orleans Cajun Delights', contactName: 'Shelley Burke', address: 'P.O. Box 78934', city: 'New Orleans', country: 'USA', phone: '(100) 555-4822' },
    { supplierId: 3, supplierName: "Grandma Kelly's Homestead", contactName: 'Regina Murphy', address: '707 Oxford Rd.', city: 'Ann Arbor', country: 'USA', phone: '(313) 555-5735' },
    { supplierId: 4, supplierName: 'Tokyo Traders', contactName: 'Yoshi Nagase', address: '9-8 Sekimai', city: 'Tokyo', country: 'Japan', phone: '(03) 3555-5011' },
    { supplierId: 5, supplierName: "Cooperativa de Quesos 'Las Cabras'", contactName: 'Antonio del Valle', address: 'Calle del Rosal 4', city: 'Oviedo', country: 'Spain', phone: '(98) 598 76 54' },
  ];

  readonly employees: Employee[] = [
    this.emp(1, 'Nancy', 'Davolio', '1948-12-08', 'Se unió a la compañía como representante de ventas. Responsable de la cuenta de clientes de la costa oeste.'),
    this.emp(2, 'Andrew', 'Fuller', '1952-02-19', 'Vicepresidente de ventas. Coordina al equipo comercial y supervisa los almacenes del norte.'),
    this.emp(3, 'Janet', 'Leverling', '1963-08-30', 'Representante de ventas con experiencia en cuentas corporativas.'),
    this.emp(4, 'Margaret', 'Peacock', '1937-09-19', 'Representante de ventas. Atiende clientes de Europa.'),
    this.emp(5, 'Steven', 'Buchanan', '1955-03-04', 'Gerente de ventas. Responsable del almacén norte.'),
  ];

  readonly shippers: Shipper[] = [
    { shipperId: 1, shipperName: 'Speedy Express', phone: '(503) 555-9831' },
    { shipperId: 2, shipperName: 'United Package', phone: '(503) 555-3199' },
    { shipperId: 3, shipperName: 'Federal Shipping', phone: '(503) 555-9931' },
  ];

  readonly warehouses: Warehouse[] = [
    { warehouseId: 1, warehouseName: 'Almacén Central', location: 'Seattle, WA', managerEmployeeId: 2, managerName: 'Andrew Fuller' },
    { warehouseId: 2, warehouseName: 'Almacén Norte', location: 'Londres, UK', managerEmployeeId: 5, managerName: 'Steven Buchanan' },
  ];

  readonly customers: Customer[] = [
    this.cus(1, 'Alfreds Futterkiste', 'Maria Anders', 'Obere Str. 57', 'Berlin', '12209', 'Germany'),
    this.cus(2, 'Ana Trujillo Emparedados', 'Ana Trujillo', 'Avda. de la Constitución 2222', 'México D.F.', '05021', 'Mexico'),
    this.cus(3, 'Antonio Moreno Taquería', 'Antonio Moreno', 'Mataderos 2312', 'México D.F.', '05023', 'Mexico'),
    this.cus(4, 'Around the Horn', 'Thomas Hardy', '120 Hanover Sq.', 'London', 'WA1 1DP', 'UK'),
    this.cus(5, 'Berglunds snabbköp', 'Christina Berglund', 'Berguvsvägen 8', 'Luleå', 'S-958 22', 'Sweden'),
    this.cus(6, 'Blondel père et fils', 'Frédérique Citeaux', '24, place Kléber', 'Strasbourg', '67000', 'France'),
    this.cus(7, 'Bólido Comidas preparadas', 'Martín Sommer', 'C/ Araquil, 67', 'Madrid', '28023', 'Spain'),
    this.cus(8, "Bon app'", 'Laurence Lebihan', '12, rue des Bouchers', 'Marseille', '13008', 'France'),
    this.cus(9, 'Ernst Handel', 'Roland Mendel', 'Kirchgasse 6', 'Graz', '8010', 'Austria'),
    this.cus(10, 'QUICK-Stop', 'Horst Kloss', 'Taucherstraße 10', 'Cunewalde', '01307', 'Germany'),
  ];

  readonly products: Product[] = [
    this.prod(1, 'Chai', 1, 1, '10 cajas x 20 bolsas', 18),
    this.prod(2, 'Chang', 1, 1, '24 botellas de 12 oz', 19),
    this.prod(3, 'Aniseed Syrup', 2, 1, '12 botellas de 550 ml', 10),
    this.prod(4, "Chef Anton's Cajun Seasoning", 2, 2, '48 frascos de 6 oz', 22),
    this.prod(5, "Grandma's Boysenberry Spread", 2, 3, '12 frascos de 8 oz', 25),
    this.prod(6, "Uncle Bob's Organic Dried Pears", 7, 3, '12 paquetes de 1 lb', 30),
    this.prod(7, 'Northwoods Cranberry Sauce', 2, 3, '12 frascos de 12 oz', 40),
    this.prod(8, 'Mishi Kobe Niku', 6, 4, '18 paquetes de 500 g', 97),
    this.prod(9, 'Ikura', 8, 4, '12 frascos de 200 ml', 31),
    this.prod(10, 'Queso Cabrales', 4, 5, 'Paquete de 1 kg', 21),
    this.prod(11, 'Queso Manchego La Pastora', 4, 5, '10 paquetes de 500 g', 38),
    this.prod(12, 'Konbu', 8, 4, 'Caja de 2 kg', 6),
    this.prod(13, 'Tofu', 7, 4, '40 paquetes de 100 g', 23),
    this.prod(14, 'Genen Shouyu', 2, 1, '24 botellas de 250 ml', 15),
    this.prod(15, 'Pavlova', 3, 2, '32 cajas de 500 g', 17),
    this.prod(16, "Sir Rodney's Scones", 3, 2, '24 paquetes x 4 piezas', 10),
  ];

  /** Stock por almacén y producto (cantidad determinista para que la demo sea estable). */
  readonly stock: { warehouseId: number; productId: number; quantity: number; minStockLevel: number }[] = [];
  readonly kardex: KardexEntry[] = [];
  readonly orders: RawOrder[] = [];
  private nextTransaction = 1;

  constructor() {
    for (const w of this.warehouses) {
      for (const p of this.products) {
        const quantity = ((p.productId * 37 + w.warehouseId * 53) % 90) + 5;
        this.stock.push({ warehouseId: w.warehouseId, productId: p.productId, quantity, minStockLevel: 15 });
        this.addKardex(w.warehouseId, p.productId, 'INBOUND', quantity, w.managerEmployeeId, null, 'Initial stock', '1996-07-01T08:00:00');
      }
    }
    // Pedidos históricos (anteriores al ERP): no mueven inventario ni tienen filas en el Kardex.
    for (let i = 0; i < 24; i++) {
      const year = 1996 + Math.floor(i / 8);
      const month = String(((i * 5) % 12) + 1).padStart(2, '0');
      const day = String(((i * 3) % 27) + 1).padStart(2, '0');
      const lines = [0, 1, 2].slice(0, (i % 3) + 1).map((k) => ({
        productId: ((i * 5 + k * 3) % 16) + 1, quantity: ((i + k) % 9 + 1) * 2,
      }));
      this.orders.push({
        orderId: 10248 + i, orderDate: `${year}-${month}-${day}T00:00:00`, customerId: ((i * 3) % 10) + 1,
        employeeId: (i % 5) + 1, shipperId: i % 7 === 0 ? null : (i % 3) + 1, warehouseId: null, moved: false, lines,
      });
    }
  }

  // ---------- helpers de respuesta ----------
  /** Respuesta simulada con una pequeña latencia, igual que una llamada HTTP. */
  respond<T>(value: T): Observable<T> {
    return of(structuredClone(value)).pipe(delay(180));
  }

  /** Error simulado con la misma forma que un ProblemDetails de ASP.NET. */
  fail(status: number, statusText: string, detail: string): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status, statusText, error: { title: statusText, detail, status } }));
  }

  /** Datos de la sesión actual (el login simulado los guarda en sessionStorage). */
  session(): { employeeId: number; role: string | null } {
    try {
      const raw = sessionStorage.getItem(SESSION_KEY);
      const s = raw ? JSON.parse(raw) : null;
      return { employeeId: s?.employeeId ?? 1, role: s?.role ?? null };
    } catch {
      return { employeeId: 1, role: null };
    }
  }

  // ---------- derivados ----------
  employeeName(id: number): string { return this.employees.find((e) => e.employeeId === id)?.fullName ?? '—'; }
  productById(id: number): Product | undefined { return this.products.find((p) => p.productId === id); }
  customerById(id: number): Customer | undefined { return this.customers.find((c) => c.customerId === id); }

  stockItems(): StockItem[] {
    return this.stock.map((s) => ({
      warehouseId: s.warehouseId,
      warehouseName: this.warehouses.find((w) => w.warehouseId === s.warehouseId)?.warehouseName ?? '',
      productId: s.productId,
      productName: this.productById(s.productId)?.productName ?? '',
      quantity: s.quantity, minStockLevel: s.minStockLevel, needsRestock: s.quantity < s.minStockLevel,
      kardexOk: this.kardexBalance(s.warehouseId, s.productId) === s.quantity,
    }));
  }

  kardexBalance(warehouseId: number, productId: number): number {
    return this.kardex
      .filter((k) => k.warehouseId === warehouseId && k.productId === productId)
      .reduce((a, k) => a + (k.transactionType === 'INBOUND' ? k.quantity : -k.quantity), 0);
  }

  addKardex(warehouseId: number, productId: number, type: 'INBOUND' | 'OUTBOUND', quantity: number,
            employeeId: number, orderId: number | null, notes: string, date = new Date().toISOString().slice(0, 19)): void {
    this.kardex.push({
      transactionId: this.nextTransaction++, transactionDate: date, warehouseId,
      warehouseName: this.warehouses.find((w) => w.warehouseId === warehouseId)?.warehouseName ?? '',
      productId, productName: this.productById(productId)?.productName ?? '', transactionType: type, quantity,
      employeeId, employeeName: this.employeeName(employeeId), orderId, notes,
    });
  }

  private emp(employeeId: number, firstName: string, lastName: string, birthDate: string, notes: string): Employee {
    return { employeeId, firstName, lastName, fullName: `${firstName} ${lastName}`, birthDate: `${birthDate}T00:00:00`, notes };
  }

  private cus(customerId: number, customerName: string, contactName: string, address: string, city: string,
              postalCode: string, country: string): Customer {
    return { customerId, customerName, contactName, address, city, postalCode, country };
  }

  private prod(productId: number, productName: string, categoryId: number, supplierId: number, unit: string, price: number): Product {
    return {
      productId, productName, categoryId, categoryName: this.categories.find((c) => c.categoryId === categoryId)?.categoryName ?? null,
      supplierId, supplierName: this.suppliers.find((s) => s.supplierId === supplierId)?.supplierName ?? null, unit, price,
    };
  }
}
