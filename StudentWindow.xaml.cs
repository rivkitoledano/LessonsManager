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
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using LessonsManager.Models;
using LessonsManager.Data;
using LessonsManager.Controls;

namespace LessonsManager
{
    public partial class StudentWindow : Window
    {
        private bool isDeviceConnected = false;
        private string devicePath = "";
        private readonly LessonRepository _repository;

        // Import Win32 API functions for kiosk mode
        [DllImport("user32.dll")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SPI_SETSCREENSAVERRUNNING = 97;
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        public StudentWindow()
        {
            InitializeComponent();
            _repository = new LessonRepository();
            
            // Enable full kiosk mode
            EnableKioskMode();
            
            // Initialize navigation
            LessonsTreeView.FolderChanged += (s, e) => UpdatePathDisplay();
        }

        private void EnableKioskMode()
        {
            try
            {
                // Hide taskbar
                int hWnd = FindWindow("Shell_TrayWnd", "");
                if (hWnd != 0)
                {
                    ShowWindow(hWnd, SW_HIDE);
                }

                // Hide desktop icons
                int desktopWnd = FindWindow("Progman", "Program Manager");
                if (desktopWnd != 0)
                {
                    ShowWindow(desktopWnd, SW_HIDE);
                }

                // Disable screensaver
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 1, IntPtr.Zero, 0);

                // Disable Alt+Tab
                DisableAltTab();
                
                // Disable Ctrl+Alt+Del
                DisableCtrlAltDel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kiosk mode error: {ex.Message}");
            }
        }

        private void DisableAltTab()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                }
                key.SetValue("NoAltTab", 1, RegistryValueKind.DWord);
                key.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to disable Alt+Tab: {ex.Message}");
            }
        }

        private void DisableCtrlAltDel()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                }
                key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                key.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to disable Ctrl+Alt+Del: {ex.Message}");
            }
        }

        private void RestoreSystem()
        {
            try
            {
                // Show taskbar
                int hWnd = FindWindow("Shell_TrayWnd", "");
                if (hWnd != 0)
                {
                    ShowWindow(hWnd, SW_SHOW);
                }

                // Show desktop icons
                int desktopWnd = FindWindow("Progman", "Program Manager");
                if (desktopWnd != 0)
                {
                    ShowWindow(desktopWnd, SW_SHOW);
                }

                // Enable screensaver
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 0, IntPtr.Zero, 0);

                // Restore registry settings
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key != null)
                {
                    key.DeleteValue("NoAltTab", false);
                    key.DeleteValue("DisableTaskMgr", false);
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore system: {ex.Message}");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Block ALL key combinations that could exit
            if (e.Key == Key.Escape || 
                e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt ||
                e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt ||
                e.Key == Key.F11 ||
                e.Key == Key.System) // Alt key
            {
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent closing
            base.OnClosing(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Remove close button from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
        }

        private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isDeviceConnected)
            {
                // Try to connect to USB device
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.Removable && drive.IsReady)
                    {
                        isDeviceConnected = true;
                        devicePath = drive.RootDirectory.FullName;
                        DeviceStatus.Text = $"התקן מחובר: {devicePath}";
                        DeviceStatus.Foreground = new SolidColorBrush(Colors.LightGreen);
                        ConnectDeviceButton.Content = "נתק התקן";
                        ConnectDeviceButton.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                        LoadAvailableLessons();
                        return;
                    }
                }

                DeviceStatus.Text = "לא נמצא התקן זמין. חבר התקן USB ונסה שוב.";
                DeviceStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
            }
            else
            {
                // Disconnect device
                isDeviceConnected = false;
                devicePath = "";
                DeviceStatus.Text = "לא מחובר להתקן";
                DeviceStatus.Foreground = new SolidColorBrush(Colors.LightGray);
                ConnectDeviceButton.Content = "חבר התקן";
                ConnectDeviceButton.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
        }

        private void LoadAvailableLessons()
        {
            var treeItems = new ObservableCollection<TreeItem>();
            var subjects = _repository.GetAllSubjects();
            
            foreach (var subject in subjects)
            {
                var subjectItem = new TreeItem(subject.Name, subject.Name, $"subject_{subject.Name}", true, 0);
                treeItems.Add(subjectItem);
                
                foreach (var subSubject in subject.SubSubjects)
                {
                    var subSubjectPath = $"{subject.Name}/{subSubject.Name}";
                    var subSubjectItem = new TreeItem(subSubject.Name, subSubjectPath, $"subsubject_{subSubject.Name}", true, 1);
                    treeItems.Add(subSubjectItem);
                    
                    foreach (var lesson in subSubject.Lessons)
                    {
                        var lessonPath = $"{subSubjectPath}/{lesson.Title}";
                        var lessonItem = new TreeItem($"{lesson.Title} ({lesson.Year})", lessonPath, lesson.Id, false, 2, lesson);
                        treeItems.Add(lessonItem);
                    }
                }
            }
            
            LessonsTreeView.TreeItems = treeItems;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (LessonsTreeView.SelectedItem == null)
            {
                MessageBox.Show("אנא בחר שיעור להורדה", "בחירת שיעור", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (LessonsTreeView.SelectedItem is TreeItem selectedItem && !selectedItem.IsFolder)
            {
                if (!isDeviceConnected || string.IsNullOrEmpty(devicePath))
                {
                    MessageBox.Show("חבר התקן תחילה", "חיבור התקן", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    string sourcePath = _repository.GetLessonAudioPath(selectedItem.Id);
                    if (File.Exists(sourcePath))
                    {
                        string fileName = $"{selectedItem.Lesson!.Subject}_{selectedItem.Lesson.SubSubject}_{selectedItem.Lesson.Title}.mp3";
                        string destinationPath = System.IO.Path.Combine(devicePath, fileName);

                        File.Copy(sourcePath, destinationPath, true);
                        
                        MessageBox.Show($"השיעור '{selectedItem.Lesson.Title}' הורד בהצלחה להתקן!", "הורדה הצליחה", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("קובץ השיעור לא נמצא", "שגיאה", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"שגיאה בהורדת השיעור: {ex.Message}", "שגיאה", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("ניתן להוריד רק שיעורים ספציפיים", "הורדת שיעור", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Navigation functions
        private void UpdatePathDisplay()
        {
            if (LessonsTreeView.CurrentFolder == null)
            {
                PathTextBlock.Text = "שורש";
            }
            else
            {
                PathTextBlock.Text = LessonsTreeView.CurrentFolder.FullPath;
            }
        }

        private void NavBackButton_Click(object sender, RoutedEventArgs e)
        {
            LessonsTreeView.NavigateBack();
        }

        private void RootButton_Click(object sender, RoutedEventArgs e)
        {
            LessonsTreeView.NavigateToRoot();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // In student mode, we don't allow going back to main window
            // This method exists only to prevent XAML errors
            return;
        }
    }
}
