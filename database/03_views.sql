-- Vista 1 (requisito): pedidos con sus productos y el total por línea
CREATE OR REPLACE VIEW vw_order_details_extended
WITH (security_invoker = true) AS
SELECT o.OrderID,
       o.OrderDate,
       c.CustomerID,
       c.CustomerName,
       c.Country,
       e.EmployeeID,
       e.FirstName || ' ' || e.LastName AS EmployeeName,
       sh.ShipperName,
       p.ProductID,
       p.ProductName,
       cat.CategoryName,
       od.Quantity,
       p.Price                AS UnitPrice,
       od.Quantity * p.Price  AS LineTotal
FROM Orders o
JOIN OrderDetails od     ON od.OrderID    = o.OrderID
JOIN Products p          ON p.ProductID   = od.ProductID
LEFT JOIN Categories cat ON cat.CategoryID = p.CategoryID
LEFT JOIN Customers c    ON c.CustomerID  = o.CustomerID
LEFT JOIN Employees e    ON e.EmployeeID  = o.EmployeeID
LEFT JOIN Shippers sh    ON sh.ShipperID  = o.ShipperID;

-- Vista 2 (requisito): total de ventas por cliente (que cuentan con pedidos)
CREATE OR REPLACE VIEW vw_sales_by_customer
WITH (security_invoker = true) AS
SELECT c.CustomerID,
       c.CustomerName,
       c.Country,
       COUNT(DISTINCT o.OrderID)        AS TotalOrders,
       SUM(od.Quantity)                 AS TotalUnits,
       SUM(od.Quantity * p.Price)       AS TotalSales
FROM Customers c
JOIN Orders o        ON o.CustomerID = c.CustomerID
JOIN OrderDetails od ON od.OrderID   = o.OrderID
JOIN Products p      ON p.ProductID  = od.ProductID
GROUP BY c.CustomerID, c.CustomerName, c.Country;

-- Vista 3 (extra): total de ventas por región (país)
CREATE OR REPLACE VIEW vw_sales_by_country
WITH (security_invoker = true) AS
SELECT c.Country,
       COUNT(DISTINCT c.CustomerID)     AS TotalCustomers,
       COUNT(DISTINCT o.OrderID)        AS TotalOrders,
       SUM(od.Quantity * p.Price)       AS TotalSales
FROM Customers c
JOIN Orders o        ON o.CustomerID = c.CustomerID
JOIN OrderDetails od ON od.OrderID   = o.OrderID
JOIN Products p      ON p.ProductID  = od.ProductID
GROUP BY c.Country;

-- Vista 4 (ERP): estado del stock por almacén + verificación contra Kardex
CREATE OR REPLACE VIEW vw_stock_status
WITH (security_invoker = true) AS
SELECT w.WarehouseID,
       w.WarehouseName,
       p.ProductID,
       p.ProductName,
       s.Quantity,
       s.MinStockLevel,
       (s.Quantity <= s.MinStockLevel) AS NeedsRestock,
       COALESCE(k.KardexBalance, 0)    AS KardexBalance,
       (s.Quantity = COALESCE(k.KardexBalance, 0)) AS KardexOk
FROM InventoryStock s
JOIN Warehouses w ON w.WarehouseID = s.WarehouseID
JOIN Products p   ON p.ProductID   = s.ProductID
LEFT JOIN (
    SELECT WarehouseID, ProductID,
           SUM(CASE WHEN TransactionType = 'INBOUND' THEN Quantity ELSE -Quantity END) AS KardexBalance
    FROM InventoryTransactions
    GROUP BY WarehouseID, ProductID
) k ON k.WarehouseID = s.WarehouseID AND k.ProductID = s.ProductID;

COMMENT ON VIEW vw_sales_by_customer IS
  'Sales per customer. Only customers with at least one order (inner JOIN). Customers with no orders appear in Query 1 of 06_consultas.sql.';
