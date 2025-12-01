using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace LessonsManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool EnableWindow(int hWnd, bool bEnable);

        public MainWindow()
        {
            InitializeComponent();
            DisableTaskManager();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void DisableTaskManager()
        {
            try
            {
                int hwnd = FindWindow("Windows.TaskManager", "Task Manager");
                if (hwnd != 0)
                {
                    ShowWindow(hwnd, 0);
                    EnableWindow(hwnd, false);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Block Alt+F4, Ctrl+Alt+Delete, etc.
            if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true;
            }
            
            // Block Windows key
            if (e.Key == Key.LWin || e.Key == Key.RWin)
            {
                e.Handled = true;
            }
        }

        private void StudentButton_Click(object sender, RoutedEventArgs e)
        {
            StudentWindow studentWindow = new StudentWindow();
            studentWindow.Show();
            this.Hide();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            AdminLoginWindow adminLoginWindow = new AdminLoginWindow();
            adminLoginWindow.Show();
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}