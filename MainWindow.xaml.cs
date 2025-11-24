using dns.DB;
using dns.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static dns.Profile;

namespace dns
{
    public partial class MainWindow : Window
    {
        private bool _isRegisterMode = false;
        private MyDbContext _db;

        public MainWindow()
        {
            InitializeComponent();
            _db = new MyDbContext(new DbContextOptions<MyDbContext>());
            SwitchMode(false);
        }

        private void ToggleLink_Click(object sender, RoutedEventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;
            SwitchMode(_isRegisterMode);
        }

        private void SwitchMode(bool isRegister)
        {
            _isRegisterMode = isRegister;

            if (isRegister)
            {
                HeaderTitle.Text = "Создайте аккаунт";
                HeaderSubtitle.Text = "Быстрая регистрация — 1 минута";
                SubmitButton.Content = "Зарегистрироваться";
                TogglePrefix.Text = "Уже есть аккаунт? ";
                ToggleLink.Inlines.Clear();
                ToggleLink.Inlines.Add("Войти");

                FullNamePanel.Visibility = Visibility.Visible;
                ConfirmPanel.Visibility = Visibility.Visible;
            }
            else
            {
                HeaderTitle.Text = "Вход в аккаунт";
                HeaderSubtitle.Text = "Введите email или логин и пароль";
                SubmitButton.Content = "Войти";
                TogglePrefix.Text = "Нет аккаунта? ";
                ToggleLink.Inlines.Clear();
                ToggleLink.Inlines.Add("Зарегистрироваться");

                FullNamePanel.Visibility = Visibility.Collapsed;
                ConfirmPanel.Visibility = Visibility.Collapsed;

                FullNameBox.Text = "";
                ((PasswordBox)ConfirmPasswordBox).Password = "";
            }

            ClearErrors();
        }

        private void ClearErrors()
        {
            FullNameError.Visibility = Visibility.Collapsed;
            EmailError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;
            ConfirmError.Visibility = Visibility.Collapsed;

            ResetBorder(FullNameBox);
            ResetBorder(EmailBox);
            ResetBorder(PasswordBox);
            ResetBorder((PasswordBox)ConfirmPasswordBox);
        }

        private void ResetBorder(Control control)
        {
            var border = GetTemplateChild(control, "border") as Border;
            if (border != null)
                border.BorderBrush = (Brush)Application.Current.Resources["BorderBrush"];
        }

        private void SetErrorBorder(Control control)
        {
            var border = GetTemplateChild(control, "border") as Border;
            if (border != null)
                border.BorderBrush = (Brush)Application.Current.Resources["DangerBrush"];
        }

        private object GetTemplateChild(Control control, string childName)
        {
            control.ApplyTemplate();
            return control.Template.FindName(childName, control);
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();
            bool valid = true;

            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ((PasswordBox)ConfirmPasswordBox).Password;
            string login = _isRegisterMode ? FullNameBox.Text.Trim() : email;

            // Валидация Email
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError(EmailError, EmailBox, "Введите email");
                valid = false;
            }
            else if (!IsValidEmail(email))
            {
                ShowError(EmailError, EmailBox, "Некорректный формат email");
                valid = false;
            }

            // Валидация пароля
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError(PasswordError, PasswordBox, "Введите пароль");
                valid = false;
            }
            else if (password.Length < 6)
            {
                ShowError(PasswordError, PasswordBox, "Пароль должен быть не менее 6 символов");
                valid = false;
            }
            // регистрация
            if (_isRegisterMode)
            {
                if (string.IsNullOrWhiteSpace(login))
                {
                    ShowError(FullNameError, FullNameBox, "Введите логин");
                    valid = false;
                }
                else if (login.Length < 3)
                {
                    ShowError(FullNameError, FullNameBox, "Логин должен быть не менее 3 символов");
                    valid = false;
                }
                else if (login.Contains(" "))
                {
                    ShowError(FullNameError, FullNameBox, "Логин не может содержать пробелы");
                    valid = false;
                }
                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ShowError(ConfirmError, (PasswordBox)ConfirmPasswordBox, "Повторите пароль");
                    valid = false;
                }
                else if (password != confirmPassword)
                {
                    ShowError(ConfirmError, (PasswordBox)ConfirmPasswordBox, "Пароли не совпадают");
                    valid = false;
                }

                if (!valid) return;

                try
                {
                    bool userExists = _db.User.Any(u => u.Login == login || u.Mail == email);
                    if (userExists)
                    {
                        var existingLogin = _db.User.Any(u => u.Login == login);
                        var existingEmail = _db.User.Any(u => u.Mail == email);

                        if (existingLogin && existingEmail)
                            MessageBox.Show("Пользователь с таким логином и email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else if (existingLogin)
                            ShowError(FullNameError, FullNameBox, "Этот логин уже занят");
                        else
                            ShowError(EmailError, EmailBox, "Этот email уже зарегистрирован");

                        return;
                    }

                    var newUser = new User
                    {
                        Login = login,
                        Mail = email,
                        Password = password,
                        RoleId = 2,
                        CreatedAt = DateOnly.FromDateTime(DateTime.Today),
                        Phone = null,    
                        Address = null
                    };

                    _db.User.Add(newUser);
                    _db.SaveChanges();

                    MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                        
                    _isRegisterMode = false;
                    SwitchMode(false);

                    FullNameBox.Text = "";
                    PasswordBox.Password = "";
                    ((PasswordBox)ConfirmPasswordBox).Password = "";

                    EmailBox.Text = email;

                    PasswordBox.Focus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // вход
                if (!valid) return;

                try
                {
                    var user = _db.User.FirstOrDefault(u =>
                        (u.Login == login || u.Mail == login) && u.Password == password);

                    if (user == null)
                    {
                        ShowError(PasswordError, PasswordBox, "Неверный логин, email или пароль");
                        return;
                    }

                    CurrentUser.UserId = user.UsersId;
                    CurrentUser.Login = user.Login;
                    CurrentUser.Email = user.Mail;
                    CurrentUser.Address = user.Address;

                    this.Hide();

                    var catalog = new Catalog();
                    catalog.Closed += (s, ev) => this.Close();
                    catalog.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowError(TextBlock errorLabel, Control input, string message)
        {
            errorLabel.Text = message;
            errorLabel.Visibility = Visibility.Visible;
            SetErrorBorder(input);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _db?.Dispose();
            base.OnClosed(e);
        }
    }
}