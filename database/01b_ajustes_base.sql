-- Ajuste 1: sincronizar secuencias
-- ---------------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('categories',   'categoryid'),    (SELECT MAX(CategoryID)    FROM Categories));
SELECT setval(pg_get_serial_sequence('customers',    'customerid'),    (SELECT MAX(CustomerID)    FROM Customers));
SELECT setval(pg_get_serial_sequence('employees',    'employeeid'),    (SELECT MAX(EmployeeID)    FROM Employees));
SELECT setval(pg_get_serial_sequence('shippers',     'shipperid'),     (SELECT MAX(ShipperID)     FROM Shippers));
SELECT setval(pg_get_serial_sequence('suppliers',    'supplierid'),    (SELECT MAX(SupplierID)    FROM Suppliers));
SELECT setval(pg_get_serial_sequence('products',     'productid'),     (SELECT MAX(ProductID)     FROM Products));
SELECT setval(pg_get_serial_sequence('orders',       'orderid'),       (SELECT MAX(OrderID)       FROM Orders));
SELECT setval(pg_get_serial_sequence('orderdetails', 'orderdetailid'), (SELECT MAX(OrderDetailID) FROM OrderDetails));


-- Ajuste 2: ON DELETE CASCADE en OrderDetails -> Orders, al eliminar un pedido se eliminan sus detalles.
-- ---------------------------------------------------------------------
ALTER TABLE OrderDetails DROP CONSTRAINT IF EXISTS orderdetails_orderid_fkey;

ALTER TABLE OrderDetails
    ADD CONSTRAINT orderdetails_orderid_fkey
    FOREIGN KEY (OrderID) REFERENCES Orders (OrderID)
    ON DELETE CASCADE;


-- Ajuste 3: índices sobre llaves foráneas y filtros frecuentes
-- ---------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_orders_customer      ON Orders (CustomerID);
CREATE INDEX IF NOT EXISTS idx_orders_employee      ON Orders (EmployeeID);
CREATE INDEX IF NOT EXISTS idx_orders_date          ON Orders (OrderDate);
CREATE INDEX IF NOT EXISTS idx_orderdetails_order   ON OrderDetails (OrderID);
CREATE INDEX IF NOT EXISTS idx_orderdetails_product ON OrderDetails (ProductID);
