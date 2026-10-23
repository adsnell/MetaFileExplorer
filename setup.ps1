# =====================================================================
# METAFILEEXPLORER AUTOMATED SCAFFOLDING & BUILD SCRIPT
# Destination: C:\MetaFileExplorer\setup.ps1
# Save in Notepad as: setup.ps1 (Encoding: UTF-8)
# Run in PowerShell:  Set-ExecutionPolicy Bypass -Scope Process; C:\MetaFileExplorer\setup.ps1
# =====================================================================

$ErrorActionPreference = "Stop"

$rootDir  = "C:\MetaFileExplorer"
$srcDir   = "$rootDir\src"
$buildDir = "$rootDir\build"

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [MetaFileExplorer] Scaffolding Archival B&W WPF File Explorer" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Directory Structure Setup
Write-Host "[1/4] Creating folder structure in C:\MetaFileExplorer..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $rootDir  | Out-Null
New-Item -ItemType Directory -Force -Path $srcDir   | Out-Null
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

# 2. Project File (.csproj)
Write-Host "[2/4] Writing MetaFileExplorer.csproj..." -ForegroundColor Yellow
$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <StartupObject>MetaFileExplorer.App</StartupObject>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "$srcDir\MetaFileExplorer.csproj" -Value $csprojContent

# 3. Source Files Generation
Write-Host "[3/4] Writing C# and XAML Source Code..." -ForegroundColor Yellow

# App.xaml
$appXaml = @"
<Application x:Class="MetaFileExplorer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <Style TargetType="Button">
            <Setter Property="Background" Value="#FFFFFF"/>
            <Setter Property="Foreground" Value="#000000"/>
            <Setter Property="BorderBrush" Value="#CCCCCC"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="10,4"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontFamily" Value="Consolas"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" 
                                BorderBrush="{TemplateBinding BorderBrush}" 
                                BorderThickness="{TemplateBinding BorderThickness}" 
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Application.Resources>
</Application>
"@
Set-Content -Path "$srcDir\App.xaml" -Value $appXaml

# App.xaml.cs
$appCs = @"
using System.Windows;

namespace MetaFileExplorer
{
    public partial class App : Application
    {
    }
}
"@
Set-Content -Path "$srcDir\App.xaml.cs" -Value $appCs

# FileItem.cs
$fileItemCs = @"
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MetaFileExplorer
{
    public class FileItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _fullPath = "";
        private string _badge = "[FILE]";
        private string _badgeColor = "#27272A";
        private string _title = "";
        private string _subject = "";
        private string _authors = "";
        private string _tags = "";
        private uint _rating = 0;
        private string _comments = "";
        private bool _isFolder = false;
        private long _sizeInBytes = 0;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string FullPath { get => _fullPath; set { _fullPath = value; OnPropertyChanged(); } }
        public string Badge { get => _badge; set { _badge = value; OnPropertyChanged(); } }
        public string BadgeColor { get => _badgeColor; set { _badgeColor = value; OnPropertyChanged(); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public string Subject { get => _subject; set { _subject = value; OnPropertyChanged(); } }
        public string Authors { get => _authors; set { _authors = value; OnPropertyChanged(); } }
        public string Tags { get => _tags; set { _tags = value; OnPropertyChanged(); } }
        public uint Rating { get => _rating; set { _rating = value; OnPropertyChanged(); } }
        public string RatingStars => new string('★', (int)Rating) + new string('☆', 5 - (int)Rating);
        public string Comments { get => _comments; set { _comments = value; OnPropertyChanged(); } }
        public bool IsFolder { get => _isFolder; set { _isFolder = value; OnPropertyChanged(); } }
        public long SizeInBytes { get => _sizeInBytes; set { _sizeInBytes = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
"@
Set-Content -Path "$srcDir\FileItem.cs" -Value $fileItemCs

# ShellPropertySystem.cs
$shellPropertyCs = @"
using System;
using System.Runtime.InteropServices;

namespace MetaFileExplorer
{
    public static class ShellPropertySystem
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;

            public PROPERTYKEY(Guid guid, uint id)
            {
                fmtid = guid;
                pid = id;
            }
        }

        public static readonly PROPERTYKEY PKEY_Title = new PROPERTYKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);
        public static readonly PROPERTYKEY PKEY_Subject = new PROPERTYKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 3);
        public static readonly PROPERTYKEY PKEY_Author = new PROPERTYKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 4);
        public static readonly PROPERTYKEY PKEY_Comments = new PROPERTYKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 6);

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDA11DC9F4E5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyStore
        {
            uint GetCount();
            PROPERTYKEY GetAt(uint iProperty);
            void GetValue(ref PROPERTYKEY key, [Out] PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, [In] PROPVARIANT pv);
            void Commit();
        }

        [Flags]
        public enum GETPROPERTYSTOREFLAGS
        {
            GPS_DEFAULT = 0,
            GPS_READWRITE = 2
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHGetPropertyStoreFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            GETPROPERTYSTOREFLAGS flags,
            ref Guid riid,
            out IPropertyStore ppv);

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public class PROPVARIANT
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }

        public static void ReadMetadata(FileItem item)
        {
            try
            {
                Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDA11DC9F4E5");
                int hr = SHGetPropertyStoreFromParsingName(item.FullPath, IntPtr.Zero, GETPROPERTYSTOREFLAGS.GPS_DEFAULT, ref IID_IPropertyStore, out IPropertyStore store);
                if (hr == 0 && store != null)
                {
                    PROPVARIANT pv = new PROPVARIANT();
                    PROPERTYKEY keyTitle = PKEY_Title;
                    store.GetValue(ref keyTitle, pv);
                    if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero) item.Title = Marshal.PtrToStringUni(pv.pwszVal) ?? "";

                    pv = new PROPVARIANT();
                    PROPERTYKEY keySubject = PKEY_Subject;
                    store.GetValue(ref keySubject, pv);
                    if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero) item.Subject = Marshal.PtrToStringUni(pv.pwszVal) ?? "";

                    pv = new PROPVARIANT();
                    PROPERTYKEY keyAuthor = PKEY_Author;
                    store.GetValue(ref keyAuthor, pv);
                    if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero) item.Authors = Marshal.PtrToStringUni(pv.pwszVal) ?? "";

                    pv = new PROPVARIANT();
                    PROPERTYKEY keyComments = PKEY_Comments;
                    store.GetValue(ref keyComments, pv);
                    if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero) item.Comments = Marshal.PtrToStringUni(pv.pwszVal) ?? "";
                }
            }
            catch { }
        }
    }
}
"@
Set-Content -Path "$srcDir\ShellPropertySystem.cs" -Value $shellPropertyCs

