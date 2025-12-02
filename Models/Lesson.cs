namespace LessonsManager.Models
{
    public class Lesson
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string SubSubject { get; set; } = "";
        public string Year { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string PdfPath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public long FileSize { get; set; } = 0;
        public long PdfSize { get; set; } = 0;
        public bool HasPdf { get; set; } = false;
    }

    public class SubjectNode
    {
        public string Name { get; set; } = "";
        public List<SubSubjectNode> SubSubjects { get; set; } = new();
    }

    public class SubSubjectNode
    {
        public string Name { get; set; } = "";
        public List<Lesson> Lessons { get; set; } = new();
    }
}
