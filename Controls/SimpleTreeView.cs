using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LessonsManager.Models;

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
            if (d is SimpleTreeView treeView && e.NewValue != null)
            {
                treeView.RefreshDisplay();
            }
        }

        private static void OnCurrentFolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleTreeView treeView)
            {
                treeView.RefreshDisplay();
                treeView.FolderChanged?.Invoke(treeView, EventArgs.Empty);
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
            if (item != null)
            {
                if (item.Name == "..")
                {
                    NavigateBack();
                }
                else if (item.IsFolder)
                {
                    CurrentFolder = item;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var item = GetItemFromPoint(e.GetPosition(this));
                if (item != null && !item.Name.Equals(".."))
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
            if (TreeItems == null || TreeItems.Count == 0)
            {
                this.ItemsSource = new ObservableCollection<TreeItem>();
                return;
            }

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
                    var pathParts = CurrentFolder.FullPath.Split('/');
                    var parentPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
                    var parent = TreeItems.FirstOrDefault(x => x.FullPath == parentPath);

                    // Add back navigation item
                    displayItems.Add(new TreeItem("..", parentPath, "0", true, CurrentFolder.Level - 1));
                }
                else
                {
                    // At level 1, back goes to root
                    displayItems.Add(new TreeItem("..", "", "0", true, -1));
                }

                // Show items in current folder
                var currentPath = CurrentFolder.FullPath;
                var currentLevel = CurrentFolder.Level;

                var currentItems = TreeItems.Where(x =>
                {
                    if (!x.FullPath.StartsWith(currentPath + "/"))
                        return false;

                    var relativePath = x.FullPath.Substring(currentPath.Length + 1);
                    return !relativePath.Contains("/"); // Direct children only
                });

                foreach (var item in currentItems.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name))
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
                var pathParts = CurrentFolder.FullPath.Split('/');
                var parentPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
                var parent = TreeItems?.FirstOrDefault(x => x.FullPath == parentPath);
                CurrentFolder = parent;
            }
            else
            {
                NavigateToRoot();
            }
        }

        public void NavigateToFolder(TreeItem folder)
        {
            if (folder != null && folder.IsFolder)
            {
                CurrentFolder = folder;
            }
        }

        public ObservableCollection<string> GetBreadcrumbPath()
        {
            var breadcrumb = new ObservableCollection<string>();

            if (CurrentFolder != null)
            {
                var parts = CurrentFolder.FullPath.Split('/');
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        breadcrumb.Add(part);
                    }
                }
            }

            return breadcrumb;
        }
    }
}