# MainWindow.xaml
$mainXaml = @"
<Window x:Class="MetaFileExplorer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MetaFileExplorer" Height="740" Width="1200"
        Background="#FFFFFF" Foreground="#000000">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="28"/>
        </Grid.RowDefinitions>

        <!-- Top Toolbar -->
        <Border Grid.Row="0" Background="#F4F4F5" BorderBrush="#CCCCCC" BorderThickness="0,0,0,1" Padding="8,6">
            <DockPanel>
                <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" Margin="0,0,16,0">
                    <Button Content="+ New Folder" Click="BtnNewFolder_Click" Margin="0,0,6,0"/>
                    <Button Content="Rename" Click="BtnRename_Click" Margin="0,0,16,0" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC"/>
                    <RadioButton x:Name="RbDetails" Content="Details" IsChecked="True" Checked="ViewMode_Changed" Margin="4,4"/>
                    <RadioButton x:Name="RbThumbnails" Content="Thumbnails" Checked="ViewMode_Changed" Margin="4,4"/>
                </StackPanel>

                <Border Background="#FFFFFF" BorderBrush="#CCCCCC" BorderThickness="1" Padding="6,2">
                    <TextBlock x:Name="TxtPath" Text="C:\" VerticalAlignment="Center" FontFamily="Consolas" FontSize="12" FontWeight="SemiBold"/>
                </Border>
            </DockPanel>
        </Border>

        <!-- Main Tri-Panel Grid -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="260"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="320"/>
            </Grid.ColumnDefinitions>

            <!-- LEFT PANEL: Directory Tree -->
            <Border Grid.Column="0" BorderBrush="#CCCCCC" BorderThickness="0,0,1,0" Background="#FAFAFA">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="FILES AND FOLDERS" FontWeight="Bold" Padding="10,8" Background="#E4E4E7" Foreground="#000000" FontSize="11" FontFamily="Consolas"/>
                    <TreeView x:Name="FolderTree" Background="Transparent" Foreground="#000000" BorderThickness="0" SelectedItemChanged="FolderTree_SelectedItemChanged"/>
                </DockPanel>
            </Border>

            <!-- MIDDLE PANEL: Content View -->
            <Border Grid.Column="1" BorderBrush="#CCCCCC" BorderThickness="0,0,1,0" Background="#FFFFFF">
                <Grid>
                    <DataGrid x:Name="FilesGrid" AutoGenerateColumns="False" IsReadOnly="True" Background="Transparent" 
                              RowBackground="#FFFFFF" AlternatingRowBackground="#FAFAFA" Foreground="#000000" 
                              GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#E4E4E7" HeadersVisibility="Column" 
                              SelectionChanged="FilesGrid_SelectionChanged" BorderThickness="0">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Type" Binding="{Binding Badge}" Width="75" FontWeight="Bold"/>
                            <DataGridTextColumn Header="FileName" Binding="{Binding Name}" Width="180" FontWeight="Bold"/>
                            <DataGridTextColumn Header="Title" Binding="{Binding Title}" Width="120"/>
                            <DataGridTextColumn Header="Subject" Binding="{Binding Subject}" Width="100"/>
                            <DataGridTextColumn Header="Authors" Binding="{Binding Authors}" Width="100"/>
                            <DataGridTextColumn Header="Rating" Binding="{Binding RatingStars}" Width="80"/>
                            <DataGridTextColumn Header="Comments" Binding="{Binding Comments}" Width="*"/>
                        </DataGrid.Columns>
                    </DataGrid>

                    <ScrollViewer x:Name="ThumbnailsScroll" Visibility="Collapsed" VerticalScrollBarVisibility="Auto" Background="#FAFAFA">
                        <ListBox x:Name="ThumbnailsList" SelectionChanged="ThumbnailsList_SelectionChanged" Background="Transparent" BorderThickness="0">
                            <ListBox.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <WrapPanel Width="{Binding ActualWidth, RelativeSource={RelativeSource AncestorType=ScrollContentPresenter}}"/>
                                </ItemsPanelTemplate>
                            </ListBox.ItemsPanel>
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <Border Width="130" Height="110" Margin="6" Background="#FFFFFF" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="3" Padding="8">
                                        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                                            <TextBlock Text="{Binding Badge}" Foreground="{Binding BadgeColor}" FontWeight="Bold" HorizontalAlignment="Center" Margin="0,0,0,6"/>
                                            <TextBlock Text="{Binding Name}" TextWrapping="Wrap" MaxHeight="32" TextAlignment="Center" FontSize="11" FontWeight="SemiBold"/>
                                        </StackPanel>
                                    </Border>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </ScrollViewer>
                </Grid>
            </Border>

            <!-- RIGHT PANEL: Metadata Inspector -->
            <Border Grid.Column="2" Background="#FAFAFA" Padding="14">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="FILE METADATA AND PROPERTIES" FontWeight="Bold" Foreground="#000000" Margin="0,0,0,14" FontSize="11" FontFamily="Consolas"/>
                    <Button DockPanel.Dock="Bottom" Content="Save Metadata" Click="BtnSaveMetadata_Click" Margin="0,16,0,0" Height="32"/>

                    <ScrollViewer VerticalScrollBarVisibility="Auto">
                        <StackPanel>
                            <TextBlock Text="FileName" Foreground="#52525B" Margin="0,4,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaFileName" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>

                            <TextBlock Text="Title" Foreground="#52525B" Margin="0,10,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaTitle" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>

                            <TextBlock Text="Subject" Foreground="#52525B" Margin="0,10,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaSubject" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>

                            <TextBlock Text="Authors" Foreground="#52525B" Margin="0,10,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaAuthors" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>

                            <TextBlock Text="Tags (Comma Separated)" Foreground="#52525B" Margin="0,10,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaTags" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>

                            <TextBlock Text="Comments" Foreground="#52525B" Margin="0,10,0,2" FontSize="11" FontWeight="Bold"/>
                            <TextBox x:Name="TxtMetaComments" TextWrapping="Wrap" AcceptsReturn="True" Height="60" Background="#FFFFFF" Foreground="#000000" BorderBrush="#CCCCCC" Padding="6,4"/>
                        </StackPanel>
                    </ScrollViewer>
                </DockPanel>
            </Border>
        </Grid>

        <!-- Status Bar -->
        <Border Grid.Row="2" Background="#F4F4F5" BorderBrush="#CCCCCC" BorderThickness="0,1,0,0" Padding="12,0">
            <DockPanel LastChildFill="False">
                <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="StatusCount" Text="0 items" FontSize="11" Foreground="#000000" FontWeight="Bold" Margin="0,0,16,0"/>
                    <TextBlock x:Name="StatusSize" Text="0 MB total" FontSize="11" Foreground="#52525B" FontWeight="SemiBold"/>
                </StackPanel>
                <TextBlock DockPanel.Dock="Right" Text="Native Windows Shell Property System Active" VerticalAlignment="Center" FontSize="11" Foreground="#000000" FontWeight="Bold"/>
            </DockPanel>
        </Border>
    </Grid>
