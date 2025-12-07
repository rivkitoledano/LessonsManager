using LessonsManager.Models;
using System.Text.Json;
using System.IO;

namespace LessonsManager.Data
{
    public class LessonRepository
    {
        private const string DATA_FOLDER = "LessonsData";
        private const string METADATA_FILE = "lessons_metadata.json";
        private const string AUDIO_FOLDER = "AudioFiles";
        private const string PDF_FOLDER = "PdfFiles";
        
        private readonly string _dataPath;
        private readonly string _metadataPath;
        private readonly string _audioPath;
        private readonly string _pdfPath;

        public LessonRepository()
        {
            _dataPath = Path.Combine(Environment.CurrentDirectory, DATA_FOLDER);
            _metadataPath = Path.Combine(_dataPath, METADATA_FILE);
            _audioPath = Path.Combine(_dataPath, AUDIO_FOLDER);
            _pdfPath = Path.Combine(_dataPath, PDF_FOLDER);
            
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);
            
            if (!Directory.Exists(_audioPath))
                Directory.CreateDirectory(_audioPath);
            
            if (!Directory.Exists(_pdfPath))
                Directory.CreateDirectory(_pdfPath);
        }

        public List<SubjectNode> GetAllSubjects()
        {
            var lessons = LoadAllLessons();
            var subjects = new List<SubjectNode>();

            var groupedLessons = lessons
                .GroupBy(l => l.Subject)
                .OrderBy(g => g.Key);

            foreach (var subjectGroup in groupedLessons)
            {
                var subjectNode = new SubjectNode { Name = subjectGroup.Key };
                
                var subSubjectGroups = subjectGroup
                    .GroupBy(l => l.SubSubject)
                    .OrderBy(g => g.Key);

                foreach (var subSubjectGroup in subSubjectGroups)
                {
                    var subSubjectNode = new SubSubjectNode 
                    { 
                        Name = subSubjectGroup.Key,
                        Lessons = subSubjectGroup.OrderBy(l => l.Title).ToList()
                    };
                    
                    subjectNode.SubSubjects.Add(subSubjectNode);
                }
                
                subjects.Add(subjectNode);
            }

            return subjects;
        }

        public List<Lesson> GetLessonsBySubjectAndSubSubject(string subject, string subSubject)
        {
            var allLessons = LoadAllLessons();
            return allLessons
                .Where(l => l.Subject == subject && l.SubSubject == subSubject)
                .OrderBy(l => l.Title)
                .ToList();
        }

        public bool AddLesson(Lesson lesson, string sourceFilePath, string? pdfSourcePath = null)
        {
            try
            {
                // Generate unique filename for audio
                string fileExtension = Path.GetExtension(sourceFilePath);
                string uniqueFileName = $"{lesson.Id}{fileExtension}";
                string destinationPath = Path.Combine(_audioPath, uniqueFileName);

                // Copy audio file
                File.Copy(sourceFilePath, destinationPath, true);
                
                // Set file path and size
                lesson.FilePath = destinationPath;
                lesson.FileSize = new FileInfo(destinationPath).Length;

                // Handle PDF file if provided
                if (!string.IsNullOrEmpty(pdfSourcePath) && File.Exists(pdfSourcePath))
                {
                    string pdfExtension = Path.GetExtension(pdfSourcePath);
                    string uniquePdfName = $"{lesson.Id}_pdf{pdfExtension}";
                    string pdfDestinationPath = Path.Combine(_pdfPath, uniquePdfName);
                    
                    // Copy PDF file
                    File.Copy(pdfSourcePath, pdfDestinationPath, true);
                    
                    // Set PDF path and size
                    lesson.PdfPath = pdfDestinationPath;
                    lesson.PdfSize = new FileInfo(pdfDestinationPath).Length;
                    lesson.HasPdf = true;
                }

                // Add to metadata
                var lessons = LoadAllLessons();
                lessons.Add(lesson);
                SaveAllLessons(lessons);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding lesson: {ex.Message}");
                return false;
            }
        }

       
        public Lesson? GetLessonById(string lessonId)
        {
            var lessons = LoadAllLessons();
            return lessons.FirstOrDefault(l => l.Id == lessonId);
        }

        public string GetLessonAudioPath(string lessonId)
        {
            var lesson = GetLessonById(lessonId);
            return lesson?.FilePath ?? "";
        }
        // ���� �� ������ ��� �-LessonRepository.cs ���:

        // הוסף/החלף את המתודה UpdateLesson ב-LessonRepository.cs שלך

