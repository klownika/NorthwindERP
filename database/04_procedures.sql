-- =====================================================================
-- 05_procedures.sql
--
-- REGLAS DE NEGOCIO v1
--   R1. Un pedido puede tener VARIOS productos (lista JSON).
--   R2. Cada pedido sale de UN SOLO almacén, que se indica como parámetro.
--       Si a ese almacén le falta stock de cualquier producto, el pedido
--       completo se rechaza (no se guarda nada).
--   R3. El precio es siempre Products.Price (no se recibe por parámetro).
--   (Los mensajes de error y las notas del Kardex están en inglés porque
--    llegan a la API y a la interfaz. Formato de notas:
--      'Initial stock' | 'Sale - order N' | 'Return - order N deleted')
--   R4. Eliminar un pedido con sp_delete_order devuelve el stock al almacén
--       de donde salió y deja un INBOUND en el Kardex. Los movimientos
--       anteriores no se borran (el Kardex es historial).
--       Un DELETE directo sobre Orders también funciona (cascada a
--       OrderDetails) pero NO devuelve stock: úsalo solo para pedidos que
--       no movieron inventario (los históricos de 1996-97 o los de la demo).
--
-- CÓDIGOS DE ERROR (SQLSTATE propio, para que el backend los traduzca a HTTP)
--   NW400 -> 400 Bad Request   datos inválidos (lista vacía, cantidad no numérica o <= 0)
--   NW404 -> 404 Not Found     cliente, empleado, almacén, transportista, producto o pedido inexistente
--   NW409 -> 409 Conflict      stock insuficiente
-- =====================================================================

-- ---------------------------------------------------------------------
-- Procedimiento 1 (requisito): registra un pedido completo
--   CALL sp_register_order(cliente, empleado, almacén,
--        '[{"product_id": 11, "quantity": 5}, {"product_id": 42, "quantity": 2}]',
--        transportista, NULL);
-- Devuelve el OrderID generado en p_order_id.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE sp_register_order(
    p_customer_id  INTEGER,
    p_employee_id  INTEGER,
    p_warehouse_id INTEGER,
    p_details      JSONB,
    p_shipper_id   INTEGER DEFAULT NULL,
    INOUT p_order_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_line  RECORD;
    v_stock INTEGER;
BEGIN
    -- Validaciones de cabecera
    IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerID = p_customer_id) THEN
        RAISE EXCEPTION 'Customer % not found', p_customer_id USING ERRCODE = 'NW404';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeID = p_employee_id) THEN
        RAISE EXCEPTION 'Employee % not found', p_employee_id USING ERRCODE = 'NW404';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM Warehouses WHERE WarehouseID = p_warehouse_id) THEN
        RAISE EXCEPTION 'Warehouse % not found', p_warehouse_id USING ERRCODE = 'NW404';
    END IF;
    IF p_shipper_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Shippers WHERE ShipperID = p_shipper_id) THEN
        RAISE EXCEPTION 'Shipper % not found', p_shipper_id USING ERRCODE = 'NW404';
    END IF;

    -- Validaciones del detalle
    IF p_details IS NULL OR jsonb_typeof(p_details) <> 'array' THEN
        RAISE EXCEPTION 'Order details must be a JSON array' USING ERRCODE = 'NW400';
    END IF;
    IF jsonb_array_length(p_details) = 0 THEN
        RAISE EXCEPTION 'Order must contain at least one product' USING ERRCODE = 'NW400';
    END IF;
    -- Cada elemento debe ser {"product_id": entero, "quantity": entero > 0}.
    -- Se revisa el tipo JSON ANTES de convertir, para dar un error claro
    -- en vez del error crudo "invalid input syntax for type integer".
    IF EXISTS (
        SELECT 1
        FROM jsonb_array_elements(p_details) AS e
        WHERE jsonb_typeof(e) <> 'object'
           OR jsonb_typeof(e -> 'product_id') IS DISTINCT FROM 'number'
           OR jsonb_typeof(e -> 'quantity')   IS DISTINCT FROM 'number'
           OR CASE WHEN jsonb_typeof(e -> 'product_id') = 'number'
                    AND jsonb_typeof(e -> 'quantity')   = 'number'
                   THEN (e ->> 'product_id')::NUMERIC <> TRUNC((e ->> 'product_id')::NUMERIC)
                     OR (e ->> 'quantity')::NUMERIC   <> TRUNC((e ->> 'quantity')::NUMERIC)
                     OR (e ->> 'quantity')::NUMERIC   <= 0
                   ELSE FALSE END
    ) THEN
        RAISE EXCEPTION 'Each item must have integer product_id and quantity, with quantity greater than zero'
            USING ERRCODE = 'NW400';
    END IF;

    -- Cabecera del pedido
    INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, ShipperID)
    VALUES (p_customer_id, p_employee_id, NOW(), p_shipper_id)
    RETURNING OrderID INTO p_order_id;

    -- Una línea por producto (si viene repetido, se suman las cantidades)
    FOR v_line IN
        SELECT d.product_id, SUM(d.quantity)::INTEGER AS quantity
        FROM jsonb_to_recordset(p_details) AS d(product_id INTEGER, quantity INTEGER)
        GROUP BY d.product_id
        ORDER BY d.product_id
    LOOP
        IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductID = v_line.product_id) THEN
            RAISE EXCEPTION 'Product % not found', v_line.product_id USING ERRCODE = 'NW404';
        END IF;

        -- Bloquea la fila de stock para que dos pedidos simultáneos no
        -- vendan las mismas unidades
        SELECT Quantity INTO v_stock
        FROM InventoryStock
        WHERE WarehouseID = p_warehouse_id AND ProductID = v_line.product_id
        FOR UPDATE;

        IF v_stock IS NULL OR v_stock < v_line.quantity THEN
            RAISE EXCEPTION 'Insufficient stock for product % in warehouse % (available: %, requested: %)',
                v_line.product_id, p_warehouse_id, COALESCE(v_stock, 0), v_line.quantity
                USING ERRCODE = 'NW409';
        END IF;

        INSERT INTO OrderDetails (OrderID, ProductID, Quantity)
        VALUES (p_order_id, v_line.product_id, v_line.quantity);

        UPDATE InventoryStock
        SET Quantity = Quantity - v_line.quantity
        WHERE WarehouseID = p_warehouse_id AND ProductID = v_line.product_id;

        INSERT INTO InventoryTransactions
            (WarehouseID, ProductID, EmployeeID, TransactionType, Quantity, OrderID, Notes)
        VALUES
            (p_warehouse_id, v_line.product_id, p_employee_id, 'OUTBOUND', v_line.quantity,
             p_order_id, 'Sale - order ' || p_order_id);
    END LOOP;
