/* Лабораторная работа № 3. База данных интернет-магазина одежды.
   СУБД: Microsoft SQL Server. Скрипт можно выполнить повторно.
   Он пересоздаёт пять учебных таблиц только в базе Lab3_ClothingStoreDB. */

USE master;
GO
IF DB_ID(N'Lab3_ClothingStoreDB') IS NULL
BEGIN
    CREATE DATABASE Lab3_ClothingStoreDB;
END;
GO

USE Lab3_ClothingStoreDB;
GO

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NOT NULL DROP TABLE dbo.AppUsers;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers
(
    CustomerID       INT IDENTITY(1,1) NOT NULL,
    FullName         NVARCHAR(100) NOT NULL,
    Email            NVARCHAR(254) NOT NULL,
    Phone            NVARCHAR(20) NULL,
    City             NVARCHAR(60) NOT NULL,
    RegistrationDate DATE NOT NULL,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerID),
    CONSTRAINT UQ_Customers_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryID   INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(60) NOT NULL,
    Description  NVARCHAR(250) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);
GO

CREATE TABLE dbo.Products
(
    ProductID     INT IDENTITY(1,1) NOT NULL,
    CategoryID    INT NOT NULL,
    ProductName   NVARCHAR(100) NOT NULL,
    SKU           VARCHAR(20) NOT NULL,
    Size          NVARCHAR(10) NOT NULL,
    Color         NVARCHAR(30) NOT NULL,
    Price         DECIMAL(10,2) NOT NULL,
    StockQuantity INT NOT NULL,
    IsActive      BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CONSTRAINT PK_Products PRIMARY KEY (ProductID),
    CONSTRAINT UQ_Products_SKU UNIQUE (SKU),
    CONSTRAINT CK_Products_Price CHECK (Price > 0),
    CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID)
);
GO

CREATE TABLE dbo.Orders
(
    OrderID        INT IDENTITY(1,1) NOT NULL,
    CustomerID     INT NOT NULL,
    OrderDate      DATETIME2(0) NOT NULL,
    OrderStatus    NVARCHAR(20) NOT NULL,
    DeliveryAddress NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (OrderID),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerID)
        REFERENCES dbo.Customers(CustomerID),
    CONSTRAINT CK_Orders_OrderStatus CHECK
        (OrderStatus IN (N'Новый', N'Оплачен', N'Собирается',
                         N'Отправлен', N'Доставлен', N'Отменён'))
);
GO

CREATE TABLE dbo.OrderItems
(
    OrderItemID INT IDENTITY(1,1) NOT NULL,
    OrderID     INT NOT NULL,
    ProductID   INT NOT NULL,
    Quantity    INT NOT NULL,
    UnitPrice   DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderItemID),
    CONSTRAINT UQ_OrderItems_Order_Product UNIQUE (OrderID, ProductID),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderID)
        REFERENCES dbo.Orders(OrderID),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductID)
        REFERENCES dbo.Products(ProductID)
);
GO

INSERT INTO dbo.Customers
    (FullName, Email, Phone, City, RegistrationDate)
VALUES
    (N'Анна Смирнова',     N'anna.smirnova@example.com',     N'+7-900-111-11-01', N'Вологда',       '2026-01-10'),
    (N'Иван Петров',       N'ivan.petrov@example.com',       N'+7-900-111-11-02', N'Череповец',    '2026-02-14'),
    (N'Мария Кузнецова',   N'maria.kuznetsova@example.com',  N'+7-900-111-11-03', N'Вологда',       '2026-03-05'),
    (N'Дмитрий Соколов',   N'dmitry.sokolov@example.com',    N'+7-900-111-11-04', N'Ярославль',    '2026-04-21'),
    (N'Елена Волкова',     N'elena.volkova@example.com',     N'+7-900-111-11-05', N'Вологда',       '2026-05-12'),
    (N'Павел Орлов',       N'pavel.orlov@example.com',       N'+7-900-111-11-06', N'Сокол',         '2026-06-08');
GO

INSERT INTO dbo.Categories (CategoryName, Description)
VALUES
    (N'Футболки',          N'Футболки и базовые верхние изделия'),
    (N'Брюки',             N'Джинсы и повседневные брюки'),
    (N'Верхняя одежда',    N'Куртки и парки'),
    (N'Обувь',             N'Повседневная обувь'),
    (N'Аксессуары',        N'Головные уборы и сумки');
