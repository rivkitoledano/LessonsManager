using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LessonsManager.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace LessonsManager.Controls
{
    public class SimpleTreeView : ListBox
    {
        public ObservableCollection<TreeItem> TreeItems
        {
            get { return (ObservableCollection<TreeItem>)GetValue(TreeItemsProperty); }
            set { SetValue(TreeItemsProperty, value); }
        }

        public static readonly DependencyProperty TreeItemsProperty =
            DependencyProperty.Register(nameof(TreeItems), typeof(ObservableCollection<TreeItem>), 
                typeof(SimpleTreeView), new PropertyMetadata(null, OnTreeItemsChanged));

        public event EventHandler? FolderChanged;

        public TreeItem? CurrentFolder
        {
            get { return (TreeItem)GetValue(CurrentFolderProperty); }
            set { SetValue(CurrentFolderProperty, value); }
        }

        public static readonly DependencyProperty CurrentFolderProperty =
            DependencyProperty.Register(nameof(CurrentFolder), typeof(TreeItem), 
                typeof(SimpleTreeView), new PropertyMetadata(null, OnCurrentFolderChanged));

        private static void OnTreeItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleTreeView treeView)
            {
                treeView.RefreshDisplay();
            }
        }

        private static void OnCurrentFolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleTreeView treeView)
            {
                treeView.RefreshDisplay();
            }
        }

        public SimpleTreeView()
        {
            this.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 232, 237));
            this.Padding = new Thickness(5);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            
            var item = GetItemFromPoint(e.GetPosition(this));
            if (item != null && item.IsFolder)
            {
                CurrentFolder = item;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var item = GetItemFromPoint(e.GetPosition(this));
                if (item != null)
                {
                    DragDrop.DoDragDrop(this, item, DragDropEffects.Copy);
                }
            }
        }

        private TreeItem? GetItemFromPoint(Point point)
        {
            var element = this.InputHitTest(point) as FrameworkElement;
            while (element != null && element != this)
            {
                if (element is ListBoxItem listBoxItem && listBoxItem.DataContext is TreeItem treeItem)
                {
                    return treeItem;
                }
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return null;
        }

        private void RefreshDisplay()
        {
            var displayItems = new ObservableCollection<TreeItem>();
            
            if (CurrentFolder == null)
            {
                // Show root folders
                var rootFolders = TreeItems.Where(x => x.Level == 0 && x.IsFolder);
                foreach (var folder in rootFolders)
                {
                    displayItems.Add(folder);
                }
            }
            else
            {
                // Show parent folder (for navigation back)
                if (CurrentFolder.Level > 0)
                {
                    var parentPath = string.Join("/", CurrentFolder.FullPath.Split('/').Take(CurrentFolder.FullPath.Split('/').Length - 1));
                    var parent = TreeItems.FirstOrDefault(x => x.FullPath == parentPath);
                    if (parent != null)
                    {
                        displayItems.Add(new TreeItem("..", parent.FullPath, parent.Id, true, parent.Level - 1));
                    }
                }
                
                // Show items in current folder
                var currentItems = TreeItems.Where(x => 
                    x.FullPath.StartsWith(CurrentFolder.FullPath + "/") && 
                    x.FullPath.Split('/').Length == CurrentFolder.FullPath.Split('/').Length + 1);
                
                foreach (var item in currentItems)
                {
                    displayItems.Add(item);
                }
            }
            
            this.ItemsSource = displayItems;
        }

        public void NavigateToRoot()
        {
            CurrentFolder = null;
        }

        public void NavigateBack()
        {
            if (CurrentFolder != null && CurrentFolder.Level > 0)
            {
                var parentPath = string.Join("/", CurrentFolder.FullPath.Split('/').Take(CurrentFolder.FullPath.Split('/').Length - 1));
                var parent = TreeItems.FirstOrDefault(x => x.FullPath == parentPath);
                CurrentFolder = parent;
            }
            else
            {
                NavigateToRoot();
            }
        }
    }
}
