using dns.DB;
using dns.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace dns
{
    public partial class ReviewsWindow : Window
    {
        private readonly int _productId;

        public ReviewsWindow(int productId)
        {
            InitializeComponent();
            _productId = productId;
            Loaded += ReviewsWindow_Loaded;
        }

        private async void ReviewsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReviewsAsync();
        }

        private async System.Threading.Tasks.Task LoadReviewsAsync()
        {
            await using var db = new MyDbContext(new DbContextOptions<MyDbContext>());

            var product = await db.Product
                .Where(p => p.ProductId == _productId)
                .Select(p => new { p.Name })
                .FirstOrDefaultAsync();

            if (product != null)
                ProductNameText.Text = $"Отзывы: {product.Name}";

            var comments = await db.Comment
                .Where(c => c.ProductId == _productId)
                .Include(c => c.Users)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            ReviewsPanel.Children.Clear();

            if (!comments.Any())
            {
                ReviewsPanel.Children.Add(new TextBlock
                {
                    Text = "Пока нет отзывов",
                    FontSize = 18,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                return;
            }

            foreach (var comment in comments)
            {
                var reviewCard = CreateReviewCard(comment);
                ReviewsPanel.Children.Add(reviewCard);
                ReviewsPanel.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 12), Opacity = 0.3 });
            }
        }

        private Border CreateReviewCard(Comment comment)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8),
                Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.1 }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Автор + дата
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            var userText = new TextBlock
            {
                Text = comment.Users?.Login ?? "Аноним",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush")
            };
            var dateText = new TextBlock
            {
                Text = comment.Date.ToString("dd.MM.yyyy"),
                Foreground = Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0)
            };
            header.Children.Add(userText);
            header.Children.Add(dateText);

            // Рейтинг
            var starsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
            for (int i = 1; i <= 5; i++)
            {
                var star = new TextBlock
                {
                    Text = i <= comment.Rating ? "★" : "☆",
                    FontSize = 20,
                    Foreground = i <= comment.Rating ? Brushes.Orange : Brushes.LightGray
                };
                starsPanel.Children.Add(star);
            }

            // Текст отзыва
            var descText = new TextBlock
            {
                Text = comment.Description,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush")
            };

            Grid.SetRow(header, 0);
            Grid.SetRow(starsPanel, 1);
            Grid.SetRow(descText, 2);

            grid.Children.Add(header);
            grid.Children.Add(starsPanel);
            grid.Children.Add(descText);

            border.Child = grid;
            return border;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}