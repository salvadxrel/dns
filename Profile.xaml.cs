// Profile.xaml.cs
using dns.DB;
using dns.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;

namespace dns
{
    public partial class Profile : Window
    {
        private readonly MyDbContext _db;
        private User? _currentUser;

        // Конструктор теперь принимает DbContext от Catalog
        public Profile(MyDbContext dbContext)
        {
            InitializeComponent();
            _db = dbContext; // используем уже существующий контекст

            Loaded += Profile_Loaded;
        }

        private async void Profile_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.UserId == null)
            {
                MessageBox.Show("Сессия истекла. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReturnToLogin();
                return;
            }

            try
            {
                _currentUser = await _db.User
                    .FirstOrDefaultAsync(u => u.UsersId == CurrentUser.UserId.Value);

                if (_currentUser == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ReturnToLogin();
                    return;
                }

                LoginTextBox.Text = _currentUser.Login;
                EmailTextBox.Text = _currentUser.Mail;
                PasswordBox.Password = ""; // не показываем старый пароль
                PasswordBox.Tag = "Оставьте пустым, чтобы не менять пароль";
                AddressTextBox.Text = string.IsNullOrWhiteSpace(_currentUser.Address)
                    ? "Не указан"
                    : _currentUser.Address;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;

            try
            {
                _currentUser.Login = LoginTextBox.Text.Trim();
                _currentUser.Mail = EmailTextBox.Text.Trim();

                // Обновляем адрес
                string address = AddressTextBox.Text.Trim();
                _currentUser.Address = (address == "" || address == "Не указан") ? null : address;

                // Обновляем пароль только если поле не пустое
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    _currentUser.Password = PasswordBox.Password; // потом замени на хеш!
                }

                _db.Update(_currentUser);
                _db.SaveChanges();

                // Сохранили в БД

                // Обновляем сессию
                CurrentUser.Login = _currentUser.Login;
                CurrentUser.Email = _currentUser.Mail;
                CurrentUser.Address = _currentUser.Address;

                MessageBox.Show("Профиль успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // просто закрываем — Catalog жив и контекст живой
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelProfile_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем поля до текущих значений из БД
            if (_currentUser != null)
            {
                LoginTextBox.Text = _currentUser.Login;
                EmailTextBox.Text = _currentUser.Mail;
                PasswordBox.Password = "";
                AddressTextBox.Text = string.IsNullOrWhiteSpace(_currentUser.Address) ? "Не указан" : _currentUser.Address;
            }
        }

        private void BackToCatalog_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Catalog остаётся живым
        }

        private void ReturnToLogin()
        {
            CurrentUser.Clear();
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        // УБРАЛИ Dispose() — контекст принадлежит Catalog, он сам его закроет!
        // protected override void OnClosed(...) — УДАЛИ ЭТОТ МЕТОД ИЛИ ОСТАВЬ БЕЗ _db.Dispose()
    }
public static class CurrentUser
        {
            public static int? UserId { get; set; }
            public static string? Login { get; set; }
            public static string? Email { get; set; }
            public static string? Address { get; set; } // будет обновляться

            public static void Clear()
            {
                UserId = null;
                Login = null;
                Email = null;
                Address = null;
            }
        }
    }