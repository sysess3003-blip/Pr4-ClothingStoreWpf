using ClothingStoreWpf.Models;
using ClothingStoreWpf.Services;
using System.Windows;

namespace ClothingStoreWpf.Views;

public partial class OrdersWindow : Window
{
    private readonly AppUser _user;

    public OrdersWindow(AppUser user)
    {
        InitializeComponent();
        _user = user;
        StatusComboBox.ItemsSource = Database.OrderStatuses;
        StatusComboBox.SelectedIndex = 0;
        bool canManage = Roles.CanManageOrders(user.RoleName);
        UpdateStatusButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        DeleteOrderButton.Visibility = user.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        StatusComboBox.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        if (!canManage)
        {
            AccessHint.Text = "Показаны только ваши заказы. Изменение и удаление недоступны.";
            ManagePanel.Visibility = Visibility.Collapsed;
        }
        else if (user.IsAdmin)
        {
            AccessHint.Text = "Администратор: просмотр всех заказов, изменение статуса и удаление.";
        }
        else
        {
            AccessHint.Text = "Менеджер: просмотр всех заказов и изменение статуса.";
        }
        LoadOrders();
    }

    private void LoadOrders() => OrdersGrid.ItemsSource = Database.GetOrders(_user);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try { LoadOrders(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void UpdateStatus_Click(object sender, RoutedEventArgs e)
    {
        if (!Roles.CanManageOrders(_user.RoleName)) return;
        if (OrdersGrid.SelectedItem is not OrderView order)
        {
            MessageBox.Show("Выберите заказ.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (StatusComboBox.SelectedItem is not string status) return;
        try
        {
            Database.UpdateOrderStatus(order.OrderID, status);
            LoadOrders();
            MessageBox.Show("Статус заказа обновлён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (!_user.IsAdmin) return;
        if (OrdersGrid.SelectedItem is not OrderView order)
        {
            MessageBox.Show("Выберите заказ.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBoxResult answer = MessageBox.Show($"Удалить заказ № {order.OrderID} и его позиции?",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            Database.DeleteOrder(order.OrderID);
            LoadOrders();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
