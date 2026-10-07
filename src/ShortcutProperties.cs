using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public static class CodexShortcut {
  [ComImport, Guid("00021401-0000-0000-C000-000000000046")] class ShellLink {}
  [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  interface IShellLinkW {
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, IntPtr find, uint flags);
    void GetIDList(out IntPtr pidl); void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string text);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string text);
    void GetHotkey(out short key); void SetHotkey(short key);
    void GetShowCmd(out int cmd); void SetShowCmd(int cmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, out int index);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
    void Resolve(IntPtr hwnd, uint flags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
  }
  [StructLayout(LayoutKind.Sequential)] struct PropertyKey { public Guid fmtid; public uint pid; }
  [StructLayout(LayoutKind.Explicit, Size=24)] struct PropVariant {
    [FieldOffset(0)] public ushort vt; [FieldOffset(8)] public IntPtr str;
  }
  [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  interface IPropertyStore {
    void GetCount(out uint count); void GetAt(uint i, out PropertyKey key);
    void GetValue(ref PropertyKey key, out PropVariant value);
    void SetValue(ref PropertyKey key, ref PropVariant value); void Commit();
  }
  [DllImport("shell32.dll", CharSet=CharSet.Unicode)] static extern int SHParseDisplayName(string name, IntPtr context, out IntPtr pidl, uint attrsIn, out uint attrsOut);
  [DllImport("shell32.dll")] static extern int SHGetNameFromIDList(IntPtr pidl, uint sigdn, out IntPtr name);
  [DllImport("shell32.dll", CharSet=CharSet.Unicode)] static extern void SHChangeNotify(uint change, uint flags, string item1, string item2);
  static PropertyKey AppIdKey() { return new PropertyKey { fmtid=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid=5 }; }
  public static void SetAppId(string path,string appId) {
    var link=(IShellLinkW)new ShellLink();
    try {
      ((IPersistFile)link).Load(path,2);
      var store=(IPropertyStore)link; var key=AppIdKey();
      var value=new PropVariant { vt=31, str=Marshal.StringToCoTaskMemUni(appId) };
      try { store.SetValue(ref key,ref value); store.Commit(); } finally { Marshal.FreeCoTaskMem(value.str); }
      ((IPersistFile)link).Save(path,true);
      SHChangeNotify(0x2000,0x1005,path,null);
    } finally { Marshal.FinalReleaseComObject(link); }
  }
}

