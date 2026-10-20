using ClothingStoreWpf.Models;
using ClothingStoreWpf.Services;
using System.Windows;

namespace ClothingStoreWpf.Views;

public partial class LoginWindow : Window
{
    public AppUser? AuthenticatedUser { get; private set; }

    public LoginWindow() => InitializeComponent();

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginTextBox.Text) || string.IsNullOrEmpty(PasswordInput.Password))
        {
            MessageBox.Show("Введите логин и пароль.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            AuthenticatedUser = Database.Authenticate(LoginTextBox.Text, PasswordInput.Password);
            if (AuthenticatedUser is null)
            {
                MessageBox.Show("Неверный логин или пароль либо учётная запись отключена.",
                    "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordInput.Clear();
                return;
            }
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
