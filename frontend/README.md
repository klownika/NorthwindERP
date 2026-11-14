# StockIN · Frontend (Angular)

Interfaz web de **NorthwindERP / StockIN**, hecha en **Angular 22** (componentes standalone, signals y control flow `@if`/`@for`).
Consume la API de `../backend` (ASP.NET Core) y respeta el diseño DORfic del prototipo: sidebar naranja comprimible, franja por módulo, tablas paginadas y reportes con gráficos.

> ## ⚠ Estado actual: datos simulados (mock)
> El backend solo tiene listos **Auth, Ventas (pedidos) y Customers**; el resto de módulos aún no existe y la conexión la hará otra persona del equipo.
> Por eso **ningún servicio llama todavía a `HttpClient`**: todos leen y escriben en memoria desde `src/app/core/mock/mock-db.ts`
> (los cambios se pierden al recargar la página).
>
> **Usuarios de prueba:** `admin` / `Admin123!` (rol Admin, puede eliminar pedidos) y `vendedor` / `Vendedor123!`.
>
> **Cómo conectar el backend real:** en cada método de `core/api/*.api.ts` y de `core/auth/auth.service.ts`, el comentario
> `/** GET /api/... */` indica el endpoint. Reemplaza el cuerpo por la llamada con `HttpClient` (el interceptor del token y
> `core/util/params.ts` ya están listos), conserva la firma del método (las pantallas no cambian) y, al terminar,
> borra `core/mock/`. En desarrollo `proxy.conf.json` redirige `/api` a `http://localhost:5080`.

## Requisitos

| Herramienta | Versión |
|---|---|
| Node.js | 22 LTS o superior |
| npm | 10+ |
| Backend (cuando se conecte) | `NorthwindERP.API` corriendo en `http://localhost:5080` |

## Ejecutar en local

```bash
cd frontend
npm install          # solo la primera vez
npm start            # = ng serve → http://localhost:4200
```

```bash
npm run build        # compilación de producción en dist/frontend
```

## Estructura (organizada por módulos)

```
src/app/
├── core/                      # lo que usa toda la app (una sola instancia)
│   ├── models/api.models.ts   # interfaces = DTOs del backend
│   ├── api/                   # un servicio por grupo de endpoints
│   │   ├── inventory.api.ts   #   /api/products, /api/categories, /api/inventory/*
│   │   ├── sales.api.ts       #   /api/orders/*, /api/customers/*
│   │   ├── staff.api.ts       #   /api/employees, /api/shippers, /api/suppliers
│   │   └── reports.api.ts     #   /api/reports/*
│   ├── mock/mock-db.ts        # datos simulados en memoria (se borra al conectar el backend)
│   ├── auth/                  # login, JWT, interceptor y guards
│   └── util/                  # formatos ($, fechas), errores ProblemDetails, HttpParams
├── layout/                    # estructura fija
│   ├── shell/                 #   sidebar + header + franja DORfic + pestañas
│   ├── nav.ts                 #   módulos y submódulos del sidebar
│   ├── page.service.ts        #   título de detalle ("Pedido #10443")
│   └── low-stock.service.ts   #   contador de la alerta del header
├── shared/                    # piezas reutilizables
│   ├── data/loader.ts         #   estado "cargando / error" de cada pantalla
│   └── ui/                    #   pager, donut, column-chart, kpi-grid, icon, load-state
└── features/                  # un folder por módulo del sidebar (carga diferida)
    ├── auth/login/
    ├── inicio/
    ├── inventario/            # catálogo, stock, kardex, almacenes, proveedores
    ├── ventas/                # pedidos, pedido-detalle, nuevo-pedido, clientes, cliente-ficha
    ├── personal/              # empleados, transportistas, mi-cuenta
    └── reportes/              # por-cliente, por-pais, anio-clientes, anio-empleados
```

Cada módulo tiene su archivo `*.routes.ts` y se carga solo cuando entras a él (`loadChildren`).

## Pantallas y endpoints

| Pantalla | Ruta | Endpoint · objeto de la BD |
|---|---|---|
| Login | `/login` | `POST /api/auth/login` |
| Inicio | `/` | `GET /api/orders`, `/api/reports/sales-by-customer`, `/api/inventory/stock?lowStockOnly=true` |
| Catálogo | `/inventario/catalogo` | `GET /api/products`, `/api/categories`, `/api/inventory/stock` |
| Stock | `/inventario/stock` | `GET /api/inventory/stock` · `vw_stock_status` |
| Kardex | `/inventario/kardex` | `GET /api/inventory/kardex` · `InventoryTransactions` |
| Almacenes | `/inventario/almacenes` | `GET /api/inventory/warehouses` |
| Proveedores | `/inventario/proveedores` | `GET /api/suppliers` |
| Pedidos | `/ventas/pedidos` | `GET /api/orders` · `vw_order_details_extended` |
| Detalle de pedido | `/ventas/pedidos/:id` | `GET /api/orders/{id}` · `DELETE` → `sp_delete_order` (solo Admin) |
| Nuevo pedido | `/ventas/nuevo-pedido` | `POST /api/orders/with-customer` → `sp_register_order` |
| Clientes | `/ventas/clientes` | `GET /api/customers` |
| Ficha de cliente | `/ventas/clientes/:id` | `PUT /api/customers/{id}` · `GET /api/orders/customer/{id}` → `get_customer_order_history()` |
| Empleados | `/personal/empleados` | `GET /api/employees` |
| Transportistas | `/personal/transportistas` | `GET /api/shippers` |
| Mi cuenta (avatar) | `/cuenta` | `GET /api/auth/me` · `POST /api/auth/change-password` |
| Ventas por cliente | `/reportes/por-cliente` | `GET /api/reports/sales-by-customer` · `vw_sales_by_customer` |
| Ventas por país | `/reportes/por-pais` | `GET /api/reports/sales-by-country` · `vw_sales_by_country` |
| Por año · clientes | `/reportes/anio-clientes` | `GET /api/reports/sales-by-customer-year` |
| Por año · empleados | `/reportes/anio-empleados` | `GET /api/reports/sales-by-employee-year` |

## Notas

- El token se guarda en `sessionStorage`; si la API responde **401**, el interceptor cierra la sesión.
- Los errores de la API (`ProblemDetails`) se muestran tal cual: "409 CONFLICT · Insufficient stock for product …".
- La paginación es en el cliente: 8 productos por página en el catálogo y 10 filas en las tablas.
- Si cambias un DTO en el backend, actualiza su interfaz en `core/models/api.models.ts`.
