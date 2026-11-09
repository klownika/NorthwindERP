# NorthwindERP — API (v1)

API REST en **ASP.NET Core 8** sobre la base de datos Northwind en Supabase. Este documento explica cómo funcionan los endpoints, cómo se comunican las capas y dónde se cumplen las reglas de negocio y los requisitos del profesor.

> Complementa a `database/README.md`, que explica la base de datos (tablas, vistas, procedimientos y reglas R1–R9).

---

## 1. Arquitectura: quién habla con quién

```
 Frontend / Swagger / NorthwindERP.API.http
        │  HTTP + JWT (Authorization: Bearer ...)
        ▼
┌──────────────────────── NorthwindERP.API ─────────────────────────┐
│ Middlewares   → ErrorHandlingMiddleware: excepción → ProblemDetails│
│ Controllers   → reciben el request, validan el DTO, leen el token  │
│ DTOs          → contrato público (lo que entra y sale en JSON)     │
│ Mappings      → Core → DTO (.ToDto()), un archivo por módulo       │
└───────────────────────────────┬────────────────────────────────────┘
                                │ interfaces (IAuthService, IOrderService, ...)
┌───────────────────────── NorthwindERP.Core ────────────────────────┐
│ Entities   → tablas simples (UserAccount, Customer, Employee...)   │
│ Models     → records de dominio (LoginResult, OrderSummary...)     │
│ Interfaces → contratos. Services → lógica de Cuentas y Clientes    │
│ Sin SQL, sin EF, sin HTTP.                                          │
└───────────────────────────────┬────────────────────────────────────┘
                                │ implementaciones
┌────────────────────── NorthwindERP.Infrastructure ─────────────────┐
│ Data/NorthwindDbContext + Repositories → EF Core (tablas simples)  │
│ Services → Npgsql + SQL (procedimientos y vistas)                  │
│ Security → BCrypt, JWT, AdminSeeder                                │
└───────────────────────────────┬────────────────────────────────────┘
                                ▼
                  PostgreSQL (Supabase): tablas, vistas, procedimientos
```

**Dos formas de acceder a datos, a propósito:**

| | EF Core (`DbContext` + repositorios) | Npgsql + SQL (`Infrastructure/Services`) |
|---|---|---|
| Módulos | Cuentas, Clientes, Logística | Inventario, Órdenes, Reportes |
| Para qué | CRUD simple sobre una tabla | Llamar procedimientos (`CALL`), leer vistas, agregaciones |
| Por qué | Menos código: EF genera el SQL | EF no llama `PROCEDURE` con `INOUT` de forma natural; el SQL queda explícito y auditable |

**La idea central:** las reglas que protegen los datos (stock, Kardex, cascada) **viven en la base de datos**, dentro de los procedimientos. La API no recalcula stock ni escribe en el Kardex: **orquesta**. Valida la forma del request, toma el empleado del token, abre la transacción, llama al procedimiento y traduce los errores.

| Capa | Responsabilidad | Ejemplo |
|---|---|---|
| DTO + `[ApiController]` | **Forma** de los datos | `quantity` debe ser ≥ 1 → 400 antes de tocar la BD |
| Controller | **Identidad y permisos** | `EmployeeId` sale del token; `DELETE` exige rol Admin |
| Service (Infrastructure) | **Transacción y SQL** | Cliente nuevo + `CALL sp_register_order` en una sola transacción |
| Base de datos | **Reglas del negocio** | Stock suficiente, descuento, Kardex, cascada |

---

## 2. Catálogo de endpoints

Todos requieren JWT, excepto `login` y `health`.

### 🔐 Auth — módulo Cuentas

| Método | Ruta | Rol | Qué hace | BD |
|---|---|---|---|---|
| POST | `/api/auth/login` | Anónimo | Valida usuario y contraseña y devuelve un JWT | `UserAccounts` + `Roles` |
| GET | `/api/auth/me` | Cualquiera | Datos del usuario del token | — (lee los claims) |
| POST | `/api/auth/change-password` | Cualquiera | Cambia la contraseña y apaga `MustChangePassword` | `UPDATE UserAccounts` |

