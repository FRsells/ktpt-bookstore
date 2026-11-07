-- Проверочные запросы, которые использовались при тестировании программы (раздел 2.7).
USE bookstore;
GO

-- 1. Текущая цена книг по правилу БП-1: скидка 25 %, если в прошлом календарном месяце
--    по книге не было позиций заказов. Результат должен совпадать с ценами в каталоге программы.
DECLARE @end date = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1); -- 1-е число текущего месяца
DECLARE @start date = DATEADD(MONTH, -1, @end);                          -- 1-е число прошлого месяца
SELECT b.book_id, b.title, b.base_price, b.stock_quantity,
       CASE WHEN EXISTS (SELECT 1 FROM order_items i JOIN orders o ON o.order_id = i.order_id
                         WHERE i.book_id = b.book_id AND o.order_date >= @start AND o.order_date < @end)
            THEN b.base_price
            ELSE CAST(ROUND(b.base_price * 0.75, 2) AS decimal(10,2)) END AS current_price,
       CASE WHEN b.stock_quantity > 5 THEN N'много'
            WHEN b.stock_quantity > 0 THEN N'мало'
            ELSE N'нет в наличии' END AS availability,
       CASE WHEN b.stock_quantity <= 3 THEN N'красная' ELSE N'обычная' END AS card_color
FROM books b
ORDER BY current_price;

-- 2. Остаток книги до и после оформления заказа (подставить номер книги).
SELECT book_id, title, stock_quantity FROM books WHERE book_id = 5;

-- 3. Последние заказы с позициями: покупатель, книга, количество, цена покупки.
SELECT TOP 10 o.order_id, o.order_date, u.login, b.title, i.quantity, i.unit_price,
       i.quantity * i.unit_price AS total
FROM orders o
JOIN users u ON u.user_id = o.client_id
JOIN order_items i ON i.order_id = o.order_id
JOIN books b ON b.book_id = i.book_id
ORDER BY o.order_id DESC;

-- 4. Заказы без позиций (после удаления последней позиции таких быть не должно).
SELECT o.order_id FROM orders o
WHERE NOT EXISTS (SELECT 1 FROM order_items i WHERE i.order_id = o.order_id);

-- 5. Отрицательных остатков быть не должно (их запрещает и ограничение CHECK).
SELECT book_id, stock_quantity FROM books WHERE stock_quantity < 0;

-- 6. Роли пользователей (названия должны совпадать с проверками в Session.cs).
SELECT u.login, u.last_name, r.role_name FROM users u JOIN roles r ON r.role_id = u.role_id ORDER BY r.role_id;

-- 7. Резервная копия перед тестами и восстановление после них.
-- BACKUP DATABASE bookstore TO DISK = N'bookstore_before_tests.bak' WITH INIT;
-- USE master; ALTER DATABASE bookstore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- RESTORE DATABASE bookstore FROM DISK = N'bookstore_before_tests.bak' WITH REPLACE;
-- ALTER DATABASE bookstore SET MULTI_USER;
