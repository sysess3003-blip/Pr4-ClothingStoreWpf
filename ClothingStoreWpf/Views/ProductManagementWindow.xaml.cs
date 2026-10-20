using ClothingStoreWpf.Models;
using ClothingStoreWpf.Services;
using System.Globalization;
using System.Windows;

namespace ClothingStoreWpf.Views;

public partial class ProductManagementWindow : Window
{
    private int _selectedProductId;

    public ProductManagementWindow()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = Database.GetCategories();
        LoadProducts();
        NewProductForm();
    }

    private void LoadProducts()
    {
        ProductsGrid.ItemsSource = Database.GetProducts(includeInactive: true);
    }

    private void ProductsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product) return;
        _selectedProductId = product.ProductID;
        ProductNameBox.Text = product.ProductName;
        CategoryBox.SelectedValue = product.CategoryID;
        SkuBox.Text = product.SKU;
        SizeBox.Text = product.Size;
        ColorBox.Text = product.Color;
        PriceBox.Text = product.Price.ToString("0.00", CultureInfo.CurrentCulture);
        StockBox.Text = product.StockQuantity.ToString(CultureInfo.CurrentCulture);
    }

    private void New_Click(object sender, RoutedEventArgs e) => NewProductForm();

    private void NewProductForm()
    {
        _selectedProductId = 0;
        ProductsGrid.SelectedItem = null;
        ProductNameBox.Clear();
        SkuBox.Clear();
        SizeBox.Clear();
        ColorBox.Clear();
        PriceBox.Clear();
        StockBox.Text = "0";
        CategoryBox.SelectedIndex = CategoryBox.Items.Count > 0 ? 0 : -1;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProductNameBox.Text) || string.IsNullOrWhiteSpace(SkuBox.Text) ||
            string.IsNullOrWhiteSpace(SizeBox.Text) || string.IsNullOrWhiteSpace(ColorBox.Text) || CategoryBox.SelectedItem is not Category category)
        {
            MessageBox.Show("Заполните название, категорию, артикул, размер и цвет.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price) &&
            !decimal.TryParse(PriceBox.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
        {
            MessageBox.Show("Цена должна быть числом.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (price <= 0)
        {
            MessageBox.Show("Цена должна быть больше нуля.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(StockBox.Text, out int stock) || stock < 0)
        {
            MessageBox.Show("Остаток должен быть целым неотрицательным числом.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Database.SaveProduct(new Product
            {
                ProductID = _selectedProductId, CategoryID = category.CategoryID, CategoryName = category.CategoryName,
                ProductName = ProductNameBox.Text.Trim(), SKU = SkuBox.Text.Trim(), Size = SizeBox.Text.Trim(),
                Color = ColorBox.Text.Trim(), Price = price, StockQuantity = stock, IsActive = true
            });
            LoadProducts();
            NewProductForm();
            MessageBox.Show("Карточка товара сохранена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось сохранить товар. Проверьте уникальность артикула и корректность данных.\n\n" + ex.Message,
                "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
        {
            MessageBox.Show("Выберите товар в таблице.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!product.IsActive)
        {
            MessageBox.Show("Этот товар уже скрыт из каталога.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Скрыть товар «{product.ProductName}» из каталога? История заказов сохранится.",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            Database.DeleteProduct(product.ProductID);
            LoadProducts();
            NewProductForm();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
