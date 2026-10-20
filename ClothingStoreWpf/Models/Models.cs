namespace ClothingStoreWpf.Models;

public static class Roles
{
    public const string Admin = "Администратор";
    public const string Manager = "Менеджер";
    public const string User = "Пользователь";

    public static bool CanManageOrders(string role) => role is Admin or Manager;
}

public sealed class AppUser
{
    public int AppUserId { get; init; }
    public int? CustomerId { get; init; }
    public string Login { get; init; } = "";
    public string RoleName { get; init; } = Roles.User;
    public bool IsAdmin => RoleName == Roles.Admin;
    public bool IsManager => RoleName == Roles.Manager;
    public bool IsCustomer => RoleName == Roles.User && CustomerId.HasValue;
}

public sealed class Product
{
    public int ProductID { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string SKU { get; set; } = "";
    public string Size { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CartItem
{
    public Product Product { get; init; } = new();
    public int Quantity { get; set; }
    public string ProductName => Product.ProductName;
    public decimal Price => Product.Price;
    public decimal LineTotal => Price * Quantity;
}

public sealed class OrderView
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public decimal Total { get; set; }
}

public sealed class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
}
