-- Roles de usuario 
CREATE TABLE Roles
(
    RoleID   SERIAL PRIMARY KEY,
    RoleName VARCHAR(25) NOT NULL UNIQUE
);

-- Cada usuario del sistema es un empleado de Northwind
CREATE TABLE UserAccounts
(
    UserAccountID      SERIAL PRIMARY KEY,
    EmployeeID         INTEGER     NOT NULL UNIQUE,
    Username           VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash       TEXT        NOT NULL,
    RoleID             INTEGER     NOT NULL,
    IsActive           BOOLEAN     NOT NULL DEFAULT TRUE,
    MustChangePassword BOOLEAN     NOT NULL DEFAULT TRUE,
    CreatedAt          TIMESTAMP   NOT NULL DEFAULT NOW(),

    FOREIGN KEY (EmployeeID) REFERENCES Employees (EmployeeID),
    FOREIGN KEY (RoleID)     REFERENCES Roles (RoleID)
);

-- Inventario
CREATE TABLE Warehouses
(
    WarehouseID       SERIAL PRIMARY KEY,
    WarehouseName     VARCHAR(50)  NOT NULL UNIQUE,
    Location s         VARCHAR(100) NOT NULL,
    ManagerEmployeeID INTEGER      NOT NULL,

    FOREIGN KEY (ManagerEmployeeID) REFERENCES Employees (EmployeeID)
);

-- Stock actual: una fila por (almacén, producto)
CREATE TABLE InventoryStock
(
    StockID       SERIAL PRIMARY KEY,
    WarehouseID   INTEGER NOT NULL,
    ProductID     INTEGER NOT NULL,
    Quantity      INTEGER NOT NULL DEFAULT 0 CHECK (Quantity >= 0),
    MinStockLevel INTEGER NOT NULL DEFAULT 0 CHECK (MinStockLevel >= 0),

    UNIQUE (WarehouseID, ProductID),
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses (WarehouseID),
    FOREIGN KEY (ProductID)   REFERENCES Products (ProductID)
);

-- Kardex: historial de movimientos. 
-- Regla: el stock de cada fila de InventoryStock = SUM(INBOUND) - SUM(OUTBOUND) de sus movimientos.
--   INBOUND  = entra mercadería (stock inicial, devolución por pedido eliminado)
--   OUTBOUND = sale mercadería (venta registrada con sp_register_order)
CREATE TABLE InventoryTransactions
(
    TransactionID   SERIAL PRIMARY KEY,
    WarehouseID     INTEGER     NOT NULL,
    ProductID       INTEGER     NOT NULL,
    EmployeeID      INTEGER     NOT NULL,
    TransactionType VARCHAR(10) NOT NULL CHECK (TransactionType IN ('INBOUND', 'OUTBOUND')),
    Quantity        INTEGER     NOT NULL CHECK (Quantity > 0),
    TransactionDate TIMESTAMP   NOT NULL DEFAULT NOW(),
    OrderID         INTEGER,
    Notes           VARCHAR(200),

    FOREIGN KEY (WarehouseID) REFERENCES Warehouses (WarehouseID),
    FOREIGN KEY (ProductID)   REFERENCES Products (ProductID),
    FOREIGN KEY (EmployeeID)  REFERENCES Employees (EmployeeID),
    FOREIGN KEY (OrderID)     REFERENCES Orders (OrderID) ON DELETE SET NULL
);

-- Índices del Kardex (los de Orders/OrderDetails están en 01b)
CREATE INDEX idx_invtx_product_date    ON InventoryTransactions (ProductID, TransactionDate);
CREATE INDEX idx_invtx_order           ON InventoryTransactions (OrderID);
