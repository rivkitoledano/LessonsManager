using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LessonsManager.Data;
using LessonsManager.Models;
using Microsoft.Win32;

namespace LessonsManager
{
    public partial class StudentWindow : Window
    {
        public class TreeItem
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public string Id { get; set; }
            public bool IsFolder { get; set; }
            public int Level { get; set; }
            public Lesson? Lesson { get; set; }

            public TreeItem(string name, string fullPath, string id, bool isFolder, int level, Lesson? lesson = null)
            {
                Name = name;
                FullPath = fullPath;
                Id = id;
                IsFolder = isFolder;
                Level = level;
                Lesson = lesson;
            }
        }

        // Win32 API for kiosk mode
        [DllImport("user32.dll")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

        private const int SW_HIDE = 0;
        private const int SPI_SETSCREENSAVERRUNNING = 97;

        private bool isDeviceConnected = false;
        private string devicePath = "";
        private readonly LessonRepository _repository;
        private DispatcherTimer _deviceCheckTimer;
        private ObservableCollection<TreeItem> _allTreeItems = new ObservableCollection<TreeItem>();
        private ObservableCollection<TreeItem> _currentFolderItems = new ObservableCollection<TreeItem>();
        private ObservableCollection<TreeItem> _filteredItems = new ObservableCollection<TreeItem>();
        private Stack<string> _navigationHistory = new Stack<string>();
        private string _currentPath = "";

        public StudentWindow()
        {
            InitializeComponent();
            _repository = new LessonRepository();

            FolderItems.ItemsSource = _filteredItems;

            // Enable kiosk mode
            EnableKioskMode();

            this.Loaded += (s, e) =>
            {
                LoadAvailableLessons();
                LoadFilterOptions();
                NavigateToRoot();

                _deviceCheckTimer = new DispatcherTimer();
                _deviceCheckTimer.Interval = TimeSpan.FromSeconds(2);
                _deviceCheckTimer.Tick += DeviceCheckTimer_Tick;
                _deviceCheckTimer.Start();
            };

            // Prevent closing
            this.Closing += (s, e) => e.Cancel = true;
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

                // Disable screensaver
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 1, IntPtr.Zero, 0);

                // Disable Alt+Tab
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                }
                key.SetValue("NoAltTab", 1, RegistryValueKind.DWord);
                key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                key.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kiosk mode error: {ex.Message}");
            }
        }

        private void LoadFilterOptions()
        {
            var subjects = _repository.GetAllSubjects();

            // Load subjects
            var subjectsList = new List<string> { "הכל" };
            subjectsList.AddRange(subjects.Select(s => s.Name).Distinct());
            SubjectFilterComboBox.ItemsSource = subjectsList;
            SubjectFilterComboBox.SelectedIndex = 0;

            // Load years
            var yearsList = new List<string> { "הכל" };
            var allLessons = subjects.SelectMany(s => s.SubSubjects).SelectMany(ss => ss.Lessons);
            yearsList.AddRange(allLessons.Select(l => l.Year).Distinct().OrderByDescending(y => y));
            YearFilterComboBox.ItemsSource = yearsList;
            YearFilterComboBox.SelectedIndex = 0;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ToggleFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            FiltersPanel.Visibility = FiltersPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update sub-subjects when subject changes
            if (sender == SubjectFilterComboBox && SubjectFilterComboBox.SelectedItem != null)
            {
                string selectedSubject = SubjectFilterComboBox.SelectedItem.ToString();
                var subSubjectsList = new List<string> { "הכל" };

                if (selectedSubject != "הכל")
                {
                    var subject = _repository.GetAllSubjects().FirstOrDefault(s => s.Name == selectedSubject);
                    if (subject != null)
                    {
                        subSubjectsList.AddRange(subject.SubSubjects.Select(ss => ss.Name));
                    }
                }

                SubSubjectFilterComboBox.ItemsSource = subSubjectsList;
                SubSubjectFilterComboBox.SelectedIndex = 0;
            }

            ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SubjectFilterComboBox.SelectedIndex = 0;
            SubSubjectFilterComboBox.SelectedIndex = 0;
            YearFilterComboBox.SelectedIndex = 0;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_currentFolderItems == null) return;

            var searchText = SearchTextBox.Text?.ToLower() ?? "";
            var selectedSubject = SubjectFilterComboBox.SelectedItem?.ToString() ?? "הכל";
            var selectedSubSubject = SubSubjectFilterComboBox.SelectedItem?.ToString() ?? "הכל";
            var selectedYear = YearFilterComboBox.SelectedItem?.ToString() ?? "הכל";

            _filteredItems.Clear();

            foreach (var item in _currentFolderItems)
            {
                bool matches = true;

                // Search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    matches = item.Name.ToLower().Contains(searchText);
                }

                // Subject filter
                if (matches && selectedSubject != "הכל" && item.Lesson != null)
                {
                    matches = item.Lesson.Subject == selectedSubject;
                }

                // SubSubject filter
                if (matches && selectedSubSubject != "הכל" && item.Lesson != null)
                {
                    matches = item.Lesson.SubSubject == selectedSubSubject;
                }

                // Year filter
                if (matches && selectedYear != "הכל" && item.Lesson != null)
                {
                    matches = item.Lesson.Year == selectedYear;
                }

                if (matches)
                {
                    _filteredItems.Add(item);
                }
            }
        }

        private void DeviceCheckTimer_Tick(object sender, EventArgs e)
        {
            bool deviceConnected = CheckForConnectedDevice();
            if (deviceConnected != isDeviceConnected)
            {
                isDeviceConnected = deviceConnected;
            }
        }

        private bool CheckForConnectedDevice()
        {
            if (string.IsNullOrEmpty(devicePath))
                return false;

            try
            {
                return Directory.Exists(devicePath);
            }
            catch
            {
                return false;
            }
        }

        private void LoadAvailableLessons()
        {
            _allTreeItems.Clear();
            var subjects = _repository.GetAllSubjects();

            foreach (var subject in subjects)
            {
                var subjectItem = new TreeItem(subject.Name, subject.Name, $"subject_{subject.Name}", true, 0);
                _allTreeItems.Add(subjectItem);

                foreach (var subSubject in subject.SubSubjects)
                {
                    var subSubjectPath = $"{subject.Name}/{subSubject.Name}";
                    var subSubjectItem = new TreeItem(subSubject.Name, subSubjectPath, $"subsubject_{subSubject.Name}", true, 1);
                    _allTreeItems.Add(subSubjectItem);

                    foreach (var lesson in subSubject.Lessons)
                    {
                        var lessonPath = $"{subSubjectPath}/{lesson.Title}";
                        var lessonItem = new TreeItem($"{lesson.Title} ({lesson.Year})", lessonPath, lesson.Id, false, 2, lesson);
                        _allTreeItems.Add(lessonItem);
                    }
                }
            }
        }

        private void NavigateToRoot()
        {
            _currentPath = "";
            _navigationHistory.Clear();
            UpdateCurrentFolderView();
            UpdateAddressBar();
            UpdateNavigationButtons();
        }

        private void NavigateToFolder(TreeItem folder)
        {
            if (!folder.IsFolder)
                return;

            if (!string.IsNullOrEmpty(_currentPath))
            {
                _navigationHistory.Push(_currentPath);
            }

            _currentPath = folder.FullPath;
            UpdateCurrentFolderView();
            UpdateAddressBar();
            UpdateNavigationButtons();
        }

        private void UpdateCurrentFolderView()
        {
            _currentFolderItems.Clear();

            if (string.IsNullOrEmpty(_currentPath))
            {
                var rootItems = _allTreeItems.Where(x => x.Level == 0).ToList();
                foreach (var item in rootItems)
                {
                    _currentFolderItems.Add(item);
                }
            }
            else
            {
                var currentLevel = _currentPath.Split('/').Length;

                var items = _allTreeItems.Where(x =>
                {
                    if (x.Level != currentLevel)
                        return false;

                    var parentPath = GetParentPath(x.FullPath);
                    return parentPath == _currentPath;
                }).ToList();

                foreach (var item in items)
                {
                    _currentFolderItems.Add(item);
                }
            }

            ApplyFilters();
        }

        private string GetParentPath(string fullPath)
        {
            var parts = fullPath.Split('/');
            if (parts.Length <= 1)
                return "";

            return string.Join("/", parts.Take(parts.Length - 1));
        }

        private void UpdateAddressBar()
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                AddressBar.Text = "שיעורים";
            }
            else
            {
                AddressBar.Text = "שיעורים > " + _currentPath.Replace("/", " > ");
            }
        }

        private void UpdateNavigationButtons()
        {
            BackButton.IsEnabled = _navigationHistory.Count > 0 || !string.IsNullOrEmpty(_currentPath);
            HomeButton.IsEnabled = !string.IsNullOrEmpty(_currentPath);
        }

        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TreeItem item)
            {
                if (item.IsFolder)
                {
                    NavigateToFolder(item);
                }
                else
                {
                    DownloadLesson(item);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                _currentPath = _navigationHistory.Pop();
                UpdateCurrentFolderView();
                UpdateAddressBar();
                UpdateNavigationButtons();
            }
            else if (!string.IsNullOrEmpty(_currentPath))
            {
                var parentPath = GetParentPath(_currentPath);
                _currentPath = parentPath;
                UpdateCurrentFolderView();
                UpdateAddressBar();
                UpdateNavigationButtons();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRoot();
        }

        private void DownloadLesson(TreeItem lessonItem)
        {
            if (!isDeviceConnected || string.IsNullOrEmpty(devicePath))
            {
                MessageBox.Show("חבר התקן תחילה", "חיבור התקן", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sourcePath = _repository.GetLessonAudioPath(lessonItem.Id);
                if (File.Exists(sourcePath))
                {
                    string fileName = $"{lessonItem.Lesson!.Subject}_{lessonItem.Lesson.SubSubject}_{lessonItem.Lesson.Title}.mp3";
                    string destinationPath = Path.Combine(devicePath, fileName);

                    File.Copy(sourcePath, destinationPath, true);

                    MessageBox.Show($"השיעור '{lessonItem.Lesson.Title}' הורד בהצלחה להתקן!",
                        "הורדה הצליחה", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isDeviceConnected)
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.Removable && drive.IsReady)
                    {
                        isDeviceConnected = true;
                        devicePath = drive.RootDirectory.FullName;
                        NotificationHelper.ShowSuccess($"התקן מחובר בהצלחה: {devicePath}");

                        return;
                    }
                }

                NotificationHelper.ShowError($"שגיאה בחיבור ההתקן");
            }
            else
            {
                isDeviceConnected = false;
                devicePath = "";
                MessageBox.Show("ההתקן נותק", "ניתוק התקן", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var messageBox = new CustomMessageBox(
                "אישור התנתקות",
                "האם אתה בטוח שברצונך להתנתק?",
                MessageType.Question,
                MessageButtons.YesNo);

            var result = messageBox.ShowDialog();

            if (result == MessageDialogResult.Yes) // Fix: Changed comparison to use MessageDialogResult instead of MessageBoxResult  
            {
                _deviceCheckTimer?.Stop();
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _deviceCheckTimer?.Stop();
            base.OnClosed(e);
        }
    }
}