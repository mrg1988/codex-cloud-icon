using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
public static class ShellIconCheck {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern uint GetFinalPathNameByHandle(IntPtr handle,StringBuilder path,uint length,uint flags);
 public static string PhysicalPath(string path) { using(var file=File.OpenRead(path)) {var text=new StringBuilder(2048); if(GetFinalPathNameByHandle(file.SafeFileHandle.DangerousGetHandle(),text,(uint)text.Capacity,0)==0)throw new Exception("Could not resolve path");return text.ToString();} }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct SHFILEINFO {
  public IntPtr icon; public int index; public uint attrs;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string display;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)] public string type;
 }
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(string path,uint attrs,ref SHFILEINFO info,uint size,uint flags);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern void SHChangeNotify(uint change,uint flags,string path1,string path2);
 [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint PrivateExtractIcons(string path,int index,int width,int height,[Out] IntPtr[] icons,[Out] uint[] ids,uint count,uint flags);
 public static IntPtr Extract(string path,int size) { var icons=new IntPtr[1];var ids=new uint[1];uint count=PrivateExtractIcons(path,0,size,size,icons,ids,1,0);if(count!=1||icons[0]==IntPtr.Zero)throw new Exception("Native extraction failed for size "+size);return icons[0]; }
 public static void Refresh(string path) { SHChangeNotify(0x2000,0x1005,path,null); }
 public static void RefreshAssociations() { SHChangeNotify(0x08000000,0x1000,null,null); }
 public static IntPtr GetHandle(string path) {
  var info=new SHFILEINFO(); var result=SHGetFileInfo(path,0,ref info,(uint)Marshal.SizeOf(info),0x100);
  if(result==IntPtr.Zero || info.icon==IntPtr.Zero) throw new Exception("Shell icon extraction failed");
  return info.icon;
 }
}