        public bool UpdateLesson(Lesson updatedLesson)
        {
            try
            {
                // טען את כל השיעורים
                var lessons = LoadAllLessons();

                // מצא את השיעור לעדכן
                var existingLesson = lessons.FirstOrDefault(l => l.Id == updatedLesson.Id);

                if (existingLesson == null)
                    return false;

                // עדכן את הפרטים
                existingLesson.Title = updatedLesson.Title;
                existingLesson.Subject = updatedLesson.Subject;
                existingLesson.SubSubject = updatedLesson.SubSubject;
                existingLesson.Year = updatedLesson.Year;

                // טיפול בקובץ שמע - אם שונה
                if (!string.IsNullOrEmpty(updatedLesson.FilePath) &&
                    updatedLesson.FilePath != existingLesson.FilePath &&
                    File.Exists(updatedLesson.FilePath))
                {
                    // מחק את הקובץ הישן אם קיים
                    if (File.Exists(existingLesson.FilePath))
                    {
                        try
                        {
                            File.Delete(existingLesson.FilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete old audio file: {ex.Message}");
                        }
                    }

                    // העתק את הקובץ החדש
                    string fileExtension = Path.GetExtension(updatedLesson.FilePath);
                    string uniqueFileName = $"{updatedLesson.Id}{fileExtension}";
                    string destinationPath = Path.Combine(_audioPath, uniqueFileName);

                    File.Copy(updatedLesson.FilePath, destinationPath, true);

                    existingLesson.FilePath = destinationPath;
                    existingLesson.FileSize = new FileInfo(destinationPath).Length;
                }

                // טיפול בקובץ PDF
                if (!string.IsNullOrEmpty(updatedLesson.PdfPath) &&
                    updatedLesson.PdfPath != existingLesson.PdfPath &&
                    File.Exists(updatedLesson.PdfPath))
                {
                    // מחק את קובץ ה-PDF הישן אם קיים
                    if (!string.IsNullOrEmpty(existingLesson.PdfPath) && File.Exists(existingLesson.PdfPath))
                    {
                        try
                        {
                            File.Delete(existingLesson.PdfPath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete old PDF file: {ex.Message}");
                        }
                    }

                    // העתק את קובץ ה-PDF החדש
                    string pdfExtension = Path.GetExtension(updatedLesson.PdfPath);
                    string uniquePdfName = $"{updatedLesson.Id}_pdf{pdfExtension}";
                    string pdfDestinationPath = Path.Combine(_pdfPath, uniquePdfName);

                    File.Copy(updatedLesson.PdfPath, pdfDestinationPath, true);

                    existingLesson.PdfPath = pdfDestinationPath;
                    existingLesson.PdfSize = new FileInfo(pdfDestinationPath).Length;
                    existingLesson.HasPdf = true;
                }
                else if (string.IsNullOrEmpty(updatedLesson.PdfPath) && existingLesson.HasPdf)
                {
                    // המשתמש הסיר את ה-PDF - מחק אותו
                    if (!string.IsNullOrEmpty(existingLesson.PdfPath) && File.Exists(existingLesson.PdfPath))
                    {
                        try
                        {
                            File.Delete(existingLesson.PdfPath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete PDF file: {ex.Message}");
                        }
                    }

                    existingLesson.PdfPath = "";
                    existingLesson.PdfSize = 0;
                    existingLesson.HasPdf = false;
                }

                // שמור את השינויים
                SaveAllLessons(lessons);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating lesson: {ex.Message}");
                return false;
            }
        }

        // גם עדכן את DeleteLesson כדי שימחק גם PDF
        public bool DeleteLesson(string lessonId)
        {
            try
            {
                var lessons = LoadAllLessons();
                var lessonToDelete = lessons.FirstOrDefault(l => l.Id == lessonId);

                if (lessonToDelete == null)
                    return false;

                // Delete audio file
                if (File.Exists(lessonToDelete.FilePath))
                {
                    File.Delete(lessonToDelete.FilePath);
                }

                // Delete PDF file if exists
                if (lessonToDelete.HasPdf && !string.IsNullOrEmpty(lessonToDelete.PdfPath) && File.Exists(lessonToDelete.PdfPath))
                {
                    try
                    {
                        File.Delete(lessonToDelete.PdfPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not delete PDF file: {ex.Message}");
                    }
                }

                // Remove from metadata
                lessons.Remove(lessonToDelete);
                SaveAllLessons(lessons);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting lesson: {ex.Message}");
                return false;
            }
        }

        // �� ��� �� ����� SaveData, ���� �� ����:
        private void SaveData()
        {
            try
            {
                var json = JsonSerializer.Serialize(_dataPath, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex.Message}");
            }
        }
        private List<Lesson> LoadAllLessons()
        {
            try
            {
                if (!File.Exists(_metadataPath))
                    return new List<Lesson>();

                var json = File.ReadAllText(_metadataPath);
                return JsonSerializer.Deserialize<List<Lesson>>(json) ?? new List<Lesson>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading lessons: {ex.Message}");
                return new List<Lesson>();
            }
        }

        private void SaveAllLessons(List<Lesson> lessons)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(lessons, options);
                File.WriteAllText(_metadataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving lessons: {ex.Message}");
            }
        }

        public List<string> GetAvailableSubjects()
        {
            var lessons = LoadAllLessons();
            return lessons.Select(l => l.Subject).Distinct().OrderBy(s => s).ToList();
        }

        public List<string> GetAvailableSubSubjects(string subject)
        {
            var lessons = LoadAllLessons();
            return lessons
                .Where(l => l.Subject == subject)
                .Select(l => l.SubSubject)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
        }
    }
}
