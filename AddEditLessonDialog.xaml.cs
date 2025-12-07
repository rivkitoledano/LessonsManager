using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using LessonsManager.Models;
using LessonsManager.Data;

namespace LessonsManager
{
    public partial class AddEditLessonDialog : Window
    {
        public Lesson Lesson { get; private set; }
        private bool isEditMode;
        private string selectedAudioPath;
        private string selectedPdfPath; // ← הוספה חדשה
        private LessonRepository _repository;

        // Constructor for Add mode
        public AddEditLessonDialog()
        {
            InitializeComponent();
            _repository = new LessonRepository();
            isEditMode = false;
            InitializeDialog();
        }

        // Constructor for Edit mode
        public AddEditLessonDialog(Lesson lessonToEdit)
        {
            InitializeComponent();
            _repository = new LessonRepository();
            isEditMode = true;
            Lesson = lessonToEdit;
            InitializeDialog();
            LoadLessonData();
        }

        private void InitializeDialog()
        {
            // עדכן כותרת וסמל בהתאם למצב
            if (isEditMode)
            {
                DialogTitle.Text = "עריכת שיעור";
                HeaderIcon.Kind = PackIconKind.Pencil;
                SaveButtonText.Text = "שמור שינויים";
            }
            else
            {
                DialogTitle.Text = "הוספת שיעור חדש";
                HeaderIcon.Kind = PackIconKind.Plus;
                SaveButtonText.Text = "שמור שיעור";
            }

            // טען נושאים ראשיים מהמערכת
            LoadSubjects();

            // טען שנים (5 שנים אחורה עד שנה הבאה)
            var years = new List<string>();
            int currentYear = DateTime.Now.Year;
            for (int i = currentYear + 1; i >= currentYear - 5; i--)
            {
                years.Add(i.ToString());
            }
            YearComboBox.ItemsSource = years;

            // בחר שנה נוכחית כברירת מחדל במצב הוספה
            if (!isEditMode)
            {
                YearComboBox.SelectedItem = currentYear.ToString();
            }
        }

        private void LoadSubjects()
        {
            try
            {
                // טען נושאים מהמערכת
                var subjects = _repository.GetAvailableSubjects();

                if (subjects.Count > 0)
                {
                    SubjectComboBox.ItemsSource = subjects;
                }
                else
                {
                    // אם אין נושאים במערכת, השתמש בברירת מחדל
                    SubjectComboBox.ItemsSource = new List<string>
                    {
                        "גמרא", "הלכה", "מוסר", "קבלה"
                    };
                }
            }
            catch (Exception ex)
            {
                ShowError($"שגיאה בטעינת נושאים: {ex.Message}");

                // ברירת מחדל במקרה של שגיאה
                SubjectComboBox.ItemsSource = new List<string>
                {
                    "גמרא", "הלכה", "מוסר", "קבלה"
                };
            }
        }

        private void LoadLessonData()
        {
            if (Lesson == null) return;

            TitleTextBox.Text = Lesson.Title;

            // טען נושא ראשי
            if (!string.IsNullOrEmpty(Lesson.Subject))
            {
                SubjectComboBox.SelectedItem = Lesson.Subject;
            }

            // טען נושא משני (אחרי שהנושא הראשי נטען)
            if (!string.IsNullOrEmpty(Lesson.SubSubject))
            {
                SubSubjectComboBox.SelectedItem = Lesson.SubSubject;
            }

            // טען שנה
            if (!string.IsNullOrEmpty(Lesson.Year))
            {
                YearComboBox.SelectedItem = Lesson.Year;
            }

            // טען קובץ שמע - במצב עריכה, הקובץ כבר קיים
            if (!string.IsNullOrEmpty(Lesson.FilePath))
            {
                selectedAudioPath = Lesson.FilePath;
                AudioFileTextBlock.Text = Path.GetFileName(Lesson.FilePath);
                AudioFileTextBlock.Foreground = System.Windows.Media.Brushes.Black;
            }

            // טען קובץ PDF אם קיים ← הוספה חדשה
            if (!string.IsNullOrEmpty(Lesson.PdfPath) && Lesson.HasPdf)
            {
                selectedPdfPath = Lesson.PdfPath;
                PdfFileTextBlock.Text = Path.GetFileName(Lesson.PdfPath);
                PdfFileTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                ClearPdfButton.Visibility = Visibility.Visible;
            }
        }

