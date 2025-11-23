using dns.DB;
using dns.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace dns
{
    public partial class CartWindow : Window
    {
        private Order? _cartOrder;
        private readonly int CurrentUserId = 1;

        public CartWindow()
        {
            InitializeComponent();
            Loaded += CartWindow_Loaded;
            Activated += CartWindow_Activated;
        }

        private async void CartWindow_Activated(object sender, EventArgs e) => await LoadCartAsync();
        private async void CartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCartAsync();
            Loaded -= CartWindow_Loaded;
        }

        private async Task LoadCartAsync()
        {
            await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());

            _cartOrder = await db.Order
                .Include(o => o.OrderProduct)
                    .ThenInclude(op => op.Product)
                        .ThenInclude(p => p.Category)
                .AsSplitQuery() // Важно! Улучшает производительность
                .FirstOrDefaultAsync(o => o.UsersId == CurrentUserId && o.OrderStatusId == 1);

            if (_cartOrder == null)
            {
                var newOrder = new Order
                {
                    UsersId = CurrentUserId,
                    OrderStatusId = 1,
                    OrderDate = DateOnly.FromDateTime(DateTime.Today),
                    Address = ""
                };
                db.Order.Add(newOrder);
                await db.SaveChangesAsync();
                _cartOrder = newOrder;
            }

            UpdateCartDisplay();
            UpdateTotals();
        }

        private void UpdateCartDisplay()
        {
            CartItemsStackPanel.Children.Clear();

            if (_cartOrder?.OrderProduct == null || !_cartOrder.OrderProduct.Any())
            {
                CartItemsStackPanel.Children.Add(new TextBlock
                {
                    Text = "Корзина пуста",
                    FontSize = 24,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 120, 0, 0)
                });
                CartTitleTextBlock.Text = "Корзина (0 товаров)";
                return;
            }

            int totalItems = _cartOrder.OrderProduct?.Sum(op => op.Quantity ?? 0) ?? 0;
            CartTitleTextBlock.Text = $"Корзина ({totalItems} {GetDeclension(totalItems)})";

            foreach (var op in _cartOrder.OrderProduct)
            {
                CartItemsStackPanel.Children.Add(CreateCartRow(op));
            }
        }

        private Border CreateCartRow(OrderProduct orderProduct)
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(20),
                Background = (SolidColorBrush)FindResource("BgBrush"),
                CornerRadius = new CornerRadius(12),
                Effect = (DropShadowEffect)FindResource("CardShadowEffect")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Название
            var nameBlock = new TextBlock
            {
                Text = orderProduct.Product.Name,
                FontSize = 16,
                FontWeight = FontWeights.Medium,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 0);
            grid.Children.Add(nameBlock);

            // Цена
            var priceBlock = new TextBlock
            {
                Text = $"{orderProduct.Product.Price:N0} ₽",
                FontSize = 15,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(30, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(priceBlock, 1);
            grid.Children.Add(priceBlock);

            // Количество
            var qtyPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var minusBtn = new Button { Content = "-", Width = 36, Height = 36, Style = (Style)FindResource("QtyButtonStyle") };
            var qtyBox = new TextBox
            {
                Text = orderProduct.Quantity.ToString(),
                Width = 60,
                Height = 36,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                Style = (Style)FindResource("QtyInputStyle"),
                Tag = orderProduct
            };
            var plusBtn = new Button { Content = "+", Width = 36, Height = 36, Style = (Style)FindResource("QtyButtonStyle") };

            minusBtn.Click += async (_, _) => await ChangeQuantityAsync(orderProduct, -1, qtyBox);
            plusBtn.Click += async (_, _) => await ChangeQuantityAsync(orderProduct, +1, qtyBox);
            qtyBox.LostFocus += async (_, _) => await ValidateQuantityFromInputAsync(qtyBox);

            qtyPanel.Children.Add(minusBtn);
            qtyPanel.Children.Add(qtyBox);
            qtyPanel.Children.Add(plusBtn);
            Grid.SetColumn(qtyPanel, 2);
            grid.Children.Add(qtyPanel);

            // Итого
            var totalBlock = new TextBlock
            {
                Text = $"{orderProduct.Product.Price * orderProduct.Quantity:N0} ₽",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = qtyBox // для обновления
            };
            Grid.SetColumn(totalBlock, 3);
            grid.Children.Add(totalBlock);

            // Удалить
            var removeBtn = new Button
            {
                Content = "x",
                Style = (Style)FindResource("RemoveButtonStyle"),
                Width = 36,
                Height = 36,
                Margin = new Thickness(30, 0, 0, 0)
            };
            removeBtn.Click += async (_, _) => await RemoveFromCartAsync(orderProduct);
            Grid.SetColumn(removeBtn, 4);
            grid.Children.Add(removeBtn);

            border.Child = grid;
            return border;
        }

        // Главное исправление — работаем через отдельный контекст БЕЗ трекинга
        private async Task ChangeQuantityAsync(OrderProduct orderProduct, int delta, TextBox qtyBox)
        {
            int? newQty = orderProduct.Quantity + delta;
            if (newQty < 1)
            {
                await RemoveFromCartAsync(orderProduct);
                return;
            }

            await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());

            // Получаем актуальное количество на складе БЕЗ трекинга
            var availableAmount = await db.Product
                .AsNoTracking()
                .Where(p => p.ProductId == orderProduct.ProductId)
                .Select(p => p.Amount)
                .FirstOrDefaultAsync();

            if (newQty > availableAmount)
            {
                MessageBox.Show($"На складе только {availableAmount} шт.", "Недостаточно товара", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Обновляем только OrderProduct — без касания Product
            db.OrderProduct.Update(orderProduct);
            orderProduct.Quantity = newQty;
            await db.SaveChangesAsync();

            qtyBox.Text = newQty.ToString();
            UpdateLineTotal(orderProduct, qtyBox);
            UpdateTotals();
        }

        private async Task ValidateQuantityFromInputAsync(TextBox qtyBox)
        {
            if (qtyBox.Tag is not OrderProduct op) return;

            if (!int.TryParse(qtyBox.Text, out int newQty) || newQty < 1)
            {
                qtyBox.Text = op.Quantity.ToString();
                return;
            }

            await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());

            var available = await db.Product
                .AsNoTracking()
                .Where(p => p.ProductId == op.ProductId)
                .Select(p => p.Amount)
                .FirstOrDefaultAsync();

            if (newQty > available)
            {
                MessageBox.Show($"На складе только {available} шт.", "Недостаточно", MessageBoxButton.OK, MessageBoxImage.Warning);
                qtyBox.Text = op.Quantity.ToString();
                return;
            }

            op.Quantity = newQty;
            db.OrderProduct.Update(op);
            await db.SaveChangesAsync();

            UpdateLineTotal(op, qtyBox);
            UpdateTotals();
        }

        private async Task RemoveFromCartAsync(OrderProduct orderProduct)
        {
            if (MessageBox.Show("Удалить товар из корзины?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());
                db.OrderProduct.Remove(orderProduct);
                await db.SaveChangesAsync();
                await LoadCartAsync(); // перезагружаем корзину
            }
        }

        private void UpdateLineTotal(OrderProduct op, TextBox qtyBox)
        {
            var grid = (qtyBox.Parent as Panel)?.Parent as Grid;
            var totalBlock = grid?.Children.OfType<TextBlock>().FirstOrDefault(t => Grid.GetColumn(t) == 3);
            totalBlock.Text = $"{op.Product.Price * op.Quantity:N0} ₽";
        }

        private void UpdateTotals()
        {
            if (_cartOrder == null) return;
            decimal? total = _cartOrder.OrderProduct.Sum(op => op.Product.Price * op.Quantity);
            SubtotalText.Text = $"{total:N0} ₽";
            FinalTotalText.Text = $"{total:N0} ₽";
        }

        private string GetDeclension(int n)
        {
            var last = n % 10;
            var lastTwo = n % 100;
            if (last == 1 && lastTwo != 11) return "товар";
            if (last >= 2 && last <= 4 && (lastTwo < 10 || lastTwo >= 20)) return "товара";
            return "товаров";
        }

        private void ContinueShopping_Click(object sender, RoutedEventArgs e) => Close();

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (_cartOrder == null || !_cartOrder.OrderProduct.Any())
            {
                MessageBox.Show("Корзина пуста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());

            // Проверка остатков
            foreach (var op in _cartOrder.OrderProduct)
            {
                var stock = await db.Product
                    .AsNoTracking()
                    .Where(p => p.ProductId == op.ProductId)
                    .Select(p => p.Amount)
                    .FirstOrDefaultAsync();

                if (op.Quantity > stock)
                {
                    MessageBox.Show($"Недостаточно товара: {op.Product.Name}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Списание со склада
            foreach (var op in _cartOrder.OrderProduct)
            {
                var product = await db.Product.FindAsync(op.ProductId);
                if (product != null)
                {
                    product.Amount -= op.Quantity.Value;
                }
            }

            _cartOrder.OrderStatusId = 2;
            _cartOrder.OrderDate = DateOnly.FromDateTime(DateTime.Today);

            await db.SaveChangesAsync();

            MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}