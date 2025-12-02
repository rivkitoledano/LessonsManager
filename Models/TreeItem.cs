using System.Windows;
using System.Linq;

namespace LessonsManager.Models
{
    public class TreeItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Id { get; set; } = "";
        public bool IsFolder { get; set; }
        public bool IsExpanded { get; set; } = false;
        public int Level { get; set; } = 0;
        public Lesson? Lesson { get; set; } = null;
        public bool HasChildren { get; set; } = false;

        public TreeItem(string name, string fullPath, string id, bool isFolder, int level = 0, Lesson? lesson = null)
        {
            Name = name;
            FullPath = fullPath;
            Id = id;
            IsFolder = isFolder;
            Level = level;
            Lesson = lesson;
        }
    }
}
