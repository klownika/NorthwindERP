-- Consulta 1 — Uno a muchos: Clientes -> Pedidos
-- Cuántos pedidos tiene cada cliente (LEFT JOIN incluyendo clientes sin pedidos)
SELECT c.CustomerID,
       c.CustomerName,
       c.Country,
       COUNT(o.OrderID) AS TotalOrders
FROM Customers c
LEFT JOIN Orders o ON o.CustomerID = c.CustomerID
GROUP BY c.CustomerID, c.CustomerName, c.Country
ORDER BY TotalOrders DESC, c.CustomerName;

-- Consulta 2 — Uno a muchos: Pedidos -> Detalles, y Empleados -> Pedidos
-- Cada pedido con su empleado, número de líneas y total
SELECT o.OrderID,
       o.OrderDate::DATE                   AS OrderDate,
       e.FirstName || ' ' || e.LastName    AS EmployeeName,
       COUNT(od.OrderDetailID)             AS LineCount,
       SUM(od.Quantity * p.Price)          AS OrderTotal
FROM Orders o
JOIN Employees e     ON e.EmployeeID = o.EmployeeID
JOIN OrderDetails od ON od.OrderID   = o.OrderID
JOIN Products p      ON p.ProductID  = od.ProductID
GROUP BY o.OrderID, o.OrderDate, e.FirstName, e.LastName
ORDER BY o.OrderID;

-- Consulta 3 — JOIN + GROUP BY + SUM/COUNT/AVG
-- Ventas por CLIENTE con totales por año
SELECT c.CustomerName,
       EXTRACT(YEAR FROM o.OrderDate)::INT          AS SalesYear,
       COUNT(DISTINCT o.OrderID)                    AS TotalOrders,
       SUM(od.Quantity * p.Price)                   AS TotalSales,
       ROUND(AVG(od.Quantity * p.Price), 2)         AS AvgLineTotal
FROM Customers c
JOIN Orders o        ON o.CustomerID = c.CustomerID
JOIN OrderDetails od ON od.OrderID   = o.OrderID
JOIN Products p      ON p.ProductID  = od.ProductID
GROUP BY c.CustomerName, EXTRACT(YEAR FROM o.OrderDate)
ORDER BY c.CustomerName, SalesYear;

-- Consulta 4 — JOIN + GROUP BY + SUM/COUNT/AVG
-- Ventas por EMPLEADO con totales por año
SELECT e.EmployeeID,
       e.FirstName || ' ' || e.LastName             AS EmployeeName,
       EXTRACT(YEAR FROM o.OrderDate)::INT          AS SalesYear,
       COUNT(DISTINCT o.OrderID)                    AS TotalOrders,
       SUM(od.Quantity * p.Price)                   AS TotalSales,
       ROUND(SUM(od.Quantity * p.Price) / COUNT(DISTINCT o.OrderID), 2) AS AvgOrderValue
FROM Employees e
JOIN Orders o        ON o.EmployeeID = e.EmployeeID
JOIN OrderDetails od ON od.OrderID   = o.OrderID
JOIN Products p      ON p.ProductID  = od.ProductID
GROUP BY e.EmployeeID, e.FirstName, e.LastName, EXTRACT(YEAR FROM o.OrderDate)
ORDER BY SalesYear, TotalSales DESC;