GO

INSERT INTO dbo.Products
    (CategoryID, ProductName, SKU, Size, Color, Price, StockQuantity, IsActive)
VALUES
    (1, N'Футболка базовая белая', 'CL-TS-001', N'M',  N'Белый',       1290.00, 25, 1),
    (1, N'Футболка базовая чёрная','CL-TS-002', N'L',  N'Чёрный',      1290.00, 30, 1),
    (2, N'Джинсы прямые синие',    'CL-JN-001', N'32', N'Синий',       3490.00, 12, 1),
    (2, N'Брюки чинос чёрные',     'CL-TR-001', N'M',  N'Чёрный',      2790.00, 18, 1),
    (3, N'Куртка джинсовая',       'CL-JK-001', N'L',  N'Голубой',     5990.00, 7,  1),
    (3, N'Парка демисезонная',     'CL-PK-001', N'M',  N'Оливковый',   8990.00, 5,  1),
    (4, N'Кроссовки белые',        'CL-SN-001', N'39', N'Белый',       4990.00, 9,  1),
    (4, N'Ботинки чёрные',         'CL-BT-001', N'40', N'Чёрный',      6490.00, 6,  1),
    (5, N'Кепка чёрная',           'CL-CP-001', N'One size', N'Чёрный', 790.00, 14, 1),
    (5, N'Сумка бежевая',          'CL-BG-001', N'One size', N'Бежевый',1990.00, 8,  1);
GO

INSERT INTO dbo.Orders (CustomerID, OrderDate, OrderStatus, DeliveryAddress)
VALUES
    (1, '2026-10-01T10:30:00', N'Собирается', N'г. Вологда, ул. Мира, д. 10'),
    (2, '2026-10-02T12:15:00', N'Доставлен',  N'г. Череповец, ул. Ленина, д. 25'),
    (3, '2026-10-03T15:20:00', N'Оплачен',    N'г. Вологда, ул. Пушкинская, д. 8'),
    (1, '2026-10-04T17:45:00', N'Доставлен',  N'г. Вологда, ул. Мира, д. 10'),
    (4, '2026-10-05T09:00:00', N'Отправлен',  N'г. Ярославль, ул. Свободы, д. 14'),
    (5, '2026-10-06T11:10:00', N'Новый',      N'г. Вологда, ул. Конева, д. 17'),
    (6, '2026-10-07T18:00:00', N'Отменён',    N'г. Сокол, ул. Советская, д. 3');
GO

INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice)
VALUES
    (1, 1, 2, 1290.00),
    (1, 9, 1,  790.00),
    (2, 3, 1, 3490.00),
    (2, 7, 1, 4990.00),
    (3, 2, 3, 1290.00),
    (3,10, 1, 1990.00),
    (4, 5, 1, 5990.00),
    (4, 8, 1, 6490.00),
    (5, 4, 2, 2790.00),
    (5, 9, 2,  790.00),
    (6, 6, 1, 8990.00),
    (6, 1, 1, 1290.00),
    (7,10, 1, 1990.00);
GO

/* Проверка количества записей в каждой таблице. */
SELECT N'Customers' AS TableName, COUNT(*) AS RecordCount FROM dbo.Customers
UNION ALL
SELECT N'Categories', COUNT(*) FROM dbo.Categories
UNION ALL
SELECT N'Products', COUNT(*) FROM dbo.Products
UNION ALL
SELECT N'Orders', COUNT(*) FROM dbo.Orders
UNION ALL
SELECT N'OrderItems', COUNT(*) FROM dbo.OrderItems;
GO

/* 4 запроса SELECT */
-- SELECT 1. Активные товары стоимостью менее 3000 рублей.
SELECT ProductID, ProductName, Size, Color, Price, StockQuantity
FROM dbo.Products
WHERE IsActive = 1 AND Price < 3000
ORDER BY Price, ProductName;
GO

-- SELECT 2. Покупатели из Вологды.
SELECT CustomerID, FullName, Email, Phone
FROM dbo.Customers
WHERE City = N'Вологда'
ORDER BY FullName;
GO

