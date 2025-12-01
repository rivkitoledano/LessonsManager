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
        
        private readonly string _dataPath;
        private readonly string _metadataPath;
        private readonly string _audioPath;

        public LessonRepository()
        {
            _dataPath = Path.Combine(Environment.CurrentDirectory, DATA_FOLDER);
            _metadataPath = Path.Combine(_dataPath, METADATA_FILE);
            _audioPath = Path.Combine(_dataPath, AUDIO_FOLDER);
            
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);
            
            if (!Directory.Exists(_audioPath))
                Directory.CreateDirectory(_audioPath);
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

        public bool AddLesson(Lesson lesson, string sourceFilePath)
        {
            try
            {
                // Generate unique filename
                string fileExtension = Path.GetExtension(sourceFilePath);
                string uniqueFileName = $"{lesson.Id}{fileExtension}";
                string destinationPath = Path.Combine(_audioPath, uniqueFileName);

                // Copy audio file
                File.Copy(sourceFilePath, destinationPath, true);
                
                // Set file path and size
                lesson.FilePath = destinationPath;
                lesson.FileSize = new FileInfo(destinationPath).Length;

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
