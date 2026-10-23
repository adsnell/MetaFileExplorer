using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Linq;

namespace MetaFileExplorer
{
    public partial class MainWindow : Window
    {
        private string currentPath = @"C:\";
        private List<string> navigationHistory = new List<string>();
        private int historyIndex = -1;
        private string clipboardPath = string.Empty;
        private bool isCutOperation = false;
        private List<FileItemViewModel> currentFolderItems = new List<FileItemViewModel>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileSystem();
            NavigateTo(@"C:\");
        }

        private void InitializeFileSystem()
        {
            FolderTree.Items.Clear();

            string oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
            if (string.IsNullOrEmpty(oneDrivePath))
            {
                oneDrivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
            }

            if (Directory.Exists(oneDrivePath))
            {
                var oneDriveItem = new TreeViewItem
                {
                    Header = "OneDrive",
                    Tag = oneDrivePath
                };
                oneDriveItem.Items.Add(null);
                oneDriveItem.Expanded += FolderItem_Expanded;
                FolderTree.Items.Add(oneDriveItem);
            }

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    var item = new TreeViewItem
                    {
                        Header = drive.Name,
                        Tag = drive.RootDirectory.FullName
                    };
                    item.Items.Add(null);
                    item.Expanded += FolderItem_Expanded;
                    FolderTree.Items.Add(item);
                }
            }
        }

        private void FolderItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                string path = item.Tag.ToString();
                try
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        var subItem = new TreeViewItem
                        {
                            Header = dirInfo.Name,
                            Tag = dirInfo.FullName
                        };
                        subItem.Items.Add(null);
                        subItem.Expanded += FolderItem_Expanded;
                        item.Items.Add(subItem);
                    }
                }
                catch { }
            }
        }

        private void NavigateTo(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                currentPath = path;

                if (TxtAddressBar != null)
                {
                    TxtAddressBar.Text = currentPath;
                }

                if (TxtSearchBox != null)
                {
                    TxtSearchBox.Text = string.Empty;
                }

                if (historyIndex < navigationHistory.Count - 1)
                {
                    navigationHistory.RemoveRange(historyIndex + 1, navigationHistory.Count - (historyIndex + 1));
                }
                navigationHistory.Add(path);
                historyIndex = navigationHistory.Count - 1;

                LoadContents(path);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error navigating: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtAddressBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string targetPath = TxtAddressBar.Text.Trim();
                if (Directory.Exists(targetPath))
                {
                    NavigateTo(targetPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("Directory does not exist: " + targetPath, "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void LoadContents(string path)
        {
            currentFolderItems.Clear();
            FilesGrid.ItemsSource = null;
            ThumbnailsList.ItemsSource = null;
            StatusCount.Content = "Loading...";
            StatusSize.Content = "";

            var tempItems = new List<FileItemViewModel>();
            long totalSize = 0;
            int count = 0;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(path))
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            tempItems.Add(new FileItemViewModel
                            {
                                Name = dirInfo.Name,
                                FullPath = dirInfo.FullName,
                                Size = "<DIR>",
                                LastModified = dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                IsFolder = true,
                                CardColor = "#FFA726",
                                Title = string.Empty,
                                Subject = string.Empty,
                                Authors = string.Empty,
                                Tags = string.Empty,
                                Comments = string.Empty,
                                Rating = string.Empty,
                                HasThumbnail = false,
                                ThumbnailImage = null
                            });
                            count++;
                        }
                    }
                    catch { }

                    try
                    {
                        foreach (var file in Directory.GetFiles(path))
                        {
                            FileInfo fileInfo;
                            try { fileInfo = new FileInfo(file); } catch { continue; }
                            
                            totalSize += fileInfo.Length;
                            string ext = fileInfo.Extension.TrimStart('.').ToUpper();
                            
                            var vm = new FileItemViewModel
                            {
                                Name = fileInfo.Name,
                                FullPath = fileInfo.FullName,
                                Size = FormatFileSize(fileInfo.Length),
                                LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                IsFolder = false,
                                FileExtension = ext,
                                CardColor = "#42A5F5",
                                Title = string.Empty,
                                Subject = string.Empty,
                                Authors = string.Empty,
                                Tags = string.Empty,
                                Comments = string.Empty,
                                Rating = string.Empty,
                                HasThumbnail = false,
                                ThumbnailImage = null
                            };

                            // Quick non-blocking metadata scan
                            try { ExtractEmbeddedMetadataAndThumbnail(file, vm); } catch { }

                            tempItems.Add(vm);
                            count++;
                        }
                    }
                    catch { }
                });

                currentFolderItems = tempItems;
                FilesGrid.ItemsSource = currentFolderItems;
                ThumbnailsList.ItemsSource = currentFolderItems;

                StatusCount.Content = $"{count} items";
                StatusSize.Content = $"{FormatFileSize(totalSize)} total";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Access denied or error reading directory: " + ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchBox == null) return;
            string query = TxtSearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(query))
            {
                FilesGrid.ItemsSource = currentFolderItems;
                ThumbnailsList.ItemsSource = currentFolderItems;
                StatusCount.Content = $"{currentFolderItems.Count} items";
            }
            else
            {
                var filtered = currentFolderItems.Where(x => 
                    x.Name.ToLower().Contains(query) || 
                    x.Title.ToLower().Contains(query) || 
                    x.Tags.ToLower().Contains(query) || 
                    x.Authors.ToLower().Contains(query)
                ).ToList();

                FilesGrid.ItemsSource = filtered;
                ThumbnailsList.ItemsSource = filtered;
                StatusCount.Content = $"{filtered.Count} items (filtered)";
            }
        }

        private void ExtractEmbeddedMetadataAndThumbnail(string filePath, FileItemViewModel vm)
        {
            string ext = vm.FileExtension.ToUpper();
            try
            {
                if (ext == "JPG" || ext == "JPEG" || ext == "PNG" || ext == "BMP" || ext == "GIF" || ext == "WEBP" || ext == "TIFF" || ext == "TIF")
                {
                    byte[] fileBytes;
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        using (var msTemp = new MemoryStream())
                        {
                            fs.CopyTo(msTemp);
                            fileBytes = msTemp.ToArray();
                        }
                    }

                    using (var ms = new MemoryStream(fileBytes))
                    {
                        var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                        if (decoder.Frames.Count > 0)
                        {
                            var frame = decoder.Frames[0];
                            if (frame.Metadata is BitmapMetadata meta)
                            {
                                if (!string.IsNullOrEmpty(meta.Title)) vm.Title = meta.Title;
                                if (!string.IsNullOrEmpty(meta.Subject)) vm.Subject = meta.Subject;
                                if (meta.Author != null && meta.Author.Count > 0) vm.Authors = string.Join(", ", meta.Author);
                                if (meta.Keywords != null && meta.Keywords.Count > 0) vm.Tags = string.Join(", ", meta.Keywords);
                                if (!string.IsNullOrEmpty(meta.Comment)) vm.Comments = meta.Comment;
                                if (meta.Rating > 0) vm.Rating = meta.Rating.ToString();
                            }
                        }
                    }

                    var thumb = new BitmapImage();
                    thumb.BeginInit();
                    thumb.UriSource = new Uri(filePath);
                    thumb.CacheOption = BitmapCacheOption.OnLoad;
                    thumb.DecodePixelWidth = 140;
                    thumb.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    thumb.EndInit();
                    thumb.Freeze();

                    vm.ThumbnailImage = thumb;
                    vm.HasThumbnail = true;
                }
                else if (ext == "DOCX" || ext == "XLSX" || ext == "PPTX")
                {
                    using (var archive = ZipFile.OpenRead(filePath))
                    {
                        var corePropsEntry = archive.GetEntry("docProps/core.xml");
                        if (corePropsEntry != null)
                        {
                            using (var sr = new StreamReader(corePropsEntry.Open()))
                            {
                                string xmlContent = sr.ReadToEnd();
                                var xdoc = XDocument.Parse(xmlContent);

                                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                                XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";

                                var titleElem = xdoc.Root?.Element(dc + "title");
                                var subjectElem = xdoc.Root?.Element(dc + "subject");
                                var creatorElem = xdoc.Root?.Element(dc + "creator");
                                var descriptionElem = xdoc.Root?.Element(dc + "description");
                                var keywordsElem = xdoc.Root?.Element(cp + "keywords");

                                if (titleElem != null && !string.IsNullOrEmpty(titleElem.Value)) vm.Title = titleElem.Value;
                                if (subjectElem != null && !string.IsNullOrEmpty(subjectElem.Value)) vm.Subject = subjectElem.Value;
                                if (creatorElem != null && !string.IsNullOrEmpty(creatorElem.Value)) vm.Authors = creatorElem.Value;
                                if (descriptionElem != null && !string.IsNullOrEmpty(descriptionElem.Value)) vm.Comments = descriptionElem.Value;
                                if (keywordsElem != null && !string.IsNullOrEmpty(keywordsElem.Value)) vm.Tags = keywordsElem.Value;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is TreeViewItem item && item.Tag is string path)
            {
                if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
            }
        }

        private void FilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = FilesGrid.SelectedItems;
            if (selectedItems.Count == 1 && selectedItems[0] is FileItemViewModel selected)
            {
                if (ThumbnailsList.SelectedItem != selected)
                    ThumbnailsList.SelectedItem = selected;

                PopulateMetadata(selected);
            }
            else if (selectedItems.Count > 1)
            {
                TxtMetaFileName.Text = $"<{selectedItems.Count} items selected>";
                TxtMetaTitle.Text = string.Empty;
                TxtMetaSubject.Text = string.Empty;
                TxtMetaAuthors.Text = string.Empty;
                TxtMetaTags.Text = string.Empty;
                TxtMetaComments.Text = string.Empty;
                TxtMetaRating.Text = string.Empty;
                TxtSysPath.Text = $"Path: Multiple items selected ({selectedItems.Count})";
                TxtSysType.Text = "Type: Batch Selection";
                TxtSysCreated.Text = "Created: -";
                TxtSysSize.Text = "Size: -";
            }
        }

        private void ThumbnailsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ThumbnailsList.SelectedItems;
            if (selectedItems.Count == 1 && selectedItems[0] is FileItemViewModel selected)
            {
                if (FilesGrid.SelectedItem != selected)
                    FilesGrid.SelectedItem = selected;

                PopulateMetadata(selected);
            }
            else if (selectedItems.Count > 1)
            {
                TxtMetaFileName.Text = $"<{selectedItems.Count} items selected>";
                TxtMetaTitle.Text = string.Empty;
                TxtMetaSubject.Text = string.Empty;
                TxtMetaAuthors.Text = string.Empty;
                TxtMetaTags.Text = string.Empty;
                TxtMetaComments.Text = string.Empty;
                TxtMetaRating.Text = string.Empty;
                TxtSysPath.Text = $"Path: Multiple items selected ({selectedItems.Count})";
                TxtSysType.Text = "Type: Batch Selection";
                TxtSysCreated.Text = "Created: -";
                TxtSysSize.Text = "Size: -";
            }
        }

        private void FilesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FilesGrid.SelectedItem is FileItemViewModel selected)
            {
                if (selected.IsFolder)
                {
                    NavigateTo(selected.FullPath);
                }
                else
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selected.FullPath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ThumbnailsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ThumbnailsList.SelectedItem is FileItemViewModel selected)
            {
                if (selected.IsFolder)
                {
                    NavigateTo(selected.FullPath);
                }
                else
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selected.FullPath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (historyIndex > 0)
            {
                historyIndex--;
                currentPath = navigationHistory[historyIndex];
                LoadContents(currentPath);
            }
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            if (historyIndex < navigationHistory.Count - 1)
            {
                historyIndex++;
                currentPath = navigationHistory[historyIndex];
                LoadContents(currentPath);
            }
        }

        private void BtnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            string newFolderPath = Path.Combine(currentPath, "New Folder");
            int counter = 1;
            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(currentPath, $"New Folder ({counter++})");
            }
            try
            {
                Directory.CreateDirectory(newFolderPath);
                LoadContents(currentPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Could not create folder: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItems : ThumbnailsList.SelectedItems;
            if (selectedItems.Count == 0) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete {selectedItems.Count} selected item(s)?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (FileItemViewModel item in selectedItems.Cast<FileItemViewModel>().ToList())
                    {
                        if (item.IsFolder) Directory.Delete(item.FullPath, true);
                        else File.Delete(item.FullPath);
                    }
                    LoadContents(currentPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Batch delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCut_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItem : ThumbnailsList.SelectedItem;
            if (selectedItem is FileItemViewModel item)
            {
                clipboardPath = item.FullPath;
                isCutOperation = true;
                System.Windows.MessageBox.Show($"Clipped for cut: {item.Name}", "Cut", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItem : ThumbnailsList.SelectedItem;
            if (selectedItem is FileItemViewModel item)
            {
                clipboardPath = item.FullPath;
                isCutOperation = false;
                System.Windows.MessageBox.Show($"Clipped for copy: {item.Name}", "Copy", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(clipboardPath)) return;
            try
            {
                string fileName = Path.GetFileName(clipboardPath);
                string destPath = Path.Combine(currentPath, fileName);

                if (File.Exists(clipboardPath))
                {
                    File.Copy(clipboardPath, destPath, true);
                    if (isCutOperation)
                    {
                        File.Delete(clipboardPath);
                        clipboardPath = string.Empty;
                    }
                }
                else if (Directory.Exists(clipboardPath))
                {
                    CopyDirectory(clipboardPath, destPath);
                    if (isCutOperation)
                    {
                        Directory.Delete(clipboardPath, true);
                        clipboardPath = string.Empty;
                    }
                }
                LoadContents(currentPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Paste failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        private void BtnCopyTo_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItems : ThumbnailsList.SelectedItems;
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select item(s) to copy first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = $"Select destination folder to copy {selectedItems.Count} item(s)";
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
            {
                try
                {
                    foreach (FileItemViewModel item in selectedItems)
                    {
                        string destPath = Path.Combine(dialog.FolderName, item.Name);
                        if (item.IsFolder) CopyDirectory(item.FullPath, destPath);
                        else File.Copy(item.FullPath, destPath, true);
                    }
                    System.Windows.MessageBox.Show("Successfully copied items!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Batch Copy To failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnMoveTo_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItems : ThumbnailsList.SelectedItems;
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select item(s) to move first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = $"Select destination folder to move {selectedItems.Count} item(s)";
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
            {
                try
                {
                    foreach (FileItemViewModel item in selectedItems.Cast<FileItemViewModel>().ToList())
                    {
                        string destPath = Path.Combine(dialog.FolderName, item.Name);
                        if (item.IsFolder) Directory.Move(item.FullPath, destPath);
                        else File.Move(item.FullPath, destPath, true);
                    }
                    LoadContents(currentPath);
                    System.Windows.MessageBox.Show("Successfully moved items!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Batch Move To failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (RbDetails == null || RbThumbnails == null) return;

            if (RbDetails.IsChecked == true)
            {
                FilesGrid.Visibility = Visibility.Visible;
                ThumbnailsList.Visibility = Visibility.Collapsed;
            }
            else
            {
                FilesGrid.Visibility = Visibility.Collapsed;
                ThumbnailsList.Visibility = Visibility.Visible;
            }
        }

        private void PopulateMetadata(FileItemViewModel item)
        {
            try
            {
                TxtMetaFileName.Text = item.Name;
                TxtMetaTitle.Text = item.Title;
                TxtMetaSubject.Text = item.Subject;
                TxtMetaAuthors.Text = item.Authors;
                TxtMetaTags.Text = item.Tags;
                TxtMetaComments.Text = item.Comments;
                TxtMetaRating.Text = item.Rating;

                TxtSysPath.Text = "Path: " + item.FullPath;
                TxtSysType.Text = "Type: " + (item.IsFolder ? "Directory" : item.FileExtension + " File");
                
                if (item.IsFolder)
                {
                    var dirInfo = new DirectoryInfo(item.FullPath);
                    TxtSysCreated.Text = "Created: " + dirInfo.CreationTime.ToString("g");
                    TxtSysSize.Text = "Size: -";
                }
                else
                {
                    var fileInfo = new FileInfo(item.FullPath);
                    TxtSysCreated.Text = "Created: " + fileInfo.CreationTime.ToString("g");
                    TxtSysSize.Text = "Size: " + item.Size;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading metadata: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveMetadata_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = (FilesGrid.Visibility == Visibility.Visible) ? FilesGrid.SelectedItems : ThumbnailsList.SelectedItems;
            
            if (selectedItems.Count <= 1)
            {
                var selectedItem = selectedItems.Count == 1 ? selectedItems[0] as FileItemViewModel : null;
                if (selectedItem != null)
                {
                    selectedItem.Title = TxtMetaTitle.Text;
                    selectedItem.Subject = TxtMetaSubject.Text;
                    selectedItem.Authors = TxtMetaAuthors.Text;
                    selectedItem.Tags = TxtMetaTags.Text;
                    selectedItem.Comments = TxtMetaComments.Text;
                    selectedItem.Rating = TxtMetaRating.Text;

                    FilesGrid.Items.Refresh();
                    System.Windows.MessageBox.Show("Metadata saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select an item to save metadata for.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                foreach (FileItemViewModel item in selectedItems)
                {
                    if (!string.IsNullOrEmpty(TxtMetaTitle.Text)) item.Title = TxtMetaTitle.Text;
                    if (!string.IsNullOrEmpty(TxtMetaSubject.Text)) item.Subject = TxtMetaSubject.Text;
                    if (!string.IsNullOrEmpty(TxtMetaAuthors.Text)) item.Authors = TxtMetaAuthors.Text;
                    if (!string.IsNullOrEmpty(TxtMetaTags.Text)) item.Tags = TxtMetaTags.Text;
                    if (!string.IsNullOrEmpty(TxtMetaComments.Text)) item.Comments = TxtMetaComments.Text;
                    if (!string.IsNullOrEmpty(TxtMetaRating.Text)) item.Rating = TxtMetaRating.Text;
                }

                FilesGrid.Items.Refresh();
                System.Windows.MessageBox.Show($"Successfully updated metadata for {selectedItems.Count} items!", "Batch Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class FileItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private string _title;
        private string _subject;
        private string _authors;
        private string _tags;
        private string _comments;
        private string _rating;
        private bool _hasThumbnail;
        private System.Windows.Media.ImageSource _thumbnailImage;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Size { get; set; }
        public string LastModified { get; set; }
        public bool IsFolder { get; set; }
        public string FileExtension { get; set; }
        public string CardColor { get; set; }
        
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }
        public string Subject { get => _subject; set { _subject = value; OnPropertyChanged(nameof(Subject)); } }
        public string Authors { get => _authors; set { _authors = value; OnPropertyChanged(nameof(Authors)); } }
        public string Tags { get => _tags; set { _tags = value; OnPropertyChanged(nameof(Tags)); } }
        public string Comments { get => _comments; set { _comments = value; OnPropertyChanged(nameof(Comments)); } }
        public string Rating { get => _rating; set { _rating = value; OnPropertyChanged(nameof(Rating)); } }

        public bool HasThumbnail { get => _hasThumbnail; set { _hasThumbnail = value; OnPropertyChanged(nameof(HasThumbnail)); } }
        public System.Windows.Media.ImageSource ThumbnailImage { get => _thumbnailImage; set { _thumbnailImage = value; OnPropertyChanged(nameof(ThumbnailImage)); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}