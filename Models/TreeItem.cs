using System.Collections.ObjectModel;
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
        
        // New properties for hierarchical structure
        public TreeItem? Parent { get; set; } = null;
        public ObservableCollection<TreeItem>? Children { get; set; } = null;
        public string Subtitle { get; set; } = "";

        // Default constructor for XAML binding
        public TreeItem()
        {
            Children = new ObservableCollection<TreeItem>();
        }

        // Legacy constructor for backward compatibility
        public TreeItem(string name, string fullPath, string id, bool isFolder, int level = 0, Lesson? lesson = null)
        {
            Name = name;
            FullPath = fullPath;
            Id = id;
            IsFolder = isFolder;
            Level = level;
            Lesson = lesson;
            Children = new ObservableCollection<TreeItem>();
        }

        // Helper method to set parent relationship
        public void SetParent(TreeItem parent)
        {
            Parent = parent;
            if (parent != null && !parent.Children.Contains(this))
            {
                parent.Children.Add(this);
            }
        }
    }
}
