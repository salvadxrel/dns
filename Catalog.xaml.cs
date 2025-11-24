using dns.DB;
using dns.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dns
{
    public partial class Catalog : Window
    {
        private MyDbContext _dbContext;
        private List<Product> allProducts;
        private List<Product> filteredProducts;
        private bool _isLoading = true;
        // === Пагинация ===
        private const int PageSize = 12; // 4 товара в ряду × 3 ряда
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int CurrentUserId => 1; // Замени на реальный ID после авторизации
        public Catalog()
        {
            InitializeComponent();
            _dbContext = new MyDbContext(new DbContextOptions<MyDbContext>());
            Loaded += Catalog_Loaded;
        }
        private async void Catalog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductsFromDatabase();
            InitializeFilters();
            _isLoading = false;
            ApplyFiltersAndSortAndPaginate();
            Loaded -= Catalog_Loaded;
        }
        private async Task LoadProductsFromDatabase()
        {
            try
            {
                allProducts = await _dbContext.Product
                    .Include(p => p.Category)
                    .Include(p => p.Comment)
                    .ToListAsync();
                filteredProducts = new List<Product>(allProducts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                allProducts = new List<Product>();
                filteredProducts = new List<Product>();
            }
        }
        private void InitializeFilters()
        {
            if (AllCategory != null) AllCategory.IsChecked = true;
            UpdateCategoryCounts();
        }
        private void UpdateCategoryCounts()
        {
            var categories = _dbContext.Category.ToList();
            foreach (var cat in categories)
            {
                var checkbox = FindCategoryCheckBoxByName(cat.Name);
                if (checkbox != null && checkbox.Content is string content)
                {
                    int countInCategory = filteredProducts.All(p => true) 
                        ? filteredProducts.Count(p => p.CategoryId == cat.CategoryId)
                        : allProducts.Count(p => p.CategoryId == cat.CategoryId);
                    var parts = content.Split('(');
                    string categoryName = parts[0].Trim();
                    checkbox.Content = $"{categoryName} ({countInCategory})";
                }
            }
        }
        private CheckBox FindCategoryCheckBoxByName(string categoryName)
        {
            return categoryName switch
            {
                "Видеокарты" => VideoCardsCategory,
                "Процессоры" => ProcessorsCategory,
                "Оперативная память" => RAMCategory,
                "Накопители" => StorageCategory,
                "Материнские платы" => MotherboardsCategory,
                "Блоки питания" => PSUCategory,
                "Корпуса" => CasesCategory,
                _ => null
            };
        }
        private void UpdateProductsGrid()
        {
            ProductsGrid.Children.Clear();
            if (filteredProducts == null || !filteredProducts.Any())
            {
                var emptyText = new TextBlock
                {
                    Text = "Товары не найдены",
                    FontSize = 20,
                    Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                ProductsGrid.Children.Add(emptyText);
                FoundCountRun.Text = "0 товаров";
                UpdatePaginationControls();
                return;
            }
            var pageProducts = filteredProducts
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            foreach (var product in pageProducts)
            {
                var card = CreateProductCard(product);
                ProductsGrid.Children.Add(card);
            }
            FoundCountRun.Text = $"{filteredProducts.Count} товаров";
            UpdatePaginationControls();
        }
        private void UpdatePaginationControls()
        {
            _totalPages = (int)Math.Ceiling((double)filteredProducts.Count / PageSize);
            _currentPage = Math.Max(1, Math.Min(_currentPage, _totalPages > 0 ? _totalPages : 1));
            PageInfoText.Text = _totalPages <= 1
                ? ""
                : $"Страница {_currentPage} из {_totalPages}";
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
        }
        private Border CreateProductCard(Product product)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ProductCardStyle"),
                Width = 280,
                Margin = new Thickness(0, 0, 24, 24)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(130) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // параметры картинки внутри карточки товара
            var imageContainer = new Border
            {
                Background = (SolidColorBrush)FindResource("CardBgBrush"),
                CornerRadius = new CornerRadius(16, 16, 0, 0),
                ClipToBounds = true,
                Height = 150,
                Padding = new Thickness(10)
            };

            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,   
                StretchDirection = StretchDirection.Both
            };

            var image = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            string imageFileName = GetImageFileNameForCategory(product.Category?.Name);

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri($"pack://application:,,,/Resources/Images/{imageFileName}");
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                image.Source = bitmap;
            }
            catch
            {
                var placeholder = new TextBlock
                {
                    Text = "No Image",
                    FontSize = 26,
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                viewbox.Child = placeholder;
            }

            if (image.Source != null)
            {
                image.RenderTransform = new ScaleTransform(1.05, 1.05);
                image.RenderTransformOrigin = new Point(0.5, 0.5);
                Width = 50;
            }

            viewbox.Child = image;
            viewbox.Margin = new Thickness(20);
            imageContainer.Child = viewbox;

            Grid.SetRow(imageContainer, 0);
            grid.Children.Add(imageContainer);

            // === Остальной контент (название, описание, цена, кнопки) ===
            var nameText = new TextBlock
            {
                Text = product.Name,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 12, 12, 8)
            };
            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            var descText = new TextBlock
            {
                Text = product.Description?.Length > 100
                    ? product.Description.Substring(0, 97) + "..."
                    : product.Description ?? "Нет описания",
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(12, 0, 12, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(descText, 2);
            grid.Children.Add(descText);

            var priceText = new TextBlock
            {
                Text = $"{product.Price:N0} ₽",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("AccentBrush"),
                Margin = new Thickness(12, 0, 12, 12)
            };
            Grid.SetRow(priceText, 3);
            grid.Children.Add(priceText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(12, 0, 12, 12)
            };

            var buyButton = new Button
            {
                Content = "Купить",
                Style = (Style)FindResource("PrimaryButtonStyle"),
                Margin = new Thickness(0, 0, 8, 0),
                Tag = product
            };
            buyButton.Click += BuyButton_Click;

            var reviewsButton = new Button
            {
                Content = "Отзывы",
                Style = (Style)FindResource("OutlineButtonStyle"),
                Tag = product
            };
            reviewsButton.Click += ReviewsButton_Click;

            buttonsPanel.Children.Add(buyButton);
            buttonsPanel.Children.Add(reviewsButton);
            Grid.SetRow(buttonsPanel, 4);
            grid.Children.Add(buttonsPanel);

            border.Child = grid;
            return border;
        }
        // Метод для получения имени файла изображения на основе категории
        private readonly Dictionary<string, string> CategoryToFile = new()
        {
            ["Процессоры"] = "cpu.png",
            ["Оперативная память"] = "ram.png",
            ["Накопители"] = "storage.png",
            ["Материнские платы"] = "motherboard.png",
            ["Блоки питания"] = "psu.png",
            ["Корпуса"] = "case.png",
            ["Видеокарты"] = "videocard.png"
        };
        private string GetImageFileNameForCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "default.png";

            categoryName = categoryName.Trim();

            if (CategoryToFile.TryGetValue(categoryName, out var file))
                return file;

            MessageBox.Show("Неизвестная категория: " + categoryName);

            return "default.png";
        }

        private void ReviewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                var reviewsWindow = new ReviewsWindow(product.ProductId);
                reviewsWindow.Owner = this;
                reviewsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                reviewsWindow.ShowDialog();
            }
        }
        private void ApplyFiltersAndSortAndPaginate()
        {
            if (_isLoading || allProducts == null) return;
            filteredProducts = allProducts.Where(p => MatchesFilters(p)).ToList();
            filteredProducts = ApplySorting(filteredProducts).ToList();
            _currentPage = 1; // Сброс на первую страницу при новых фильтрах
            UpdateProductsGrid();
            UpdateCategoryCounts();
        }
        private bool MatchesFilters(Product p)
        {
            bool allChecked = AllCategory?.IsChecked == true;
            bool categoryMatch = allChecked;
            if (!allChecked)
            {
                var selectedCategories = new[]
                {
                    (VideoCardsCategory?.IsChecked == true, "Видеокарты"),
                    (ProcessorsCategory?.IsChecked == true, "Процессоры"),
                    (RAMCategory?.IsChecked == true, "Оперативная память"),
                    (StorageCategory?.IsChecked == true, "Накопители"),
                    (MotherboardsCategory?.IsChecked == true, "Материнские платы"),
                    (PSUCategory?.IsChecked == true, "Блоки питания"),
                    (CasesCategory?.IsChecked == true, "Корпуса")
                };
                categoryMatch = selectedCategories.Any(c => c.Item1 && p.Category?.Name == c.Item2);
            }
            if (!categoryMatch) return false;
            string searchText = SearchBox?.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(searchText) && !p.Name.ToLower().Contains(searchText))
                return false;
            if (!string.IsNullOrWhiteSpace(MinPriceTextBox?.Text))
            {
                if (decimal.TryParse(MinPriceTextBox.Text.Replace(" ", "").Replace("₽", ""),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal minPrice))
                {
                    if (p.Price < minPrice) return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(MaxPriceTextBox?.Text))
            {
                if (decimal.TryParse(MaxPriceTextBox.Text.Replace(" ", "").Replace("₽", ""),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal maxPrice))
                {
                    if (p.Price > maxPrice) return false;
                }
            }
            bool brandFilterActive = NVIDIABrand?.IsChecked == true ||
                                     AMDBrand?.IsChecked == true ||
                                     IntelBrand?.IsChecked == true ||
                                     MSIBrand?.IsChecked == true ||
                                     ASUSBrand?.IsChecked == true;
            if (brandFilterActive)
            {
                string name = p.Name.ToLower();
                bool match = false;
                if (NVIDIABrand?.IsChecked == true && (name.Contains("geforce") || name.Contains("rtx") || name.Contains("gtx"))) match = true;
                if (AMDBrand?.IsChecked == true && (name.Contains("radeon") || name.Contains("ryzen") || name.Contains("rx"))) match = true;
                if (IntelBrand?.IsChecked == true && name.Contains("intel")) match = true;
                if (MSIBrand?.IsChecked == true && name.Contains("msi")) match = true;
                if (ASUSBrand?.IsChecked == true && name.Contains("asus")) match = true;
                if (!match) return false;
            }
            return true;
        }
        private IEnumerable<Product> ApplySorting(List<Product> products)
        {
            var selectedItem = SortComboBox?.SelectedItem as ComboBoxItem;
            string sortType = selectedItem?.Content?.ToString() ?? "Популярные";
            return sortType switch
            {
                "Цена: по возрастанию" => products.OrderBy(p => p.Price),
                "Цена: по убыванию" => products.OrderByDescending(p => p.Price),
                "По рейтингу" => products.OrderByDescending(p => p.Comment.Any() ? p.Comment.Average(c => c.Rating) : 0),
                "Новинки" => products.OrderByDescending(p => p.ProductId),
                _ => products
            };
        }
        // === Обработчики пагинации ===
        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateProductsGrid();
            }
        }
        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdateProductsGrid();
            }
        }
        private async void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                await AddToCartAsync(product);
                MessageBox.Show($"Добавлено: {product.Name}", "В корзину", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private async Task AddToCartAsync(Product product)
        {
            if (product.Amount <= 0)
            {
                MessageBox.Show("Товара нет в наличии", "Нет на складе", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var cartOrder = await GetOrCreateCartOrderAsync();
            var existingItem = cartOrder.OrderProduct.FirstOrDefault(op => op.ProductId == product.ProductId);
            int? newQuantity = existingItem != null ? existingItem.Quantity + 1 : 1;
            if (newQuantity > product.Amount)
            {
                MessageBox.Show($"На складе только {product.Amount} шт.", "Недостаточно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (existingItem != null)
                existingItem.Quantity = newQuantity;
            else
                cartOrder.OrderProduct.Add(new OrderProduct { ProductId = product.ProductId, Quantity = 1 });
            await _dbContext.SaveChangesAsync();
        }
        private async Task<Order> GetOrCreateCartOrderAsync()
        {
            var cartOrder = await _dbContext.Order
                .Include(o => o.OrderProduct)
                .FirstOrDefaultAsync(o => o.UsersId == CurrentUserId && o.OrderStatusId == 1);
            if (cartOrder == null)
            {
                cartOrder = new Order
                {
                    UsersId = CurrentUserId,
                    OrderStatusId = 1,
                    OrderDate = DateOnly.FromDateTime(DateTime.Today),
                    Address = ""
                };
                _dbContext.Order.Add(cartOrder);
                await _dbContext.SaveChangesAsync();
                await _dbContext.Entry(cartOrder).Collection(o => o.OrderProduct).LoadAsync();
            }
            return cartOrder;
        }
        // === Обработчики фильтров ===
        private void ResetCategories_Click(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            AllCategory.IsChecked = true;
            VideoCardsCategory.IsChecked = ProcessorsCategory.IsChecked = RAMCategory.IsChecked =
                StorageCategory.IsChecked = MotherboardsCategory.IsChecked = PSUCategory.IsChecked =
                CasesCategory.IsChecked = NVIDIABrand.IsChecked = AMDBrand.IsChecked = IntelBrand.IsChecked =
                MSIBrand.IsChecked = ASUSBrand.IsChecked = false;
            MinPriceTextBox.Text = "0";
            MaxPriceTextBox.Text = "1000000";
            SearchBox.Text = "";
            _isLoading = false;
            ApplyFiltersAndSortAndPaginate();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void SearchButton_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void ApplyPriceFilter_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void CategoryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (sender is CheckBox cb && cb == AllCategory)
            {
                _isLoading = true;
                foreach (var check in new[] { VideoCardsCategory, ProcessorsCategory, RAMCategory, StorageCategory, MotherboardsCategory, PSUCategory, CasesCategory })
                    check.IsChecked = false;
                _isLoading = false;
            }
            else if (sender is CheckBox)
            {
                AllCategory.IsChecked = false;
            }
            ApplyFiltersAndSortAndPaginate();
        }
        private void CategoryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isLoading) ApplyFiltersAndSortAndPaginate();
        }
        private void BrandCheckBox_Checked(object sender, RoutedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void BrandCheckBox_Unchecked(object sender, RoutedEventArgs e) => ApplyFiltersAndSortAndPaginate();
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profile = new Profile(_dbContext); // ← передаём живой контекст
            profile.Owner = this;
            profile.ShowDialog();
            // После закрытия профиля — обновляем данные в каталоге (если адрес поменялся)
            ApplyFiltersAndSortAndPaginate();
        }
        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow();
            cartWindow.Owner = this;
            cartWindow.ShowDialog();
            ApplyFiltersAndSortAndPaginate();
        }
        private void HomeLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Главная страница", "Навигация");
        }
        protected override void OnClosed(EventArgs e)
        {
            _dbContext?.Dispose();
            base.OnClosed(e);
        }
    }
    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}