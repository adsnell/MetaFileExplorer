using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace MetaFileExplorer
{
    public class FileItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public long SizeInBytes { get; set; }
        public string Size { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        
        // Metadata properties
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;

        private BitmapSource? _thumbnailImage;
        public BitmapSource? ThumbnailImage
        {
            get => _thumbnailImage;
            set
            {
                _thumbnailImage = value;
                OnPropertyChanged(nameof(ThumbnailImage));
                OnPropertyChanged(nameof(HasThumbnail));
            }
        }

        public bool HasThumbnail => _thumbnailImage != null;

        // File Type Styling Properties for Thumbnails
        public string FileExtension => IsFolder ? "FOLDER" : Path.GetExtension(FullPath).TrimStart('.').ToUpper();

        public string CardColor
        {
            get
            {
                if (IsFolder) return "#FFC107"; // Yellow
                var ext = FileExtension;
                return ext switch
                {
                    "PDF" => "#D32F2F",                          // Red
                    "DOC" or "DOCX" => "#1976D2",                // Blue
                    "XLS" or "XLSX" or "CSV" => "#388E3C",       // Green
                    "PPT" or "PPTX" => "#E64A19",                // Orange
                    "CS" or "CPP" or "JS" or "PY" => "#512DA8",  // Deep Purple (Code)
                    "BAT" or "CMD" or "PS1" => "#455A64",        // Slate Gray (Scripts)
                    "TXT" or "LOG" or "MD" => "#616161",         // Gray (Text)
                    "ZIP" or "RAR" or "7Z" => "#8E24AA",         // Purple (Archives)
                    
                    // Expanded Filetype Categories
                    "MP4" or "MKV" or "AVI" or "MOV" => "#4E342E", // Video
                    "MP3" or "WAV" or "FLAC" or "AAC" => "#00838F", // Audio
                    "HTML" or "CSS" or "JSON" or "XML" => "#0277BD", // Web/Data
                    "EXE" or "MSI" or "DLL" => "#C2185B",        // Binaries
                    "PSD" or "AI" or "SVG" => "#AD1457",         // Design
                    
                    _ => "#37474F"                               // Default Dark Blue-Gray
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}