using GongSolutions.Wpf.DragDrop;
using PIModTool.Core.ViewModels.Components;
using PIModTool.Lib.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PIModTool.Wpf.Views.Components
{
    /// <summary>
    /// Interaction logic for FileTreeNodeView.xaml
    /// </summary>
    public partial class FileTreeNodeView : UserControl, IDropTarget
    {
        public static readonly DependencyProperty FilesProperty = DependencyProperty.Register(nameof(Files), typeof(IList), typeof(FileTreeNodeView), new PropertyMetadata(null, OnFilesChanged));

        public IList? Files
        {
            get => (IList?)GetValue(FilesProperty);
            set => SetValue(FilesProperty, value);
        }

        public static readonly DependencyProperty SelectedFileProperty = DependencyProperty.Register(nameof(SelectedFile), typeof(GenericFile), typeof(FileTreeNodeView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFileChanged));
        public GenericFile? SelectedFile
        {
            get => (GenericFile?)GetValue(SelectedFileProperty);
            set => SetValue(SelectedFileProperty, value);
        }

        // Update PropertyMetadata if I ennd up needing to change this during runtime in the future
        public static readonly DependencyProperty EnableContextMenuProperty = DependencyProperty.Register(nameof(EnableContextMenu), typeof(bool), typeof(FileTreeNodeView), new PropertyMetadata(null));

        public bool EnableContextMenu
        {
            get => (bool)GetValue(EnableContextMenuProperty);
            set => SetValue(EnableContextMenuProperty, value);
        }

        public static readonly DependencyProperty NewFileCommandProperty = DependencyProperty.Register(nameof(NewFileCommand), typeof(ICommand), typeof(FileTreeNodeView), new PropertyMetadata(null));
        public ICommand? NewFileCommand
        {
            get => (ICommand?)GetValue(NewFileCommandProperty);
            set => SetValue(NewFileCommandProperty, value);
        }

        public static readonly DependencyProperty ExportFileCommandProperty = DependencyProperty.Register(nameof(ExportFileCommand), typeof(ICommand), typeof(FileTreeNodeView), new PropertyMetadata(null));
        public ICommand? ExportFileCommand
        {
            get => (ICommand?)GetValue(ExportFileCommandProperty);
            set => SetValue(ExportFileCommandProperty, value);
        }

        public static readonly DependencyProperty EnableRenameProperty = DependencyProperty.Register(nameof(EnableRename), typeof(bool), typeof(FileTreeNodeView), new PropertyMetadata(true));

        public bool EnableRename
        {
            get => (bool)GetValue(EnableRenameProperty);
            set => SetValue(EnableRenameProperty, value);
        }

        public ObservableCollection<FileTreeNodeViewModel> RootNodes { get; } = new();
        private FileTreeNodeViewModel? _renamingNode;
        private string _renameOriginalName = string.Empty;
        private bool _suppressSelectionSync; // Disable sync temporarily while updating state

        public FileTreeNodeView()
        {
            InitializeComponent();
            FileTree.DataContext = this;
            GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(FileTree, this);
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(FileTree, true);
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(FileTree, true);
        }

        private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildTree();
        }

        private static void OnFilesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FileTreeNodeView ctrl = (FileTreeNodeView)d;

            if (e.OldValue is INotifyCollectionChanged oldVal)
            {
                oldVal.CollectionChanged -= ctrl.Files_CollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newVal)
            {
                newVal.CollectionChanged += ctrl.Files_CollectionChanged;
            }

            ctrl.RebuildTree();
        }

        private static void OnSelectedFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FileTreeNodeView ctrl = (FileTreeNodeView)d;
            if (!ctrl._suppressSelectionSync)
            {
                ctrl.SyncSelectionToTree(e.NewValue as GenericFile);
            }
        }

        private void RebuildTree()
        {
            HashSet<string> expanded = CollectExpandedPaths(RootNodes);
            RootNodes.Clear();

            if (Files == null) return;

            Dictionary<string, FileTreeNodeViewModel> folderCache = new Dictionary<string, FileTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase);

            FileTreeNodeViewModel EnsureFolder(string folderPath)
            {
                if (folderCache.TryGetValue(folderPath, out FileTreeNodeViewModel? cached))
                {
                    return cached;
                }

                FileTreeNodeViewModel node = new FileTreeNodeViewModel(folderPath)
                {
                    IsExpanded = expanded.Contains(folderPath)
                };
                folderCache[folderPath] = node;

                int lastSlash = folderPath.LastIndexOf('/');
                if (lastSlash <= 0)
                {
                    RootNodes.Add(node);
                }
                else
                {
                    EnsureFolder(folderPath[..lastSlash]).Children.Add(node);
                }

                return node;
            }

            foreach (GenericFile? item in Files)
            {
                if (item is not GenericFile file)
                {
                    continue;
                }

                string normalized = file.Path.Replace('\\', '/');
                int lastSlash = normalized.LastIndexOf('/');
                FileTreeNodeViewModel fileNode = new FileTreeNodeViewModel(file);

                if (lastSlash < 0)
                {
                    RootNodes.Add(fileNode);
                }
                else
                {
                    EnsureFolder(normalized[..lastSlash]).Children.Add(fileNode);
                }
            }

            SortNodes(RootNodes);
        }

        private static void SortNodes(ObservableCollection<FileTreeNodeViewModel> nodes)
        {
            List<FileTreeNodeViewModel>? sorted = nodes.OrderByDescending(n => n.IsFolder).ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();

            nodes.Clear();
            foreach (FileTreeNodeViewModel n in sorted)
            {
                nodes.Add(n);
            }
            foreach (FileTreeNodeViewModel folder in nodes.Where(n => n.IsFolder))
            {
                SortNodes(folder.Children);
            }
        }

        private static HashSet<string> CollectExpandedPaths(IEnumerable<FileTreeNodeViewModel> nodes)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FileTreeNodeViewModel n in nodes)
            {
                if (!n.IsFolder || !n.IsExpanded)
                {
                    continue;
                }
                result.Add(n.FolderPath);
                result.UnionWith(CollectExpandedPaths(n.Children));
            }
            return result;
        }

        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _suppressSelectionSync = true;
            SelectedFile = e.NewValue is FileTreeNodeViewModel { IsFolder: false } node ? node.File : null;
            _suppressSelectionSync = false;
        }

        private static void ClearAllSelection(IEnumerable<FileTreeNodeViewModel> nodes)
        {
            foreach (FileTreeNodeViewModel n in nodes) { 
                n.IsSelected = false; 
                ClearAllSelection(n.Children); 
            }
        }

        private static FileTreeNodeViewModel? FindFileNode(IEnumerable<FileTreeNodeViewModel> nodes, GenericFile target)
        {
            foreach (FileTreeNodeViewModel n in nodes)
            {
                if (!n.IsFolder && n.File == target)
                {
                    return n;
                }
                FileTreeNodeViewModel? found = FindFileNode(n.Children, target);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void SyncSelectionToTree(GenericFile? file)
        {
            ClearAllSelection(RootNodes);
            if (file == null)
            {
                return;
            }
            FileTreeNodeViewModel? node = FindFileNode(RootNodes, file);
            if (node != null)
            {
                node.IsSelected = true;
            }
        }

        private void BeginRename(FileTreeNodeViewModel node)
        {
            if (_renamingNode != null)
            {
                CommitRename(_renamingNode);
            }
            _renamingNode = node;
            _renameOriginalName = node.Name;
            node.IsRenaming = true;
        }

        private void CommitRename(FileTreeNodeViewModel node)
        {
            if (!node.IsRenaming)
            {
                return;
            }
            node.IsRenaming = false;

            string newName = node.Name.Trim();
            if (string.IsNullOrEmpty(newName)) { 
                CancelRename(node); 
                return; 
            }

            if (node.IsFolder)
            {
                string oldPath = node.FolderPath;
                int lastSlash = oldPath.LastIndexOf('/');
                string parentPath = lastSlash < 0 ? string.Empty : oldPath[..lastSlash];
                string newFolderPath = string.IsNullOrEmpty(parentPath) ? newName : parentPath + "/" + newName;
                ApplyFolderRename(node, oldPath, newFolderPath);
            }
            else
            {
                string oldPath = node.File!.Path.Replace('\\', '/');
                int lastSlash = oldPath.LastIndexOf('/');
                node.File.Path = lastSlash < 0 ? newName : oldPath[..lastSlash] + "/" + newName;
            }

            _renamingNode = null;
        }

        private void CancelRename(FileTreeNodeViewModel node)
        {
            node.Name = _renameOriginalName;
            node.IsRenaming = false;
            _renamingNode = null;
        }

        private static void ApplyFolderRename(FileTreeNodeViewModel folder, string oldFolderPath, string newFolderPath)
        {
            folder.SetFolderPath(newFolderPath);
            foreach (FileTreeNodeViewModel child in folder.Children)
            {
                if (child.IsFolder)
                {
                    ApplyFolderRename(child, child.FolderPath,
                        newFolderPath + "/" + child.Name);
                }
                else
                {
                    string oldFile = child.File!.Path.Replace('\\', '/');
                    child.File.Path = newFolderPath + oldFile[oldFolderPath.Length..];
                }
            }
        }

        private void RenameBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Focus();
            tb.SelectAll();
        }

        private void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox { DataContext: FileTreeNodeViewModel node })
            {
                if (e.Key == Key.Return) { 
                    CommitRename(node); 
                    e.Handled = true; 
                }
                else if (e.Key == Key.Escape) { 
                    CancelRename(node); 
                    e.Handled = true; 
                }
            }
        }

        private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox { DataContext: FileTreeNodeViewModel node })
            {
                CommitRename(node);
            }
        }

        private static FileTreeNodeViewModel? GetContextMenuNode(object sender)
        {
            if(sender is MenuItem { Parent: ContextMenu { PlacementTarget: FrameworkElement fe } })
            {
                return fe.DataContext as FileTreeNodeViewModel;
            }
            return null;
        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileTreeNodeViewModel? node = GetContextMenuNode(sender);
            if (node != null)
            {
                BeginRename(node);
            }
        }

        private void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileTreeNodeViewModel? node = GetContextMenuNode(sender);
            if (node == null || node.IsFolder) { 
                return; 
            }
            ExportFileCommand?.Execute(node.File);
        }

        private void NewFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewFileCommand?.Execute(null);
        }

        // GongSolutions.Wpf.DragDrop functionality
        public void DragOver(IDropInfo dropInfo)
        {
            if (!EnableRename || dropInfo.Data is not FileTreeNodeViewModel source)
            {
                return;
            }

            FileTreeNodeViewModel? targetFolder = ResolveDropTargetFolder(dropInfo);

            // Block: dropping into itself or a descendant
            if (source.IsFolder && (targetFolder == source ||
                (targetFolder != null && IsDescendant(targetFolder, source)))) return;

            // Block: no-op move (already in this folder)
            if (GetParentFolder(RootNodes, source) == targetFolder) return;

            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Move;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not FileTreeNodeViewModel source)
            {
                return;
            }

            FileTreeNodeViewModel? targetFolder = ResolveDropTargetFolder(dropInfo);

            // Remove from current parent
            ObservableCollection<FileTreeNodeViewModel>? sourceParent = GetParentCollection(RootNodes, source);
            sourceParent?.Remove(source);

            // Update all affected file paths
            ApplyPathsForMove(source, targetFolder?.FolderPath ?? string.Empty);

            // Insert into new parent and re-sort
            ObservableCollection<FileTreeNodeViewModel> targetCollection = targetFolder?.Children ?? RootNodes;
            targetCollection.Add(source);
            SortNodes(targetCollection);

            if (targetFolder != null)
            {
                targetFolder.IsExpanded = true;
            }
        }

        private FileTreeNodeViewModel? ResolveDropTargetFolder(IDropInfo dropInfo)
        {
            if (dropInfo.TargetItem is FileTreeNodeViewModel { IsFolder: true } folder)
            {
                return folder;
            }

            if (dropInfo.TargetItem is FileTreeNodeViewModel { IsFolder: false } file)
            {
                return GetParentFolder(RootNodes, file); // same folder as target file
            }

            return null; // root
        }

        private static void ApplyPathsForMove(FileTreeNodeViewModel node, string newParentPath)
        {
            if (node.IsFolder)
            {
                string newFolderPath = string.IsNullOrEmpty(newParentPath) ? node.Name : newParentPath + "/" + node.Name;
                ApplyFolderRename(node, node.FolderPath, newFolderPath);
            }
            else
            {
                node.File!.Path = string.IsNullOrEmpty(newParentPath) ? node.Name : newParentPath + "/" + node.Name;
            }
        }

        private static bool IsDescendant(FileTreeNodeViewModel candidate, FileTreeNodeViewModel ancestor)
        {
            foreach (FileTreeNodeViewModel child in ancestor.Children)
            {
                if (child == candidate || (child.IsFolder && IsDescendant(candidate, child)))
                {
                    return true;
                }
            }
                    
            return false;
        }

        private ObservableCollection<FileTreeNodeViewModel>? GetParentCollection(ObservableCollection<FileTreeNodeViewModel> nodes, FileTreeNodeViewModel target)
        {
            if (nodes.Contains(target))
            {
                return nodes;
            }
            foreach (FileTreeNodeViewModel n in nodes)
            {
                ObservableCollection<FileTreeNodeViewModel>? found = GetParentCollection(n.Children, target);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static FileTreeNodeViewModel? GetParentFolder(ObservableCollection<FileTreeNodeViewModel> nodes, FileTreeNodeViewModel target)
        {
            foreach (FileTreeNodeViewModel n in nodes)
            {
                if (n.IsFolder && n.Children.Contains(target))
                {
                    return n;
                }
                FileTreeNodeViewModel? found = GetParentFolder(n.Children, target);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
