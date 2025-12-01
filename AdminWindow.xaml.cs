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
using Microsoft.Win32;
using LessonsManager.Models;
using LessonsManager.Data;
using LessonsManager.Controls;

namespace LessonsManager
{
    public partial class AdminWindow : Window
    {
        private readonly LessonRepository _repository;

        public AdminWindow()
        {
            InitializeComponent();
            _repository = new LessonRepository();
            this.KeyDown += AdminWindow_KeyDown;
            
            // Initialize navigation
            LessonsTreeView.FolderChanged += (s, e) => UpdatePathDisplay();
        }

        private void InitializeControls()
        {
            YearComboBox.SelectedIndex = 0;
            LoadSubjects();
            
            SubjectComboBox.SelectionChanged += SubjectComboBox_SelectionChanged;
        }

        private void LoadSubjects()
        {
            var subjects = _repository.GetAvailableSubjects();
            SubjectComboBox.ItemsSource = subjects;
            
            if (subjects.Count > 0)
                SubjectComboBox.SelectedIndex = 0;
        }

        private void SubjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedSubject = SubjectComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedSubject))
            {
                var subSubjects = _repository.GetAvailableSubSubjects(selectedSubject);
                SubSubjectComboBox.ItemsSource = subSubjects;
                
                if (subSubjects.Count > 0)
                    SubSubjectComboBox.SelectedIndex = 0;
            }
        }

        private void AdminWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BackButton_Click(sender, e);
            }
        }

        private void LoadExistingLessons()
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3|All files (*.*)|*.*",
                Title = "בחר קובץ MP3"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            string subject = SubjectComboBox.Text.Trim();
            string subSubject = SubSubjectComboBox.Text.Trim();
            string lessonTitle = LessonTitleTextBox.Text.Trim();
            string year = (YearComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "2024";
            string filePath = FilePathTextBox.Text;

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(subSubject) || string.IsNullOrEmpty(lessonTitle))
            {
                UploadStatus.Text = "אנא מלא את כל השדות הנדרשים";
                UploadStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
                return;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                UploadStatus.Text = "אנא בחר קובץ MP3 תקין";
                UploadStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
                return;
            }

            try
            {
                var lesson = new Lesson
                {
                    Title = lessonTitle,
                    Subject = subject,
                    SubSubject = subSubject,
                    Year = year
                };

                if (_repository.AddLesson(lesson, filePath))
                {
                    UploadStatus.Text = $"השיעור '{lessonTitle}' הועלה בהצלחה!";
                    UploadStatus.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));

                    ClearUploadForm();
                    LoadSubjects();
                    LoadExistingLessons();
                }
                else
                {
                    UploadStatus.Text = "שגיאה בהעלאת השיעור";
                    UploadStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                }
            }
            catch (Exception ex)
            {
                UploadStatus.Text = $"שגיאה: {ex.Message}";
                UploadStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (LessonsTreeView.SelectedItem == null)
            {
                MessageBox.Show("אנא בחר שיעור למחיקה", "בחירת שיעור", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (LessonsTreeView.SelectedItem is TreeItem selectedItem && !selectedItem.IsFolder)
            {
                var result = MessageBox.Show($"האם אתה בטוח שברצונך למחוק את השיעור '{selectedItem.Name}'?", 
                                           "מחיקת שיעור", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_repository.DeleteLesson(selectedItem.Id))
                        {
                            LoadExistingLessons();
                            MessageBox.Show("השיעור נמחק בהצלחה", "מחיקה הצליחה", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("לא נמצא השיעור למחיקה", "שגיאה", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"שגיאה במחיקת השיעור: {ex.Message}", "שגיאה", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("ניתן למחוק רק שיעורים ספציפיים", "מחיקת שיעור", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LessonsTreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(TreeItem)) is TreeItem draggedItem && draggedItem.IsFolder)
            {
                try
                {
                    // Create USB folder
                    string usbPath = GetUsbDrivePath();
                    if (!string.IsNullOrEmpty(usbPath))
                    {
                        string folderPath = System.IO.Path.Combine(usbPath, draggedItem.Name);
                        Directory.CreateDirectory(folderPath);
                        
                        // Copy all lessons from this folder
                        CopyFolderContents(draggedItem, folderPath);
                        
                        MessageBox.Show($"התיקיה '{draggedItem.Name}' הועברה להתקן בהצלחה", "העברה הצליחה", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("חבר התקן USB תחילה", "חיבור התקן", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"שגיאה בהעברת התיקיה: {ex.Message}", "שגיאה", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetUsbDrivePath()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    return drive.RootDirectory.FullName;
                }
            }
            return "";
        }

        private void CopyFolderContents(TreeItem folderItem, string destinationPath)
        {
            var subjects = _repository.GetAllSubjects();
            
            foreach (var subject in subjects)
            {
                if (subject.Name == folderItem.Name)
                {
                    foreach (var subSubject in subject.SubSubjects)
                    {
                        string subFolderPath = System.IO.Path.Combine(destinationPath, subSubject.Name);
                        Directory.CreateDirectory(subFolderPath);
                        
                        foreach (var lesson in subSubject.Lessons)
                        {
                            string sourcePath = _repository.GetLessonAudioPath(lesson.Id);
                            if (File.Exists(sourcePath))
                            {
                                string fileName = $"{lesson.Title}_{lesson.Year}.mp3";
                                string destPath = System.IO.Path.Combine(subFolderPath, fileName);
                                File.Copy(sourcePath, destPath, true);
                            }
                        }
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadExistingLessons();
        }

        private void ClearUploadForm()
        {
            LessonTitleTextBox.Clear();
            SubjectComboBox.Text = "";
            SubSubjectComboBox.Text = "";
            YearComboBox.SelectedIndex = 0;
            FilePathTextBox.Clear();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
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
    }
}
