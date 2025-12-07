using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using LessonsManager.Models;
using LessonsManager.Data;

namespace LessonsManager
{
    public partial class AdminWindow : Window
    {
        private ObservableCollection<Lesson> lessons;
        private LessonRepository _repository;

        public AdminWindow()
        {
            InitializeComponent();
            _repository = new LessonRepository();
            LoadLessons();
        }

        private void LoadLessons()
        {
            try
            {
                // טען את כל הנושאים עם השיעורים שלהם
                var subjects = _repository.GetAllSubjects();

                // המר למבנה שטוח של שיעורים עבור הרשימה
                var allLessons = new List<Lesson>();
                foreach (var subject in subjects)
                {
                    foreach (var subSubject in subject.SubSubjects)
                    {
                        allLessons.AddRange(subSubject.Lessons);
                    }
                }

                lessons = new ObservableCollection<Lesson>(allLessons.OrderBy(l => l.Title));
                LessonsListView.ItemsSource = lessons;

                // הצג הודעה
                if (lessons.Count == 0)
                {
                    ShowNotification("אין שיעורים במערכת. לחץ על 'הוסף שיעור' כדי להתחיל.", 4000);
                }
                else
                {
                    ShowNotification($"נטענו {lessons.Count} שיעורים מהמערכת", 2000);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה בטעינת השיעורים: {ex.Message}");
                lessons = new ObservableCollection<Lesson>();
                LessonsListView.ItemsSource = lessons;
            }
        }

        private void AddLessonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddEditLessonDialog();
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    var newLesson = dialog.Lesson;

                    // שמור בקובץ/מסד נתונים דרך הרפוזיטורי
                    bool success = _repository.AddLesson(
                        newLesson,
                        newLesson.FilePath,
                        string.IsNullOrEmpty(newLesson.PdfPath) ? null : newLesson.PdfPath
                    );
                    if (success)
                    {
                        // הוסף לרשימה
                        lessons.Add(newLesson);

                        // הצג הודעת הצלחה
                        ShowSuccessNotification($"השיעור '{newLesson.Title}' נוסף בהצלחה!");

                        // גלול לפריט החדש
                        LessonsListView.ScrollIntoView(newLesson);
                    }
                    else
                    {
                        ShowErrorMessage("שגיאה בהוספת השיעור למערכת");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה בהוספת השיעור: {ex.Message}");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var lesson = button?.Tag as Lesson;

                if (lesson == null)
                {
                    ShowErrorMessage("לא נמצא שיעור לעריכה");
                    return;
                }

                var dialog = new AddEditLessonDialog(lesson);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    var updatedLesson = dialog.Lesson;

                    // עדכן בקובץ/מסד נתונים דרך הרפוזיטורי
                    bool success = _repository.UpdateLesson(updatedLesson);

                    if (success)
                    {
                        // עדכן ברשימה
                        var index = lessons.IndexOf(lesson);
                        if (index >= 0)
                        {
                            lessons[index] = updatedLesson;
                        }

                        // רענן את התצוגה
                        LessonsListView.Items.Refresh();

                        ShowSuccessNotification($"השיעור '{updatedLesson.Title}' עודכן בהצלחה!");
                    }
                    else
                    {
                        ShowErrorMessage("שגיאה בעדכון השיעור במערכת");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה בעדכון השיעור: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var lesson = button?.Tag as Lesson;

                if (lesson == null)
                {
                    ShowErrorMessage("לא נמצא שיעור למחיקה");
                    return;
                }

                // השתמש ב-CustomMessageBox במקום MessageBox.Show  
                var messageBox = new CustomMessageBox(
                    "מחיקת שיעור",
                    $"האם אתה בטוח שברצונך למחוק את השיעור:\n\n'{lesson.Title}'\n\nפעולה זו תמחק גם את קובץ השמע ואינה ניתנת לביטול.",
                    MessageType.Warning, // Changed from PackIconKind.DeleteAlert to MessageType.Warning  
                    MessageButtons.YesNo // Corrected from 'true' to 'MessageButtons.YesNo'  
                );
                messageBox.Owner = this;

                if (messageBox.ShowDialog() == MessageDialogResult.Yes)
                {
                    // מחק דרך הרפוזיטורי (ימחק גם את הקובץ)  
                    bool success = _repository.DeleteLesson(lesson.Id);

                    if (success)
                    {
                        // מחק מהרשימה  
                        lessons.Remove(lesson);

                        ShowSuccessNotification($"השיעור '{lesson.Title}' נמחק בהצלחה");
                    }
                    else
                    {
                        ShowErrorMessage("שגיאה במחיקת השיעור מהמערכת");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה במחיקת השיעור: {ex.Message}");
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // הצג את פאנל התצוגה המקדימה
                LessonsPanel.Visibility = Visibility.Collapsed;
                PreviewPanel.Visibility = Visibility.Visible;

                // טען את מסך התלמידים
                LoadStudentPreview();

                ShowNotification("מוצג מצב תצוגה מקדימה - כפי שהתלמידים רואים", 3000);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה בהצגת תצוגה מקדימה: {ex.Message}");

                // חזור למסך ניהול במקרה של שגיאה
                PreviewPanel.Visibility = Visibility.Collapsed;
                LessonsPanel.Visibility = Visibility.Visible;
            }
        }

        private void ClosePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            // חזור למסך ניהול
            PreviewPanel.Visibility = Visibility.Collapsed;
            LessonsPanel.Visibility = Visibility.Visible;

            ShowNotification("חזרה למסך ניהול", 2000);
        }

        private void LoadStudentPreview()
        {
            try
            {
                // נקה תוכן קודם
                StudentPreviewContainer.Child = null;

                // צור instance של StudentWindow והצג את התוכן שלו
                var studentWindow = new StudentWindow();

                // קח את התוכן מהחלון והצג אותו בקונטיינר
                if (studentWindow.Content is FrameworkElement content)
                {
                    studentWindow.Content = null; // הסר מהחלון המקורי
                    StudentPreviewContainer.Child = content; // הוסף לקונטיינר
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"שגיאה בטעינת תצוגה מקדימה: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // בדוק אם יש שינויים שלא נשמרו  
            var messageBox = new CustomMessageBox(
                "חזרה למסך הראשי",
                "האם אתה בטוח שברצונך לחזור למסך הראשי?",
                MessageType.Info, // Changed from PackIconKind.Home to MessageType.Info  
                MessageButtons.YesNo // Corrected from 'true' to 'MessageButtons.YesNo'  
            );
            messageBox.Owner = this;

            if (messageBox.ShowDialog() == MessageDialogResult.Yes)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }

        // =============== פונקציות עזר להצגת הודעות ===============

        /// <summary>
        /// הצג הודעת הצלחה בירוק עם סימן V
        /// </summary>
        private void ShowSuccessNotification(string message, int durationMs = 3000)
        {
            AdminSnackbar.MessageQueue?.Enqueue(
                $"✓ {message}",
                null,
                null,
                null,
                false,
                true,
                TimeSpan.FromMilliseconds(durationMs));
        }

        /// <summary>
        /// הצג הודעה רגילה
        /// </summary>
        private void ShowNotification(string message, int durationMs = 3000)
        {
            AdminSnackbar.MessageQueue?.Enqueue(
                message,
                null,
                null,
                null,
                false,
                true,
                TimeSpan.FromMilliseconds(durationMs));
        }

        /// <summary>
        /// הצג הודעת שגיאה עם CustomMessageBox
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            var messageBox = new CustomMessageBox(
                "שגיאה",
                message,
                MessageType.Error, // Changed from PackIconKind.AlertCircle to MessageType.Error
                MessageButtons.OK
            );
            messageBox.Owner = this;
            messageBox.ShowDialog();
        }

        /// <summary>
        /// הצג הודעת אזהרה עם CustomMessageBox
        /// </summary>
        private void ShowWarningMessage(string message)
        {
            var messageBox = new CustomMessageBox(
                "אזהרה",
                message,
                MessageType.Warning, // Changed from PackIconKind.Alert to MessageType.Warning
                MessageButtons.OK
            );
            messageBox.Owner = this;
            messageBox.ShowDialog();
        }

        /// <summary>
        /// הצג דיאלוג אישור - מחזיר true אם המשתמש לחץ כן
        /// </summary>
        private bool ShowConfirmDialog(string title, string message)
        {
            var messageBox = new CustomMessageBox(
                title,
                message,
                MessageType.Warning,
                MessageButtons.YesNo
            );
            messageBox.Owner = this;
            return messageBox.ShowDialog() == MessageDialogResult.Yes;
        }
    }
}
