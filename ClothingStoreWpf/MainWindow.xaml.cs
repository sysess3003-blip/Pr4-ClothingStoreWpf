using ClothingStoreWpf.Models;
using ClothingStoreWpf.Services;
using ClothingStoreWpf.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace ClothingStoreWpf;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<CartItem> _cart = [];
    private AppUser? _currentUser;
    private bool _databaseReady;

    public MainWindow()
    {
        InitializeComponent();
        CartGrid.ItemsSource = _cart;
        Loaded += (_, _) => InitializeApplication();
    }

    private void InitializeApplication()
    {
        try
        {
            Database.Initialize();
            _databaseReady = true;
            LoadProducts();
            UpdateAccessControls();
        }
        catch (Exception ex)
        {
            _databaseReady = false;
            UpdateAccessControls();
            MessageBox.Show(
                "Не удалось подключиться к базе данных. Убедитесь, что SQL Server запущен, " +
                "а скрипт лабораторной работы № 3 выполнен.\n\n" + ex.Message,
                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProducts()
    {
        ProductsGrid.ItemsSource = Database.GetProducts();
    }

    private void UpdateAccessControls()
    {
        UserLabel.Text = _currentUser is null ? "Гость · просмотр каталога" : $"{_currentUser.Login} · {_currentUser.RoleName}";
        LoginButton.Visibility = _currentUser is null ? Visibility.Visible : Visibility.Collapsed;
        RegisterButton.Visibility = _currentUser is null ? Visibility.Visible : Visibility.Collapsed;
        LogoutButton.Visibility = _currentUser is null ? Visibility.Collapsed : Visibility.Visible;
        OrdersButton.IsEnabled = _databaseReady && _currentUser is not null;
        ManageProductsButton.Visibility = _databaseReady && _currentUser?.IsAdmin == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (!_databaseReady) return;
        var window = new LoginWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            _currentUser = window.AuthenticatedUser;
            UpdateAccessControls();
        }
    }

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        if (!_databaseReady) return;
        var window = new RegisterWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            _currentUser = window.RegisteredUser;
            UpdateAccessControls();
            MessageBox.Show("Регистрация выполнена. Теперь можно оформлять заказы.", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _currentUser = null;
        _cart.Clear();
        UpdateCartTotal();
        UpdateAccessControls();
    }

    private void Orders_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null) return;
        new OrdersWindow(_currentUser) { Owner = this }.ShowDialog();
        LoadProducts();
    }

    private void ManageProducts_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.IsAdmin != true) return;
        new ProductManagementWindow { Owner = this }.ShowDialog();
        LoadProducts();
    }

    private void RefreshProducts_Click(object sender, RoutedEventArgs e)
    {
        try { LoadProducts(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ProductsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Product selection is handled by AddToCart_Click.
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
        {
            MessageBox.Show("Сначала выберите товар в каталоге.", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество должно быть целым числом больше нуля.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (quantity > product.StockQuantity)
        {
            MessageBox.Show("Указанное количество превышает остаток на складе.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CartItem? existing = _cart.FirstOrDefault(item => item.Product.ProductID == product.ProductID);
        if (existing is not null)
        {
            if (existing.Quantity + quantity > product.StockQuantity)
            {
                MessageBox.Show("В корзине уже есть этот товар. Общее количество превышает складской остаток.",
                    "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existing.Quantity += quantity;
        }
        else
        {
            _cart.Add(new CartItem { Product = product, Quantity = quantity });
        }
        CartGrid.Items.Refresh();
        UpdateCartTotal();
    }

    private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is CartItem selected) _cart.Remove(selected);
        UpdateCartTotal();
    }

    private void PlaceOrder_Click(object sender, RoutedEventArgs e)
    {
        if (!_databaseReady) return;
        if (_currentUser is null)
        {
            MessageBox.Show("Для оформления заказа войдите в приложение или зарегистрируйтесь.",
                "Требуется авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!_currentUser.CustomerId.HasValue)
        {
            MessageBox.Show("Оформление заказа доступно учётной записи покупателя. Войдите под обычной учётной записью.",
                "Заказ", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_cart.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы один товар в корзину.", "Корзина пуста", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
        {
            MessageBox.Show("Введите адрес доставки.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            AddressTextBox.Focus();
            return;
        }

        try
        {
            int orderId = Database.CreateOrder(_currentUser.CustomerId.Value, AddressTextBox.Text, _cart.ToList());
            _cart.Clear();
            UpdateCartTotal();
            LoadProducts();
            MessageBox.Show($"Заказ № {orderId} оформлен. Его статус можно посмотреть в разделе «Заказы».",
                "Заказ создан", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void UpdateCartTotal()
    {
        TotalLabel.Text = $"Итого: {_cart.Sum(item => item.LineTotal):N2} ₽";
    }

    private static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "Ошибка операции",
        MessageBoxButton.OK, MessageBoxImage.Error);
}
