using ClothingStoreWpf.Models;
using ClothingStoreWpf.Services;
using System.Windows;

namespace ClothingStoreWpf.Views;

public partial class RegisterWindow : Window
{
    public AppUser? RegisteredUser { get; private set; }

    public RegisterWindow() => InitializeComponent();

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        string fullName = FullNameBox.Text.Trim();
        string email = EmailBox.Text.Trim();
        string city = CityBox.Text.Trim();
        string login = LoginBox.Text.Trim();
        string password = PasswordInput.Password;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Заполните все обязательные поля, отмеченные звёздочкой.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
        {
            MessageBox.Show("Введите корректный адрес электронной почты.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (login.Length < 3 || login.Length > 50)
        {
            MessageBox.Show("Логин должен содержать от 3 до 50 символов.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (password.Length < 8)
        {
            MessageBox.Show("Пароль должен содержать не менее 8 символов.", "Проверка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (password != ConfirmPasswordInput.Password)
        {
            MessageBox.Show("Пароли не совпадают.", "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            ConfirmPasswordInput.Clear();
            return;
        }

        try
        {
            RegisteredUser = Database.RegisterCustomer(fullName, email, PhoneBox.Text, city, login, password);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось выполнить регистрацию. Проверьте, не заняты ли логин или электронная почта.\n\n" + ex.Message,
                "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
