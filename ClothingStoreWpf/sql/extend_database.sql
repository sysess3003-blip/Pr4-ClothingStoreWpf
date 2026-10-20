/*
  Дополнительная таблица учётных записей для WPF-приложения.
  Основные таблицы Customers, Categories, Products, Orders и OrderItems
  создаются скриптом Lab3_ClothingStoreDB.sql из лабораторной работы № 3.
  Приложение создаёт эту таблицу автоматически при первом запуске.
  Этот скрипт приведён для документирования расширения схемы.
*/
USE Lab3_ClothingStoreDB;
GO
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUsers
    (
        AppUserID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
        CustomerID INT NULL,
        Login NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(300) NOT NULL,
        RoleName NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_AppUsers_Login UNIQUE (Login),
        CONSTRAINT CK_AppUsers_RoleName CHECK (RoleName IN (N'Администратор', N'Менеджер', N'Пользователь')),
        CONSTRAINT FK_AppUsers_Customers FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID)
    );
END;
GO
-- Учётные записи admin и manager создаются приложением с хешированными паролями.
