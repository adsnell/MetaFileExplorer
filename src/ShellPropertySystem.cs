using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

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

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDA11DC9F4E5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyStore
        {
            uint GetCount();
            PROPERTYKEY GetAt(uint iProperty);
            void GetValue(ref PROPERTYKEY key, [Out] out PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, [In] ref PROPVARIANT pv);
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

        [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void PSGetPropertyKeyFromName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            out PROPERTYKEY ppropkey);

        [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        public static extern int PropVariantToString(
            ref PROPVARIANT propvar,
            [Out] StringBuilder psz,
            uint cch);

        [DllImport("ole32.dll", PreserveSig = false)]
        public static extern void PropVariantClear(ref PROPVARIANT pvar);

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct PROPVARIANT : IDisposable
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(2)] public ushort wReserved1;
            [FieldOffset(4)] public ushort wReserved2;
            [FieldOffset(6)] public ushort wReserved3;
            [FieldOffset(8)] public IntPtr pwszVal;

            public static PROPVARIANT FromString(string str)
            {
                var pv = new PROPVARIANT();
                pv.vt = 31; // VT_LPWSTR
                pv.pwszVal = Marshal.StringToCoTaskMemUni(str);
                return pv;
            }

            public string GetStringValue()
            {
                var sb = new StringBuilder(1024);
                int hr = PropVariantToString(ref this, sb, (uint)sb.Capacity);
                if (hr >= 0)
                {
                    return sb.ToString();
                }
                return string.Empty;
            }

            public void Dispose()
            {
                try
                {
                    PropVariantClear(ref this);
                }
                catch { }
            }
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            void GetImage(
                SIZE size,
                SIIGBF flags,
                out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int width, int height) { cx = width; cy = height; }
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0,
            SIIGBF_BIGGERSIZEOK = 1,
            SIIGBF_MEMORYONLY = 2,
            SIIGBF_ICONONLY = 4,
            SIIGBF_THUMBNAILONLY = 8,
            SIIGBF_INCACHEONLY = 16
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out] out IShellItem ppv);

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        public static void ReadMetadata(FileItem item)
        {
            try
            {
                Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDA11DC9F4E5");
                int hr = SHGetPropertyStoreFromParsingName(item.FullPath, IntPtr.Zero, GETPROPERTYSTOREFLAGS.GPS_DEFAULT, ref IID_IPropertyStore, out IPropertyStore store);
                
                Console.WriteLine("[DEBUG] ReadMetadata [" + item.Name + "] - HRESULT: " + hr);

                if (hr == 0 && store != null)
                {
                    ReadProp(store, "System.Title", val => item.Title = val);
                    ReadProp(store, "System.Subject", val => item.Subject = val);
                    ReadProp(store, "System.Author", val => item.Authors = val);
                    ReadProp(store, "System.Comment", val => item.Comments = val);

                    Console.WriteLine("[DEBUG] Finished reading properties for: " + item.Name);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Failed to get PropertyStore. HRESULT: " + hr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DEBUG] ReadMetadata Fatal Exception: " + ex.Message);
            }
        }

        private static void ReadProp(IPropertyStore store, string canonicalName, Action<string> assignAction)
        {
            try
            {
                PSGetPropertyKeyFromName(canonicalName, out PROPERTYKEY key);
                store.GetValue(ref key, out PROPVARIANT pv);
                using (pv)
                {
                    string val = pv.GetStringValue();
                    Console.WriteLine("  -> " + canonicalName + " = '" + val + "'");
                    if (!string.IsNullOrEmpty(val))
                    {
                        assignAction(val);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  -> Error reading " + canonicalName + ": " + ex.Message);
            }
        }

        public static bool SaveMetadata(FileItem item)
        {
            try
            {
                Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDA11DC9F4E5");
                int hr = SHGetPropertyStoreFromParsingName(item.FullPath, IntPtr.Zero, GETPROPERTYSTOREFLAGS.GPS_READWRITE, ref IID_IPropertyStore, out IPropertyStore store);
                if (hr == 0 && store != null)
                {
                    WriteProp(store, "System.Title", item.Title);
                    WriteProp(store, "System.Subject", item.Subject);
                    WriteProp(store, "System.Author", item.Authors);
                    WriteProp(store, "System.Comment", item.Comments);

                    store.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveMetadata Exception: " + ex.Message);
            }
            return false;
        }

        private static void WriteProp(IPropertyStore store, string canonicalName, string value)
        {
            try
            {
                PSGetPropertyKeyFromName(canonicalName, out PROPERTYKEY key);
                var pv = PROPVARIANT.FromString(value ?? "");
                store.SetValue(ref key, ref pv);
                pv.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("  -> Error writing " + canonicalName + ": " + ex.Message);
            }
        }

        public static BitmapSource? LoadThumbnail(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".webp")
            {
                IntPtr hbm = IntPtr.Zero;
                try
                {
                    Guid guidIShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                    SHCreateItemFromParsingName(path, IntPtr.Zero, guidIShellItem, out IShellItem shellItem);
                    if (shellItem is IShellItemImageFactory imageFactory)
                    {
                        imageFactory.GetImage(new SIZE(180, 180), SIIGBF.SIIGBF_BIGGERSIZEOK, out hbm);
                        if (hbm != IntPtr.Zero)
                        {
                            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hbm,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            bitmapSource.Freeze();
                            return bitmapSource;
                        }
                    }
                }
                catch
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.DecodePixelWidth = 180;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch { }
                }
                finally
                {
                    if (hbm != IntPtr.Zero)
                    {
                        DeleteObject(hbm);
                    }
                }
            }
            return null;
        }
    }
}