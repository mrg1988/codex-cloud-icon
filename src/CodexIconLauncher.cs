using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;

class CodexIconLauncher {
 static string AppId;
 static string PackageFamily;
 const string CustomId="Local.Codex.OriginalLogo";
 static string Root=Path.GetDirectoryName(typeof(CodexIconLauncher).Assembly.Location);
 static string IconPath=Path.Combine(Root,"codex-cloud.ico");
 static string LogPath=Path.Combine(Root,"taskbar-icon-status.txt");
 static Guid StoreId=new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
 [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetPackageFamilyName(IntPtr process,ref uint length,StringBuilder family);
 [StructLayout(LayoutKind.Sequential)] struct Key { public Guid fmtid; public uint pid; }
 [StructLayout(LayoutKind.Explicit,Size=24)] struct Value { [FieldOffset(0)] public ushort vt; [FieldOffset(8)] public IntPtr ptr; }
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface Store { void GetCount(out uint n); void GetAt(uint i,out Key k); void GetValue(ref Key k,out Value v); void SetValue(ref Key k,ref Value v); void Commit(); }
 [DllImport("shell32.dll")] static extern int SHGetPropertyStoreForWindow(IntPtr hwnd,ref Guid iid,[MarshalAs(UnmanagedType.Interface)] out Store store);
 [DllImport("ole32.dll")] static extern int PropVariantClear(ref Value v);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadImage(IntPtr module,string name,uint type,int width,int height,uint flags);
 [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
 [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hwnd,uint msg,IntPtr wparam,IntPtr lparam,uint flags,uint timeout,out IntPtr result);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback,IntPtr data);
 delegate bool EnumWindow(IntPtr hwnd,IntPtr data);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
 class Previous { public IntPtr Small,Big; public Dictionary<uint,string> Properties=new Dictionary<uint,string>(); }
 static Dictionary<IntPtr,Previous> Changed=new Dictionary<IntPtr,Previous>();
 static Key Property(uint id) { return new Key { fmtid=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),pid=id }; }
 static string Read(Store store,uint id) { var key=Property(id); Value v; store.GetValue(ref key,out v); try { return v.vt==31?Marshal.PtrToStringUni(v.ptr):null; } finally { PropVariantClear(ref v); } }
 static void Write(Store store,uint id,string text) { var key=Property(id); var v=new Value(); if(text!=null) { v.vt=31;v.ptr=Marshal.StringToCoTaskMemUni(text); } try { store.SetValue(ref key,ref v); } finally { PropVariantClear(ref v); } }
 static IntPtr Message(IntPtr hwnd,uint msg,int kind,IntPtr value) { IntPtr result; if(SendMessageTimeout(hwnd,msg,new IntPtr(kind),value,2,1000,out result)==IntPtr.Zero) throw new Exception("Window icon message timed out"); return result; }
 static HashSet<uint> FindCodex() {
  var ids=new HashSet<uint>();
  EnumWindows((hwnd,data)=> { uint pid; GetWindowThreadProcessId(hwnd,out pid); if(IsWindowVisible(hwnd)&&!ids.Contains(pid)&&MatchesPackage(pid)) ids.Add(pid); return true; },IntPtr.Zero);
  return ids;
 }
 static bool MatchesPackage(uint pid) {
  IntPtr process=OpenProcess(0x1000,false,pid); if(process==IntPtr.Zero)return false;
  try { uint length=0; if(GetPackageFamilyName(process,ref length,null)!=122)return false; var family=new StringBuilder((int)length); return GetPackageFamilyName(process,ref length,family)==0&&family.ToString()==PackageFamily; }
  finally { CloseHandle(process); }
 }
 static void LoadConfig() {
  string[] lines=File.ReadAllLines(Path.Combine(Root,"launcher.ini"));
  foreach(string line in lines) { if(line.StartsWith("AppId="))AppId=line.Substring(6); if(line.StartsWith("PackageFamily="))PackageFamily=line.Substring(14); }
  if(String.IsNullOrWhiteSpace(PackageFamily)||!PackageFamily.StartsWith("OpenAI.Codex_",StringComparison.Ordinal)||String.IsNullOrWhiteSpace(AppId)||!AppId.StartsWith(PackageFamily+"!",StringComparison.Ordinal))throw new Exception("Invalid or missing Codex package configuration. Run Install.ps1 again.");
 }
 static void Status(string text) { File.WriteAllText(LogPath,text+"\r\nChecked: "+DateTime.UtcNow.ToString("o")); }
 static void Apply(IntPtr hwnd,IntPtr small,IntPtr big) {
  Store store; Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(hwnd,ref StoreId,out store));
  try {
   Previous previous;
   if(!Changed.TryGetValue(hwnd,out previous)) {
    previous=new Previous(); foreach(uint id in new uint[]{2,3,4,5}) previous.Properties[id]=Read(store,id);
    previous.Small=Message(hwnd,0x7f,0,IntPtr.Zero); previous.Big=Message(hwnd,0x7f,1,IntPtr.Zero); Changed.Add(hwnd,previous);
   }
   string shellIcon=IconPath+",0";
   if(Read(store,3)!=shellIcon) Write(store,3,shellIcon);
   if(Read(store,2)!= "\""+Path.Combine(Root,"CodexIconLauncher.exe")+"\"") Write(store,2,"\""+Path.Combine(Root,"CodexIconLauncher.exe")+"\"");
   if(Read(store,4)!="Codex") Write(store,4,"Codex");
   if(Read(store,5)!=CustomId) Write(store,5,CustomId);
   if(Message(hwnd,0x7f,0,IntPtr.Zero)!=small) Message(hwnd,0x80,0,small);
   if(Message(hwnd,0x7f,1,IntPtr.Zero)!=big) Message(hwnd,0x80,1,big);
  } finally { Marshal.FinalReleaseComObject(store); }
 }
 static void Restore() {
  foreach(var item in Changed) if(IsWindow(item.Key)) try {
   Message(item.Key,0x80,0,item.Value.Small); Message(item.Key,0x80,1,item.Value.Big);
   Store store; Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(item.Key,ref StoreId,out store));
   try { foreach(var prop in item.Value.Properties) Write(store,prop.Key,prop.Value); } finally { Marshal.FinalReleaseComObject(store); }
  } catch { }
 }
 [STAThread] static int Main(string[] args) {
  try { LoadConfig(); } catch(Exception ex) { Status("Configuration failed: "+ex.Message); return 1; }
  bool attach=Array.IndexOf(args,"--attach")>=0;
  bool stop=Array.IndexOf(args,"--restore")>=0;
  bool check=Array.IndexOf(args,"--check")>=0;
  using(var stopEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexOriginalIcon.Stop")) {
   if(stop) { stopEvent.Set();return 0; }
   if(check) {
    IntPtr probe=LoadImage(IntPtr.Zero,IconPath,1,48,48,0x10);
    if(probe==IntPtr.Zero) { Status("Check failed: icon could not be loaded.");return 1; }
    DestroyIcon(probe);Status("Check passed.\r\nPackage: "+PackageFamily+"\r\nMatching processes: "+String.Join(",",FindCodex())+"\r\nIcon: "+IconPath);return 0;
   }
   if(!attach) try { Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"shell:AppsFolder\\"+AppId) {UseShellExecute=true}); } catch(Exception ex) { Status("Launch failed: "+ex.Message);return 1; }
   bool owner; using(var mutex=new Mutex(true,"Local\\CodexOriginalIcon.Running",out owner)) {
    if(!owner) return 0;
    IntPtr small=IntPtr.Zero,big=IntPtr.Zero;
    try {
     small=LoadImage(IntPtr.Zero,IconPath,1,16,16,0x10); big=LoadImage(IntPtr.Zero,IconPath,1,48,48,0x10);
     if(small==IntPtr.Zero||big==IntPtr.Zero) throw new Exception("The cloud icon could not be loaded");
     int idle=0;
     while(!stopEvent.WaitOne(2000)) {
      var pids=FindCodex(); if(pids.Count==0) { if(++idle>30)break;continue; } idle=0;
      var failures=new List<string>(); int applied=0;
      EnumWindows((hwnd,data)=> { uint pid;GetWindowThreadProcessId(hwnd,out pid); if(pids.Contains(pid)&&IsWindowVisible(hwnd)) try { Apply(hwnd,small,big);applied++; } catch(Exception ex) {failures.Add("Window "+hwnd+": "+ex.Message);} return true; },IntPtr.Zero);
      var stale=new List<IntPtr>();foreach(var item in Changed)if(!IsWindow(item.Key))stale.Add(item.Key);foreach(var hwnd in stale)Changed.Remove(hwnd);
      Status("Applied cloud icon to "+applied+" window(s).\r\nProcesses: "+String.Join(",",pids)+"\r\nTaskbar identity: "+CustomId+"\r\nIcon: "+IconPath+(failures.Count>0?"\r\nFailures: "+String.Join("; ",failures):""));
     }
    } catch(Exception ex) {Status(ex.ToString());return 1;}
    finally {Restore(); if(small!=IntPtr.Zero)DestroyIcon(small);if(big!=IntPtr.Zero)DestroyIcon(big);mutex.ReleaseMutex();}
   }
  }
  return 0;
 }
}

