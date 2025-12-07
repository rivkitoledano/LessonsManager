using System;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;

namespace LessonsManager
{
    public partial class AdminLoginWindow : Window
    {
        // For window dragging
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private const string ADMIN_USERNAME = "1";
        private const string ADMIN_PASSWORD = "1";

        public AdminLoginWindow()
        {
            InitializeComponent();
            
            // Set up window behavior
            this.KeyDown += AdminLoginWindow_KeyDown;
            
            // Set focus to username field
            Loaded += (s, e) => UsernameTextBox.Focus();
        }
        
        // Enable window dragging
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void AdminLoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                BackButton_Click(sender, e);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("אנא הכנס שם משתמש וסיסמה");
                return;
            }

            if (username == ADMIN_USERNAME && password == ADMIN_PASSWORD)
            {
                AdminWindow adminWindow = new AdminWindow();
                adminWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("שם משתמש או סיסמה שגויים");
                PasswordBox.Clear();
                UsernameTextBox.Focus();
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        // Window control buttons
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        
        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            // Adjust window border when maximized/restored
            if (WindowState == WindowState.Maximized)
            {
                BorderThickness = new Thickness(8);
            }
            else
            {
                BorderThickness = new Thickness(0);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Only prevent closing if it's not from the close button
            if (e != null && !_isClosingFromButton)
            {
                e.Cancel = true;
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Hide();
            }
            
            base.OnClosing(e);
        }
        
        private bool _isClosingFromButton = false;
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Make sure the application shuts down if this was the last window
            if (Application.Current.Windows.Count == 0)
            {
                Application.Current.Shutdown();
            }
        }

        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
