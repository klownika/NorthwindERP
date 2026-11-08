-- ---------------------------------------------------------------------
-- 0. Limpieza previa (por si una ejecución anterior quedó a medias)
-- ---------------------------------------------------------------------
DO $$
DECLARE v_order INTEGER;
BEGIN
    FOR v_order IN
        SELECT o.OrderID FROM Orders o
        JOIN Customers c ON c.CustomerID = o.CustomerID
        WHERE c.CustomerName = 'Bodega Miraflores SAC'
    LOOP
        CALL sp_delete_order(v_order, 1);   -- devuelve stock si el pedido lo movió
    END LOOP;
    DELETE FROM Customers WHERE CustomerName = 'Bodega Miraflores SAC';
END $$;

-- ---------------------------------------------------------------------
-- A. INSERT: nuevo cliente + pedido (Orders + OrderDetails) con SQL directo
--    (no mueve inventario; para eso está el procedimiento del bloque D)
-- ---------------------------------------------------------------------
SELECT COUNT(*) AS CustomersBefore FROM Customers;

INSERT INTO Customers (CustomerName, ContactName, Address, City, PostalCode, Country)
VALUES ('Bodega Miraflores SAC', 'Sasha Cardozo', 'Av. Larco 123', 'Lima', '15074', 'Peru');

INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, ShipperID)
VALUES (currval(pg_get_serial_sequence('customers', 'customerid')), 1, NOW(), 1);

INSERT INTO OrderDetails (OrderID, ProductID, Quantity) VALUES
(currval(pg_get_serial_sequence('orders', 'orderid')), 11, 5),
(currval(pg_get_serial_sequence('orders', 'orderid')), 42, 3);

SELECT * FROM vw_order_details_extended
WHERE CustomerName = 'Bodega Miraflores SAC';

-- ---------------------------------------------------------------------
-- B. UPDATE: cambiar la dirección de un cliente existente
-- ---------------------------------------------------------------------
SELECT CustomerID, CustomerName, Address, City FROM Customers
WHERE CustomerName = 'Bodega Miraflores SAC';

UPDATE Customers
SET Address = 'Calle Schell 456', PostalCode = '15074'
WHERE CustomerName = 'Bodega Miraflores SAC';

SELECT CustomerID, CustomerName, Address, City FROM Customers
WHERE CustomerName = 'Bodega Miraflores SAC';

-- ---------------------------------------------------------------------
-- C. DELETE en cascada: borrar el pedido del bloque A
-- ---------------------------------------------------------------------
SELECT o.OrderID, COUNT(od.OrderDetailID) AS DetailsBefore
FROM Orders o
JOIN Customers c ON c.CustomerID = o.CustomerID
LEFT JOIN OrderDetails od ON od.OrderID = o.OrderID
WHERE c.CustomerName = 'Bodega Miraflores SAC'
GROUP BY o.OrderID;

DELETE FROM Orders
WHERE CustomerID = (SELECT CustomerID FROM Customers WHERE CustomerName = 'Bodega Miraflores SAC');

-- Debe devolver 0: los detalles se borraron solos por ON DELETE CASCADE
SELECT COUNT(*) AS OrphanDetails
FROM OrderDetails od
LEFT JOIN Orders o ON o.OrderID = od.OrderID
WHERE o.OrderID IS NULL;

-- ---------------------------------------------------------------------
-- D. Procedimiento 1: registrar pedido completo (descuenta stock del almacén 1)
-- ---------------------------------------------------------------------
SELECT WarehouseID, ProductID, Quantity FROM InventoryStock
WHERE WarehouseID = 1 AND ProductID IN (11, 42) ORDER BY ProductID;

DO $$
DECLARE
    v_customer INTEGER;
    v_order    INTEGER;
BEGIN
    SELECT CustomerID INTO v_customer FROM Customers WHERE CustomerName = 'Bodega Miraflores SAC';
    CALL sp_register_order(
        v_customer,
        1,   -- empleado
        1,   -- almacén
        '[{"product_id": 11, "quantity": 5}, {"product_id": 42, "quantity": 3}]'::jsonb,
        2,   -- transportista
        v_order
    );
    RAISE NOTICE 'Order created: %', v_order;
END $$;

SELECT * FROM vw_order_details_extended WHERE OrderID = (SELECT MAX(OrderID) FROM Orders);

SELECT WarehouseID, ProductID, Quantity FROM InventoryStock
WHERE WarehouseID = 1 AND ProductID IN (11, 42) ORDER BY ProductID;

SELECT TransactionID, WarehouseID, ProductID, TransactionType, Quantity, OrderID, Notes
FROM InventoryTransactions ORDER BY TransactionID DESC LIMIT 2;

-- ---------------------------------------------------------------------
-- E. Procedimiento 2: historial de pedidos de un cliente
-- ---------------------------------------------------------------------
SELECT * FROM get_customer_order_history(
    (SELECT CustomerID FROM Customers WHERE CustomerName = 'Bodega Miraflores SAC'));

SELECT * FROM get_customer_order_history(20);   -- Ernst Handel, cliente histórico

-- ---------------------------------------------------------------------
-- F. Eliminar el pedido del bloque D devolviendo el stock
-- ---------------------------------------------------------------------
DO $$
DECLARE v_order INTEGER;
BEGIN
    SELECT MAX(OrderID) INTO v_order FROM Orders;
    CALL sp_delete_order(v_order, 1);
    RAISE NOTICE 'Order deleted: %', v_order;
END $$;

-- El stock vuelve al valor inicial y el Kardex sigue cuadrando
SELECT WarehouseID, ProductID, Quantity FROM InventoryStock
WHERE WarehouseID = 1 AND ProductID IN (11, 42) ORDER BY ProductID;

SELECT COUNT(*) AS MismatchedRows FROM vw_stock_status WHERE NOT KardexOk;  -- debe ser 0


-- ---------------------------------------------------------------------
-- G. Limpieza final: deja la base como estaba antes de la demo
-- ---------------------------------------------------------------------
DO $$
DECLARE v_order INTEGER;
BEGIN
    FOR v_order IN
        SELECT o.OrderID FROM Orders o
        JOIN Customers c ON c.CustomerID = o.CustomerID
        WHERE c.CustomerName = 'Bodega Miraflores SAC'
    LOOP
        CALL sp_delete_order(v_order, 1);   -- devuelve stock si el pedido lo movió
    END LOOP;
    DELETE FROM Customers WHERE CustomerName = 'Bodega Miraflores SAC';
END $$;

SELECT COUNT(*) AS CustomersAfter FROM Customers;              -- 91
SELECT COUNT(*) AS MismatchedRows FROM vw_stock_status WHERE NOT KardexOk;  -- 0