### 👥 Customers

| Método | Ruta | Qué hace | Requisito del profesor |
|---|---|---|---|
| GET | `/api/customers` | Lista clientes | — |
| GET | `/api/customers/{id}` | Un cliente | — |
| PUT | `/api/customers/{id}` | Actualiza la dirección | **"Actualizar teléfono o dirección"** (la tabla no tiene teléfono) |

### 🚚 Logística (listas de apoyo para el frontend)

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/employees` | Empleados (vendedores, responsables de almacén) |
| GET | `/api/shippers` | Transportistas (combo del formulario de pedido) |
| GET | `/api/suppliers` | Proveedores |

### 🛒 Orders — módulo Órdenes

| Método | Ruta | Rol | Qué hace | BD | Requisito |
|---|---|---|---|---|---|
| POST | `/api/orders/with-customer` | Cualquiera | Crea un pedido con cliente nuevo o existente | `INSERT Customers` + `CALL sp_register_order` | **Insertar cliente + pedido**, **Procedimiento 1** |
| GET | `/api/orders` | Cualquiera | Pedidos con su total (filtros: cliente, empleado, fechas) | `vw_order_details_extended` agrupada | **Vista 1** |
| GET | `/api/orders/{id}` | Cualquiera | Un pedido con sus productos y el total por línea | `vw_order_details_extended` | **Vista 1** |
| GET | `/api/orders/customer/{customerId}` | Cualquiera | Historial del cliente | `get_customer_order_history()` | **Procedimiento 2** |
| DELETE | `/api/orders/{id}` | **Admin** | Elimina el pedido y devuelve el stock | `CALL sp_delete_order` | **Eliminar en cascada** |

### 📦 Inventory + Products — módulo Inventario (solo lectura)

| Método | Ruta | Qué hace | BD |
|---|---|---|---|
| GET | `/api/inventory/stock` | Stock por almacén, `NeedsRestock` y `KardexOk` (filtros: almacén, producto, `lowStockOnly`) | `vw_stock_status` |
| GET | `/api/inventory/kardex` | Movimientos (filtros: producto, almacén, `type` = INBOUND/OUTBOUND, fechas, `take`) | `InventoryTransactions` |
| GET | `/api/inventory/warehouses` | Almacenes con su responsable | `Warehouses` + `Employees` |
| GET | `/api/products` | Catálogo con categoría, proveedor y precio (filtros: `search`, `categoryId`) | `Products` + joins |
| GET | `/api/categories` | Categorías | `Categories` |

### 📊 Reports (nuevo)

| Método | Ruta | Qué hace | Requisito |
|---|---|---|---|
| GET | `/api/reports/sales-by-customer` | Ventas totales por cliente | **Vista 2** (por cliente) |
| GET | `/api/reports/sales-by-country` | Ventas totales por país | **Vista 2** (por región) |
| GET | `/api/reports/sales-by-customer-year?year=&customerId=` | Ventas por cliente y año | **Consulta JOIN + GROUP BY + SUM/COUNT/AVG** |
| GET | `/api/reports/sales-by-employee-year?year=` | Ventas por empleado y año | **Consulta "ventas por empleado con totales por año"** |

---

## 3. El flujo más importante: crear un pedido

### Request

```http
POST /api/orders/with-customer
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "newCustomer": { "customerName": "Bodega Miraflores SAC", "contactName": "Mathias", "address": "Av. Larco 123",
                   "city": "Lima", "postalCode": "15074", "country": "Peru" },
  "warehouseId": 1,
  "shipperId": 2,
  "items": [
    { "productId": 11, "quantity": 2 },
    { "productId": 42, "quantity": 1 }
  ]
}
```

Para un cliente existente, en lugar de `newCustomer` se envía `"customerId": 20`. Se manda uno de los dos, **nunca ambos**.

### Qué pasa por dentro

```
1. [ApiController] valida el DTO
     ├─ items no vacío, productId ≥ 1, quantity 1..10000, warehouseId ≥ 1
     └─ exactamente uno de customerId / newCustomer          ──✗──► 400 ValidationProblem
