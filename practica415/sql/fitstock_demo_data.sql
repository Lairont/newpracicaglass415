-- Optional, clearly marked demo records for the first local presentation.
-- Safe to run more than once: product SKUs, order numbers, and movement notes are checked.
BEGIN;

INSERT INTO products
    ("Name", "Sku", "Category", "Unit", "Quantity", "MinimumStock", "UnitPrice", "Zone", "Condition", "PurchaseDate", "WarrantyUntil", "Notes")
VALUES
    ('Коврик для йоги, демо', 'DEMO-YOGA-001', 'Йога', 'шт', 18, 5, 350.00, 'A-01', 1, CURRENT_DATE::timestamp, NULL, 'Демонстрационная запись. Замените своими данными.'),
    ('Набор гантелей, демо', 'DEMO-DUMBBELL-001', 'Силовой инвентарь', 'упак', 8, 2, 1250.00, 'A-02', 1, CURRENT_DATE::timestamp, NULL, 'Демонстрационная запись. Замените своими данными.'),
    ('Эспандер, демо', 'DEMO-BAND-001', 'Функциональный тренинг', 'шт', 2, 5, 180.00, 'B-01', 1, CURRENT_DATE::timestamp, NULL, 'Демонстрационная запись с низким остатком.')
ON CONFLICT ("Sku") DO NOTHING;

INSERT INTO stock_movements
    ("ProductId", "ProductName", "Sku", "Type", "Quantity", "Unit", "Zone", "Note", "CreatedAt")
SELECT p."Id", p."Name", p."Sku", 0, seed.quantity, p."Unit", p."Zone", seed.note, LOCALTIMESTAMP - interval '2 days'
FROM (VALUES
    ('DEMO-YOGA-001', 20, 'Демо: начальный остаток'),
    ('DEMO-DUMBBELL-001', 8, 'Демо: начальный остаток'),
    ('DEMO-BAND-001', 2, 'Демо: начальный остаток')
) AS seed(sku, quantity, note)
JOIN products p ON p."Sku" = seed.sku
WHERE NOT EXISTS (
    SELECT 1 FROM stock_movements m WHERE m."Sku" = seed.sku AND m."Note" = seed.note
);

INSERT INTO warehouse_orders
    ("Number", "Customer", "ProductId", "ProductName", "Sku", "Quantity", "UnitPrice", "Status", "CreatedAt")
SELECT 'DEMO-ORD-001', 'Демонстрационный заказ (выполнен)', p."Id", p."Name", p."Sku", 2, p."UnitPrice", 1, LOCALTIMESTAMP - interval '1 day'
FROM products p
WHERE p."Sku" = 'DEMO-YOGA-001'
ON CONFLICT ("Number") DO NOTHING;

INSERT INTO stock_movements
    ("ProductId", "ProductName", "Sku", "Type", "Quantity", "Unit", "Zone", "Note", "CreatedAt")
SELECT p."Id", p."Name", p."Sku", 1, 2, p."Unit", p."Zone", 'Демо: отгрузка DEMO-ORD-001', LOCALTIMESTAMP - interval '1 day'
FROM products p
WHERE p."Sku" = 'DEMO-YOGA-001'
  AND EXISTS (SELECT 1 FROM warehouse_orders o WHERE o."Number" = 'DEMO-ORD-001' AND o."Status" = 1)
  AND NOT EXISTS (SELECT 1 FROM stock_movements m WHERE m."Sku" = p."Sku" AND m."Note" = 'Демо: отгрузка DEMO-ORD-001');

INSERT INTO warehouse_orders
    ("Number", "Customer", "ProductId", "ProductName", "Sku", "Quantity", "UnitPrice", "Status", "CreatedAt")
SELECT 'DEMO-ORD-002', 'Демонстрационный заказ (ожидает отгрузки)', p."Id", p."Name", p."Sku", 1, p."UnitPrice", 0, LOCALTIMESTAMP
FROM products p
WHERE p."Sku" = 'DEMO-DUMBBELL-001'
ON CONFLICT ("Number") DO NOTHING;

COMMIT;
