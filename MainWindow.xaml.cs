using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using MaterialDesignThemes.Wpf;  // Add this for Snackbar

namespace LessonsManager
{
    public partial class MainWindow : Window
    {
        // Remove this line - MainSnackbar is already defined in XAML
        // public Snackbar MainSnackbar { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // Set up window chrome for custom title bar
            var chrome = new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(5),
                UseAeroCaptionButtons = false
            };

            WindowChrome.SetWindowChrome(this, chrome);

            // Set up window state changed event
            StateChanged += MainWindow_StateChanged;

            // Set initial position
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Handle window state changes if needed
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation dialog before closing
            var result = MessageBox.Show(
                "האם אתה בטוח שברצונך לסגור את התוכנה?",
                "אישור יציאה",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void StudentButton_Click(object sender, RoutedEventArgs e)
        {
            var studentWindow = new StudentWindow();
            studentWindow.Show();
            this.Hide();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var adminLoginWindow = new AdminLoginWindow();
            adminLoginWindow.Show();
            this.Hide();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "מערכת ניהול שיעורים\n\nגרסה 2.0\n\n© 2025 כל הזכויות שמורות",
                "אודות המערכת",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Enable window dragging
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}