END;
$$;

-- ---------------------------------------------------------------------
-- Procedimiento 2 (requisito): historial de pedidos de un cliente
-- Es FUNCTION y no PROCEDURE para que pueda devolver los valores de las tablas
--   SELECT * FROM get_customer_order_history(1);
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION get_customer_order_history(p_customer_id INTEGER)
RETURNS TABLE (
    OrderID      INTEGER,
    OrderDate    TIMESTAMP,
    ShipperName  VARCHAR,
    EmployeeName TEXT,
    ProductName  VARCHAR,
    Quantity     INTEGER,
    UnitPrice    NUMERIC,
    LineTotal    NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT o.OrderID,
           o.OrderDate,
           sh.ShipperName,
           e.FirstName || ' ' || e.LastName,
           p.ProductName,
           od.Quantity,
           p.Price,
           od.Quantity * p.Price
    FROM Orders o
    JOIN OrderDetails od  ON od.OrderID  = o.OrderID
    JOIN Products p       ON p.ProductID = od.ProductID
    LEFT JOIN Employees e ON e.EmployeeID = o.EmployeeID
    LEFT JOIN Shippers sh ON sh.ShipperID = o.ShipperID
    WHERE o.CustomerID = p_customer_id
    ORDER BY o.OrderDate DESC, o.OrderID DESC, p.ProductName;
$$;

-- ---------------------------------------------------------------------
-- Procedimiento 3 (ERP): elimina un pedido devolviendo su stock (regla R4)
--   CALL sp_delete_order(10444, 1);
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE sp_delete_order(
    p_order_id    INTEGER,
    p_employee_id INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_mov RECORD;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderID = p_order_id) THEN
        RAISE EXCEPTION 'Order % not found', p_order_id USING ERRCODE = 'NW404';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeID = p_employee_id) THEN
        RAISE EXCEPTION 'Employee % not found', p_employee_id USING ERRCODE = 'NW404';
    END IF;

    -- Devuelve lo que salió del almacén por este pedido
    FOR v_mov IN
        SELECT WarehouseID, ProductID, SUM(Quantity)::INTEGER AS quantity
        FROM InventoryTransactions
        WHERE OrderID = p_order_id AND TransactionType = 'OUTBOUND'
        GROUP BY WarehouseID, ProductID
    LOOP
        UPDATE InventoryStock
        SET Quantity = Quantity + v_mov.quantity
        WHERE WarehouseID = v_mov.WarehouseID AND ProductID = v_mov.ProductID;

        INSERT INTO InventoryTransactions
            (WarehouseID, ProductID, EmployeeID, TransactionType, Quantity, OrderID, Notes)
        VALUES
            (v_mov.WarehouseID, v_mov.ProductID, p_employee_id, 'INBOUND', v_mov.quantity,
             NULL, 'Return - order ' || p_order_id || ' deleted');
    END LOOP;

    -- La cascada borra OrderDetails; el Kardex conserva sus filas (OrderID -> NULL)
    DELETE FROM Orders WHERE OrderID = p_order_id;
END;
$$;