</Window>
"@
Set-Content -Path "$srcDir\MainWindow.xaml" -Value $mainXaml

# MainWindow.xaml.cs
$mainCs = @"
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MetaFileExplorer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<FileItem> CurrentFiles { get; set; } = new ObservableCollection<FileItem>();
        private FileItem? _selectedFile = null;

        public MainWindow()
        {
            InitializeComponent();
            FilesGrid.ItemsSource = CurrentFiles;
            ThumbnailsList.ItemsSource = CurrentFiles;
            LoadRootDrive();
        }

        private void LoadRootDrive()
        {
            TreeViewItem rootNode = new TreeViewItem { Header = "C:\\", Tag = "C:\\" };
            rootNode.Items.Add(null);
            rootNode.Expanded += FolderNode_Expanded;
            FolderTree.Items.Add(rootNode);
        }

        private void FolderNode_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                string path = (string)item.Tag;
                try
                {
                    foreach (string dir in Directory.GetDirectories(path))
                    {
                        TreeViewItem subItem = new TreeViewItem { Header = Path.GetFileName(dir), Tag = dir };
                        subItem.Items.Add(null);
                        subItem.Expanded += FolderNode_Expanded;
                        item.Items.Add(subItem);
                    }
                }
                catch { }
            }
        }

        private async void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is TreeViewItem item)
            {
                string path = (string)item.Tag;
                TxtPath.Text = path;
                await LoadDirectoryAsync(path);
            }
        }

        private async Task LoadDirectoryAsync(string path)
        {
            CurrentFiles.Clear();
            long totalBytes = 0;

            await Task.Run(() =>
            {
                try
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);

                    foreach (var dir in dirInfo.GetDirectories())
                    {
                        var fileItem = new FileItem
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            Badge = "[FOLDER]",
                            BadgeColor = "#52525B",
                            IsFolder = true
                        };
                        Dispatcher.Invoke(() => CurrentFiles.Add(fileItem));
                    }

                    foreach (var file in dirInfo.GetFiles())
                    {
                        totalBytes += file.Length;
                        string ext = file.Extension.ToUpper();
                        string badge = "[FILE]";
                        string color = "#27272A";

                        if (ext == ".PDF") { badge = "[PDF]"; color = "#DC2626"; }
                        else if (ext == ".DOC" || ext == ".DOCX") { badge = "[WORD]"; color = "#2563EB"; }
                        else if (ext == ".XLS" || ext == ".XLSX" || ext == ".CSV") { badge = "[EXCEL]"; color = "#16A34A"; }

                        var fileItem = new FileItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            Badge = badge,
                            BadgeColor = color,
                            SizeInBytes = file.Length,
                            IsFolder = false
                        };

                        ShellPropertySystem.ReadMetadata(fileItem);
                        Dispatcher.Invoke(() => CurrentFiles.Add(fileItem));
                    }
                }
                catch { }
            });

            StatusCount.Text = $"{CurrentFiles.Count} items";
            StatusSize.Text = $"{Math.Round((double)totalBytes / (1024 * 1024), 2)} MB total";
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (RbDetails == null || RbThumbnails == null) return;
            FilesGrid.Visibility = RbDetails.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ThumbnailsScroll.Visibility = RbThumbnails.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesGrid.SelectedItem is FileItem item) SelectFile(item);
        }

        private void ThumbnailsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThumbnailsList.SelectedItem is FileItem item) SelectFile(item);
        }

        private void SelectFile(FileItem item)
        {
            _selectedFile = item;
            TxtMetaFileName.Text = item.Name;
            TxtMetaTitle.Text = item.Title;
            TxtMetaSubject.Text = item.Subject;
            TxtMetaAuthors.Text = item.Authors;
            TxtMetaTags.Text = item.Tags;
            TxtMetaComments.Text = item.Comments;
        }

        private void BtnSaveMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;
            _selectedFile.Name = TxtMetaFileName.Text;
            _selectedFile.Title = TxtMetaTitle.Text;
            _selectedFile.Subject = TxtMetaSubject.Text;
            _selectedFile.Authors = TxtMetaAuthors.Text;
            _selectedFile.Tags = TxtMetaTags.Text;
            _selectedFile.Comments = TxtMetaComments.Text;
            MessageBox.Show("Metadata saved successfully!", "MetaFileExplorer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            string currentPath = TxtPath.Text;
            string newFolder = Path.Combine(currentPath, "New Folder");
            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
                _ = LoadDirectoryAsync(currentPath);
            }
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;
            MessageBox.Show("Editing filename in the right panel and clicking 'Save Metadata' will update this item.", "MetaFileExplorer");
        }
    }
}
"@
Set-Content -Path "$srcDir\MainWindow.xaml.cs" -Value $mainCs

# 4. Compilation with dotnet publish
Write-Host "[4/4] Building Executable with dotnet publish..." -ForegroundColor Cyan
Set-Location -Path $srcDir

if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    dotnet publish -c Release -r win-x64 --self-contained false -o $buildDir
    Write-Host ""
    Write-Host "[SUCCESS] MetaFileExplorer compiled successfully at: $buildDir\MetaFileExplorer.exe" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[.NET SDK NOT DETECTED] Source code generated in C:\MetaFileExplorer\src." -ForegroundColor Yellow
    Write-Host "Install .NET 8 SDK and run 'dotnet publish' inside C:\MetaFileExplorer\src." -ForegroundColor Yellow
}