-- SELECT 3. Заказы за период с 1 по 6 октября 2026 года включительно.
SELECT OrderID, CustomerID, OrderDate, OrderStatus
FROM dbo.Orders
WHERE OrderDate >= '2026-10-01' AND OrderDate < '2026-10-07'
ORDER BY OrderDate;
GO

-- SELECT 4. Товары с остатком менее 8 единиц.
SELECT ProductID, ProductName, StockQuantity
FROM dbo.Products
WHERE StockQuantity < 8
ORDER BY StockQuantity;
GO

/* 4 запроса JOIN */
-- JOIN 1. Заказы вместе с именами покупателей.
SELECT o.OrderID, o.OrderDate, c.FullName, o.OrderStatus
FROM dbo.Orders AS o
INNER JOIN dbo.Customers AS c ON c.CustomerID = o.CustomerID
ORDER BY o.OrderDate;
GO

-- JOIN 2. Товары с названиями категорий.
SELECT p.ProductID, p.ProductName, c.CategoryName, p.Price
FROM dbo.Products AS p
INNER JOIN dbo.Categories AS c ON c.CategoryID = p.CategoryID
ORDER BY c.CategoryName, p.ProductName;
GO

-- JOIN 3. Состав заказов с названием товара и суммой позиции.
SELECT oi.OrderID, p.ProductName, oi.Quantity, oi.UnitPrice,
       oi.Quantity * oi.UnitPrice AS LineTotal
FROM dbo.OrderItems AS oi
INNER JOIN dbo.Products AS p ON p.ProductID = oi.ProductID
ORDER BY oi.OrderID, p.ProductName;
GO

-- JOIN 4. Детализация активных (не отменённых) заказов: покупатель, товар и сумма.
SELECT o.OrderID, c.FullName, o.OrderStatus, p.ProductName,
       oi.Quantity, oi.UnitPrice, oi.Quantity * oi.UnitPrice AS LineTotal
FROM dbo.Orders AS o
INNER JOIN dbo.Customers AS c ON c.CustomerID = o.CustomerID
INNER JOIN dbo.OrderItems AS oi ON oi.OrderID = o.OrderID
INNER JOIN dbo.Products AS p ON p.ProductID = oi.ProductID
WHERE o.OrderStatus <> N'Отменён'
ORDER BY o.OrderID, p.ProductName;
GO

/* 4 запроса GROUP BY */
-- GROUP BY 1. Число заказов каждого покупателя, в том числе ноль заказов.
SELECT c.CustomerID, c.FullName, COUNT(o.OrderID) AS OrderCount
FROM dbo.Customers AS c
LEFT JOIN dbo.Orders AS o ON o.CustomerID = c.CustomerID
GROUP BY c.CustomerID, c.FullName
ORDER BY OrderCount DESC, c.FullName;
GO

-- GROUP BY 2. Число видов товаров и суммарный остаток по категориям.
SELECT c.CategoryName, COUNT(p.ProductID) AS ProductCount,
       COALESCE(SUM(p.StockQuantity), 0) AS TotalStock
FROM dbo.Categories AS c
LEFT JOIN dbo.Products AS p ON p.CategoryID = c.CategoryID
GROUP BY c.CategoryID, c.CategoryName
ORDER BY c.CategoryName;
GO

-- GROUP BY 3. Стоимость каждого неотменённого заказа.
SELECT o.OrderID, o.OrderStatus,
       SUM(oi.Quantity * oi.UnitPrice) AS OrderTotal
FROM dbo.Orders AS o
INNER JOIN dbo.OrderItems AS oi ON oi.OrderID = o.OrderID
WHERE o.OrderStatus <> N'Отменён'
GROUP BY o.OrderID, o.OrderStatus
ORDER BY o.OrderID;
GO

-- GROUP BY 4. Товары, проданные в количестве более одной единицы.
SELECT p.ProductID, p.ProductName, SUM(oi.Quantity) AS UnitsSold
FROM dbo.OrderItems AS oi
INNER JOIN dbo.Products AS p ON p.ProductID = oi.ProductID
INNER JOIN dbo.Orders AS o ON o.OrderID = oi.OrderID
WHERE o.OrderStatus <> N'Отменён'
GROUP BY p.ProductID, p.ProductName
HAVING SUM(oi.Quantity) > 1
ORDER BY UnitsSold DESC, p.ProductName;
GO