        private void SubjectComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedSubject = SubjectComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedSubject))
            {
                try
                {
                    // טען נושאי משנה מהמערכת
                    var subSubjects = _repository.GetAvailableSubSubjects(selectedSubject);

                    if (subSubjects.Count > 0)
                    {
                        SubSubjectComboBox.ItemsSource = subSubjects;
                        SubSubjectComboBox.IsEnabled = true;
                    }
                    else
                    {
                        // אם אין נושאי משנה, אפשר הזנה חופשית
                        SubSubjectComboBox.IsEnabled = true;
                        SubSubjectComboBox.ItemsSource = new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"שגיאה בטעינת נושאי משנה: {ex.Message}");
                    SubSubjectComboBox.IsEnabled = true;
                    SubSubjectComboBox.ItemsSource = new List<string>();
                }
            }
            else
            {
                SubSubjectComboBox.ItemsSource = null;
                SubSubjectComboBox.IsEnabled = false;
                SubSubjectComboBox.SelectedItem = null;
            }
        }

        // ← שינוי שם מ-BrowseButton_Click
        private void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "בחר קובץ שמע",
                Filter = "קבצי שמע (*.mp3;*.wav;*.m4a)|*.mp3;*.wav;*.m4a|כל הקבצים (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    selectedAudioPath = openFileDialog.FileName;
                    AudioFileTextBlock.Text = Path.GetFileName(selectedAudioPath);
                    AudioFileTextBlock.Foreground = System.Windows.Media.Brushes.Black;

                    // בדוק את גודל הקובץ
                    var fileInfo = new FileInfo(selectedAudioPath);
                    var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

                    if (fileSizeMB > 500) // אזהרה על קבצים גדולים
                    {
                        ShowWarning($"הקובץ שנבחר גדול ({fileSizeMB:F1} MB). הטעינה עשויה לקחת זמן.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"שגיאה בקריאת הקובץ: {ex.Message}");
                    selectedAudioPath = null;
                    AudioFileTextBlock.Text = "לא נבחר קובץ";
                    AudioFileTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }

        // ← פונקציה חדשה לבחירת PDF
        private void BrowsePdfButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "בחר קובץ PDF",
                Filter = "קבצי PDF (*.pdf)|*.pdf|כל הקבצים (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    selectedPdfPath = openFileDialog.FileName;
                    PdfFileTextBlock.Text = Path.GetFileName(selectedPdfPath);
                    PdfFileTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                    ClearPdfButton.Visibility = Visibility.Visible;

                    // בדוק את גודל הקובץ
                    var fileInfo = new FileInfo(selectedPdfPath);
                    var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

                    if (fileSizeMB > 100) // אזהרה על קבצים גדולים
                    {
                        ShowWarning($"קובץ ה-PDF גדול ({fileSizeMB:F1} MB). הטעינה עשויה לקחת זמן.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"שגיאה בקריאת קובץ ה-PDF: {ex.Message}");
                    selectedPdfPath = null;
                    PdfFileTextBlock.Text = "לא נבחר קובץ";
                    PdfFileTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                    ClearPdfButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        // ← פונקציה חדשה למחיקת PDF
        private void ClearPdfButton_Click(object sender, RoutedEventArgs e)
        {
            selectedPdfPath = null;
            PdfFileTextBlock.Text = "לא נבחר קובץ";
            PdfFileTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            ClearPdfButton.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // בדיקת ולידציה
            if (!ValidateFields())
                return;

            try
            {
                if (isEditMode)
                {
                    // עדכן שיעור קיים
                    Lesson.Title = TitleTextBox.Text.Trim();
                    Lesson.Subject = SubjectComboBox.SelectedItem as string;
                    Lesson.SubSubject = SubSubjectComboBox.SelectedItem as string ?? SubSubjectComboBox.Text.Trim();
                    Lesson.Year = YearComboBox.SelectedItem as string;

                    // אם נבחר קובץ שמע חדש - עדכן אותו
                    if (!string.IsNullOrEmpty(selectedAudioPath) && selectedAudioPath != Lesson.FilePath)
                    {
                        Lesson.FilePath = selectedAudioPath;
                    }

                    // ← עדכן PDF אם נבחר
                    if (!string.IsNullOrEmpty(selectedPdfPath))
                    {
                        Lesson.PdfPath = selectedPdfPath;
                        Lesson.HasPdf = true;
                    }
                    else if (selectedPdfPath == null && Lesson.HasPdf)
                    {
                        // המשתמש הסיר את ה-PDF
                        Lesson.PdfPath = "";
                        Lesson.HasPdf = false;
                    }
                }
                else
                {
                    // צור שיעור חדש
                    Lesson = new Lesson
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = TitleTextBox.Text.Trim(),
                        Subject = SubjectComboBox.SelectedItem as string,
                        SubSubject = SubSubjectComboBox.SelectedItem as string ?? SubSubjectComboBox.Text.Trim(),
                        Year = YearComboBox.SelectedItem as string,
                        FilePath = selectedAudioPath,
                        PdfPath = selectedPdfPath ?? "", // ← שמירת PDF
                        HasPdf = !string.IsNullOrEmpty(selectedPdfPath), // ← הגדרת HasPdf
                        CreatedAt = DateTime.Now
                    };
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"שגיאה בשמירת השיעור: {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            // בדוק כותרת
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                ShowError("נא להזין כותרת לשיעור");
                TitleTextBox.Focus();
                return false;
            }

            // בדוק אורך כותרת
            if (TitleTextBox.Text.Trim().Length < 3)
            {
                ShowError("כותרת השיעור חייבת להכיל לפחות 3 תווים");
                TitleTextBox.Focus();
                return false;
            }

            // בדוק נושא ראשי
            if (SubjectComboBox.SelectedItem == null)
            {
                ShowError("נא לבחור נושא ראשי");
                SubjectComboBox.Focus();
                return false;
            }

            // בדוק נושא משני
            if (SubSubjectComboBox.SelectedItem == null && string.IsNullOrWhiteSpace(SubSubjectComboBox.Text))
            {
                ShowError("נא לבחור או להזין נושא משני");
                SubSubjectComboBox.Focus();
                return false;
            }

            // בדוק שנה
            if (YearComboBox.SelectedItem == null)
            {
                ShowError("נא לבחור שנה");
                YearComboBox.Focus();
                return false;
            }

            // בדוק קובץ שמע (רק במצב הוספה)
            if (!isEditMode && string.IsNullOrEmpty(selectedAudioPath))
            {
                ShowError("נא לבחור קובץ שמע");
                return false;
            }

            // בדוק שהקובץ קיים (אם נבחר)
            if (!string.IsNullOrEmpty(selectedAudioPath) && !File.Exists(selectedAudioPath))
            {
                ShowError("הקובץ שנבחר לא נמצא. נא לבחור קובץ אחר.");
                return false;
            }

            // בדוק שקובץ ה-PDF קיים (אם נבחר) ← הוספה חדשה
            if (!string.IsNullOrEmpty(selectedPdfPath) && !File.Exists(selectedPdfPath))
            {
                ShowError("קובץ ה-PDF שנבחר לא נמצא. נא לבחור קובץ אחר.");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            var messageBox = new CustomMessageBox(
                "שגיאה",
                message,
                MessageType.Error,
                MessageButtons.OK
            );
            messageBox.Owner = this;
            messageBox.ShowDialog();
        }

        private void ShowWarning(string message)
        {
            var messageBox = new CustomMessageBox(
                "אזהרה",
                message,
                MessageType.Warning,
                MessageButtons.OK
            );
            messageBox.Owner = this;
            messageBox.ShowDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // בדוק אם יש שינויים שלא נשמרו
            if (HasUnsavedChanges())
            {
                var messageBox = new CustomMessageBox(
                    "שינויים לא נשמרו",
                    "יש לך שינויים שלא נשמרו. האם אתה בטוח שברצונך לצאת?",
                    MessageType.Warning,
                    MessageButtons.YesNo
                );
                messageBox.Owner = this;

                if (messageBox.ShowDialog() != MessageDialogResult.Yes)
                    return;
            }

            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton_Click(sender, e);
        }

        private bool HasUnsavedChanges()
        {
            // במצב עריכה, בדוק אם יש שינויים מהערכים המקוריים
            if (isEditMode && Lesson != null)
            {
                if (TitleTextBox.Text.Trim() != Lesson.Title)
                    return true;

                if (SubjectComboBox.SelectedItem as string != Lesson.Subject)
                    return true;

                var currentSubSubject = SubSubjectComboBox.SelectedItem as string ?? SubSubjectComboBox.Text.Trim();
                if (currentSubSubject != Lesson.SubSubject)
                    return true;

                if (YearComboBox.SelectedItem as string != Lesson.Year)
                    return true;

                if (!string.IsNullOrEmpty(selectedAudioPath) && selectedAudioPath != Lesson.FilePath)
                    return true;

                // ← בדיקה לשינוי PDF
                if (!string.IsNullOrEmpty(selectedPdfPath) && selectedPdfPath != Lesson.PdfPath)
                    return true;
            }
            else // במצב הוספה, בדוק אם יש טקסט בשדות
            {
                if (!string.IsNullOrWhiteSpace(TitleTextBox.Text))
                    return true;

                if (SubjectComboBox.SelectedItem != null)
                    return true;

                if (SubSubjectComboBox.SelectedItem != null || !string.IsNullOrWhiteSpace(SubSubjectComboBox.Text))
                    return true;

                if (!string.IsNullOrEmpty(selectedAudioPath))
                    return true;

                // ← בדיקה אם נבחר PDF
                if (!string.IsNullOrEmpty(selectedPdfPath))
                    return true;
            }

            return false;
        }
    }
}