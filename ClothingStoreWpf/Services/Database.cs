using ClothingStoreWpf.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ClothingStoreWpf.Services;

/// <summary>ADO.NET access to the database created in laboratory work 3.</summary>
public static class Database
{
    // Change Server if your SQL Server instance is named differently, for example .\SQLEXPRESS.
    public static string ConnectionString { get; set; } =
        Environment.GetEnvironmentVariable("CLOTHING_STORE_CONNECTION") ??
        "Server=localhost;Database=Lab3_ClothingStoreDB;Integrated Security=True;TrustServerCertificate=True;";

    public static readonly string[] OrderStatuses =
        ["Новый", "Оплачен", "Собирается", "Отправлен", "Доставлен", "Отменён"];

    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        using (SqlConnection connection = OpenConnection())
        using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = @"
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
END;";
            command.ExecuteNonQuery();
        }

        CreateSeedAccountIfMissing("admin", "Admin123!", Roles.Admin);
        CreateSeedAccountIfMissing("manager", "Manager123!", Roles.Manager);
    }

    private static void CreateSeedAccountIfMissing(string login, string password, string role)
    {
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Login = @Login)
    INSERT INTO dbo.AppUsers (CustomerID, Login, PasswordHash, RoleName, IsActive)
    VALUES (NULL, @Login, @PasswordHash, @RoleName, 1);";
        command.Parameters.AddWithValue("@Login", login);
        command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.Hash(password));
        command.Parameters.AddWithValue("@RoleName", role);
        command.ExecuteNonQuery();
    }

    public static AppUser? Authenticate(string login, string password)
    {
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = @"SELECT AppUserID, CustomerID, Login, PasswordHash, RoleName
                                FROM dbo.AppUsers WHERE Login = @Login AND IsActive = 1;";
        command.Parameters.AddWithValue("@Login", login.Trim());
        using SqlDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        string hash = reader.GetString(3);
        if (!PasswordHasher.Verify(password, hash)) return null;

        return new AppUser
        {
            AppUserId = reader.GetInt32(0),
            CustomerId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
            Login = reader.GetString(2),
            RoleName = reader.GetString(4)
        };
    }

    public static AppUser RegisterCustomer(string fullName, string email, string phone, string city, string login, string password)
    {
        using SqlConnection connection = OpenConnection();
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            int customerId;
            using (SqlCommand customerCommand = connection.CreateCommand())
            {
                customerCommand.Transaction = transaction;
                customerCommand.CommandText = @"INSERT INTO dbo.Customers (FullName, Email, Phone, City, RegistrationDate)
                                                VALUES (@FullName, @Email, @Phone, @City, CAST(GETDATE() AS date));
                                                SELECT CAST(SCOPE_IDENTITY() AS int);";
                customerCommand.Parameters.AddWithValue("@FullName", fullName.Trim());
                customerCommand.Parameters.AddWithValue("@Email", email.Trim());
                object phoneValue = string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone.Trim();
                customerCommand.Parameters.AddWithValue("@Phone", phoneValue);
                customerCommand.Parameters.AddWithValue("@City", city.Trim());
                customerId = Convert.ToInt32(customerCommand.ExecuteScalar());
            }

            int appUserId;
            using (SqlCommand userCommand = connection.CreateCommand())
            {
                userCommand.Transaction = transaction;
                userCommand.CommandText = @"INSERT INTO dbo.AppUsers (CustomerID, Login, PasswordHash, RoleName, IsActive)
                                            VALUES (@CustomerID, @Login, @PasswordHash, @RoleName, 1);
                                            SELECT CAST(SCOPE_IDENTITY() AS int);";
                userCommand.Parameters.AddWithValue("@CustomerID", customerId);
                userCommand.Parameters.AddWithValue("@Login", login.Trim());
                userCommand.Parameters.AddWithValue("@PasswordHash", PasswordHasher.Hash(password));
                userCommand.Parameters.AddWithValue("@RoleName", Roles.User);
                appUserId = Convert.ToInt32(userCommand.ExecuteScalar());
            }

            transaction.Commit();
            return new AppUser { AppUserId = appUserId, CustomerId = customerId, Login = login.Trim(), RoleName = Roles.User };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static List<Product> GetProducts(bool includeInactive = false)
    {
        var products = new List<Product>();
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = @"SELECT p.ProductID, p.CategoryID, c.CategoryName, p.ProductName, p.SKU,
                                       p.Size, p.Color, p.Price, p.StockQuantity, p.IsActive
                                FROM dbo.Products AS p
                                INNER JOIN dbo.Categories AS c ON c.CategoryID = p.CategoryID
                                WHERE (@IncludeInactive = 1 OR p.IsActive = 1)
                                ORDER BY c.CategoryName, p.ProductName;";
        command.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive;
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(new Product
            {
                ProductID = reader.GetInt32(0), CategoryID = reader.GetInt32(1), CategoryName = reader.GetString(2),
                ProductName = reader.GetString(3), SKU = reader.GetString(4), Size = reader.GetString(5),
                Color = reader.GetString(6), Price = reader.GetDecimal(7), StockQuantity = reader.GetInt32(8),
                IsActive = reader.GetBoolean(9)
            });
        }
        return products;
    }

    public static List<Category> GetCategories()
    {
        var categories = new List<Category>();
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = new("SELECT CategoryID, CategoryName FROM dbo.Categories ORDER BY CategoryName;", connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read()) categories.Add(new Category { CategoryID = reader.GetInt32(0), CategoryName = reader.GetString(1) });
        return categories;
    }

    public static int CreateOrder(int customerId, string address, IReadOnlyCollection<CartItem> items)
    {
        if (items.Count == 0) throw new InvalidOperationException("Корзина пуста.");

        using SqlConnection connection = OpenConnection();
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            int orderId;
            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.Orders (CustomerID, OrderDate, OrderStatus, DeliveryAddress)
                                        VALUES (@CustomerID, SYSDATETIME(), N'Новый', @Address);
                                        SELECT CAST(SCOPE_IDENTITY() AS int);";
                command.Parameters.AddWithValue("@CustomerID", customerId);
                command.Parameters.AddWithValue("@Address", address.Trim());
                orderId = Convert.ToInt32(command.ExecuteScalar());
            }

            foreach (CartItem item in items)
            {
                using (SqlCommand stockCommand = connection.CreateCommand())
                {
                    stockCommand.Transaction = transaction;
                    stockCommand.CommandText = @"UPDATE dbo.Products SET StockQuantity = StockQuantity - @Quantity
                                                 WHERE ProductID = @ProductID AND IsActive = 1 AND StockQuantity >= @Quantity;";
                    stockCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                    stockCommand.Parameters.AddWithValue("@ProductID", item.Product.ProductID);
                    if (stockCommand.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException($"Недостаточно товара «{item.Product.ProductName}» на складе. Обновите каталог.");
                }

                using (SqlCommand itemCommand = connection.CreateCommand())
                {
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = @"INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice)
                                                VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice);";
                    itemCommand.Parameters.AddWithValue("@OrderID", orderId);
                    itemCommand.Parameters.AddWithValue("@ProductID", item.Product.ProductID);
                    itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("@UnitPrice", item.Product.Price);
                    itemCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static List<OrderView> GetOrders(AppUser user)
    {
        var orders = new List<OrderView>();
        bool allOrders = Roles.CanManageOrders(user.RoleName);
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = @"SELECT o.OrderID, o.CustomerID, c.FullName, o.OrderDate, o.OrderStatus,
                                       o.DeliveryAddress, COALESCE(SUM(oi.Quantity * oi.UnitPrice), 0) AS Total
                                FROM dbo.Orders AS o
                                INNER JOIN dbo.Customers AS c ON c.CustomerID = o.CustomerID
                                LEFT JOIN dbo.OrderItems AS oi ON oi.OrderID = o.OrderID
                                WHERE (@AllOrders = 1 OR o.CustomerID = @CustomerID)
                                GROUP BY o.OrderID, o.CustomerID, c.FullName, o.OrderDate, o.OrderStatus, o.DeliveryAddress
                                ORDER BY o.OrderDate DESC;";
        command.Parameters.Add("@AllOrders", SqlDbType.Bit).Value = allOrders;
        command.Parameters.Add("@CustomerID", SqlDbType.Int).Value = user.CustomerId ?? -1;
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(new OrderView
            {
                OrderID = reader.GetInt32(0), CustomerID = reader.GetInt32(1), CustomerName = reader.GetString(2),
                OrderDate = reader.GetDateTime(3), OrderStatus = reader.GetString(4), DeliveryAddress = reader.GetString(5),
                Total = reader.GetDecimal(6)
            });
        }
        return orders;
    }

    public static void UpdateOrderStatus(int orderId, string status)
    {
        if (!OrderStatuses.Contains(status)) throw new ArgumentException("Недопустимый статус заказа.");
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Orders SET OrderStatus = @Status WHERE OrderID = @OrderID;";
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@OrderID", orderId);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Заказ не найден.");
    }

    public static void DeleteOrder(int orderId)
    {
        using SqlConnection connection = OpenConnection();
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            using (SqlCommand items = connection.CreateCommand())
            {
                items.Transaction = transaction;
                items.CommandText = "DELETE FROM dbo.OrderItems WHERE OrderID = @OrderID;";
                items.Parameters.AddWithValue("@OrderID", orderId);
                items.ExecuteNonQuery();
            }
            using (SqlCommand order = connection.CreateCommand())
            {
                order.Transaction = transaction;
                order.CommandText = "DELETE FROM dbo.Orders WHERE OrderID = @OrderID;";
                order.Parameters.AddWithValue("@OrderID", orderId);
                if (order.ExecuteNonQuery() != 1) throw new InvalidOperationException("Заказ не найден.");
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void SaveProduct(Product product)
    {
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        if (product.ProductID == 0)
        {
            command.CommandText = @"INSERT INTO dbo.Products (CategoryID, ProductName, SKU, Size, Color, Price, StockQuantity, IsActive)
                                    VALUES (@CategoryID, @ProductName, @SKU, @Size, @Color, @Price, @StockQuantity, 1);";
        }
        else
        {
            command.CommandText = @"UPDATE dbo.Products
                                    SET CategoryID=@CategoryID, ProductName=@ProductName, SKU=@SKU, Size=@Size,
                                        Color=@Color, Price=@Price, StockQuantity=@StockQuantity
                                    WHERE ProductID=@ProductID;";
            command.Parameters.AddWithValue("@ProductID", product.ProductID);
        }
        command.Parameters.AddWithValue("@CategoryID", product.CategoryID);
        command.Parameters.AddWithValue("@ProductName", product.ProductName.Trim());
        command.Parameters.AddWithValue("@SKU", product.SKU.Trim());
        command.Parameters.AddWithValue("@Size", product.Size.Trim());
        command.Parameters.AddWithValue("@Color", product.Color.Trim());
        command.Parameters.AddWithValue("@Price", product.Price);
        command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
        int affected = command.ExecuteNonQuery();
        if (product.ProductID != 0 && affected == 0) throw new InvalidOperationException("Товар не найден.");
    }

    public static void DeleteProduct(int productId)
    {
        using SqlConnection connection = OpenConnection();
        using SqlCommand command = connection.CreateCommand();
        // Soft delete preserves products referenced by historical order items.
        command.CommandText = "UPDATE dbo.Products SET IsActive = 0 WHERE ProductID = @ProductID;";
        command.Parameters.AddWithValue("@ProductID", productId);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Товар не найден.");
    }
}