2. Controller arma CreateOrderCommand
     └─ EmployeeId = User.GetEmployeeId()   ← del TOKEN, nunca del body
3. OrderService abre UNA transacción
     ├─ INSERT INTO Customers ... RETURNING CustomerID   (si es cliente nuevo)
     ├─ CALL sp_register_order(cliente, empleado, almacén, '[{"product_id":11,"quantity":2},...]', ...)
     │     └─ en la BD: valida → bloquea stock (FOR UPDATE) → Orders → OrderDetails
     │                  → descuenta InventoryStock → OUTBOUND en el Kardex
     │                  si falta stock ──✗──► NW409 → ROLLBACK (el cliente nuevo tampoco queda)
     ├─ SELECT SUM(LineTotal) FROM vw_order_details_extended   (total del pedido)
     └─ COMMIT
4. 201 Created + header Location: /api/orders/10444
```

### Response

```json
HTTP/1.1 201 Created
Location: /api/orders/10444

{ "orderId": 10444, "customerId": 92, "orderTotal": 56.00 }
```

### Si algo falla

```json
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "status": 409,
  "title": "Insufficient stock",
  "detail": "Insufficient stock for product 11 in warehouse 1 (available: 107, requested: 9999)",
  "code": "NW409"
}
```

---

## 4. Reglas de negocio: dónde se cumple cada una

| Regla | Qué dice | API | BD |
|---|---|---|---|
| R1 | Varios productos por pedido | `Items` es una lista | Agrupa productos repetidos |
| R2 | Un solo almacén por pedido | `WarehouseId` obligatorio | Valida que exista y descuenta solo de ahí |
| R3 | Todo o nada | La transacción envuelve cliente y pedido | La excepción revierte el `CALL` |
| R4 | Precio = `Products.Price` | `OrderItemRequest` **no tiene precio** | Vistas y función usan `p.Price` |
| R5 | Eliminar devuelve el stock | `DELETE` llama a `sp_delete_order`, nunca a `DELETE` directo | INBOUND + cascada |
| R7 | El Kardex no se borra | `KardexEntryDto.OrderId` es `int?` y trae `Notes` | `ON DELETE SET NULL` |
| R8 | Stock = Σ INBOUND − Σ OUTBOUND | **Ningún endpoint escribe stock directamente**. `StockDto.KardexOk` lo verifica | `vw_stock_status` |
| R9 | Ventas por cliente solo incluye compradores | Documentado en Swagger | `JOIN` en la vista |
| — | El vendedor es quien está logueado | `EmployeeId` sale del token | Valida que el empleado exista |
| — | Solo Admin elimina pedidos | `[Authorize(Roles = Roles.Admin)]` → 403 | — |
| — | Cuenta inactiva no entra | `AuthService.LoginAsync` → 401 | `UserAccounts.IsActive` |

---

## 5. Errores: un solo formato

Todo error sale como **ProblemDetails** (RFC 7807). Un único `ErrorHandlingMiddleware`, primero en el pipeline, atrapa cualquier excepción y la traduce:

| Origen | Excepción / código | HTTP | `detail` visible |
|---|---|---|---|
| Validación del DTO (`[Required]`, `[Range]`) | — (lo hace `[ApiController]`) | 400 | Sí (`errors` por campo) |
| Core: recurso inexistente | `NotFoundException` | 404 | Sí |
| Core: regla de negocio | `BusinessRuleException` | 400 | Sí |
| Login / token | `UnauthorizedAccessException` | 401 | Sí |
| Procedimientos de la BD | `NW400` / `NW404` / `NW409` | 400 / 404 / 409 | Sí |
| FK / UNIQUE / NOT NULL / CHECK | `23503` / `23505` / `23502` / `23514` | 409 / 409 / 400 / 400 | **No** (revelaría nombres internos) |
| Cualquier otra cosa | — | 500 | Solo en Development |

Los mensajes `NWxxx` están en inglés porque los ve el usuario. Los escribimos nosotros, así que es seguro mostrarlos. Los mensajes nativos de PostgreSQL no se muestran.

---

## 6. Seguridad

1. **JWT** en todos los controladores (`[Authorize]`), salvo `login`.
2. **Roles.** El JWT lleva el claim `role` y `AddJwtAuthentication` configura `RoleClaimType`, así que `[Authorize(Roles = Roles.Admin)]` funciona directamente.
3. **El empleado sale del token** en crear y eliminar pedidos, para que nadie actúe a nombre de otro.
4. **Conexión directa a PostgreSQL** con la cadena de conexión (EF Core y Npgsql). Después de `08_security.sql`, la API REST de Supabase queda cerrada para `anon` y `authenticated`; además esa API (PostgREST) solo puede llamar funciones, no `PROCEDURE`.
5. **Secretos fuera del repo.** `appsettings.json` trae los valores vacíos; los reales van en `dotnet user-secrets` (cadena de conexión, `Jwt:Key`, `Seed:AdminPassword`).
6. **El primer Admin lo crea `SeedAdminAsync()` al arrancar,** con `MustChangePassword = true`. La contraseña temporal sale de la configuración, nunca del código.

---

## 7. Guion de prueba para la sustentación (Swagger)

| # | Paso | Qué demuestra |
|---|---|---|
| 1 | `POST /api/auth/login` con el admin → `POST /api/auth/change-password` | Cuentas, hash, MustChangePassword |
| 2 | `GET /api/inventory/stock?warehouseId=1&productId=11` | Stock inicial (anota la cantidad) |
| 3 | `POST /api/orders/with-customer` con `newCustomer` | **Insertar cliente + pedido**, **Procedimiento 1** |
| 4 | `GET /api/orders/{id}` | **Vista 1**: productos y total por línea |
| 5 | Repetir el paso 2 + `GET /api/inventory/kardex?productId=11&take=3` | El stock bajó y hay un OUTBOUND `Sale - order N` |
| 6 | `PUT /api/customers/{id}` | **Actualizar dirección** |
| 7 | `GET /api/orders/customer/{id}` | **Procedimiento 2**: historial |
| 8 | `GET /api/reports/sales-by-customer` y `/sales-by-employee-year?year=1997` | **Vista 2** y **consulta por año** |
| 9 | `POST` con `quantity: 9999` | 409 `NW409`, nada guardado (todo o nada) |
| 10 | `DELETE /api/orders/{id}` como Admin | **Eliminación en cascada** + stock devuelto |
| 11 | Repetir el paso 2 y ver `KardexOk: true` | El Kardex cuadra |
| 12 | `DELETE` con un usuario Employee | 403: permisos por rol |

---

## 8. Notas

- **Fechas:** `NOW()` en Supabase está en UTC. Los pedidos nuevos guardan la hora UTC; conviértela en el frontend.
- **Límites:** `take` acepta de 1 a 1000 en pedidos y en el Kardex, para no devolver tablas enteras por accidente.
- **Columnas en minúsculas:** PostgreSQL devuelve `orderid`, `customername`, etc.; los servicios leen con esos nombres. En JSON salen en camelCase (`orderId`) por los DTOs.
- **Pendiente para la v2:** ajustes manuales de inventario y transferencias (siempre vía procedimiento, por R8), compras, pagos y estado del pedido.

## Glosario

| Término | Significado |
|---|---|
| **DTO** | Objeto que define el JSON que entra o sale de un endpoint |
| **ProblemDetails** | Formato estándar de error HTTP (RFC 7807) |
| **Middleware** | Pieza del pipeline HTTP por la que pasa cada petición (aquí, el manejo de errores) |
| **EF Core** | ORM: traduce LINQ (`_db.Customers.Where(...)`) a SQL |
| **NpgsqlDataSource** | Fábrica de conexiones a PostgreSQL, registrada una vez como singleton |
| **PostgREST / RPC** | La API REST automática de Supabase; solo llama funciones, no procedimientos |
| **Orquestar** | Coordinar pasos (transacción, llamadas) sin reimplementar la lógica que ya está en la BD |
