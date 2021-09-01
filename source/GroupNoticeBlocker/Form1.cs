using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using UIAutomationClient;


namespace GroupNoticeBlocker
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }
        public const UInt32 FLASHW_STOP = 0;
        public const UInt32 FLASHW_CAPTION = 1;
        public const UInt32 FLASHW_TRAY = 2;
        public const UInt32 FLASHW_ALL = 3;
        public const UInt32 FLASHW_TIMER = 4;
        public const UInt32 FLASHW_TIMERNOFG = 12;
        int WM_LBUTTONDOWN = 0x0201;
        int WM_LBUTTONUP = 0x0202;
        const int SW_RESTORE = 9;
        //windows函数引用区
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(String ClassName, String WindowName);
        [DllImport("user32.dll", EntryPoint = "SendMessage")]

        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;                             //最左坐标
            public int Top;                             //最上坐标
            public int Right;                           //最右坐标
            public int Bottom;                        //最下坐标
        }
        //常量声明区
        const int SW_HIDE = 0;
        const int WM_SETFOCUS = 0x0007;
        string Title = "[@所有人]微信群消息屏蔽器V" + Properties.Settings.Default.Version + "- 微信已运行！";

        //变量声明区
        IUIAutomationElementArray listitems;
        IUIAutomationElement messagelist;
        IUIAutomationCondition listitem_con;
        IUIAutomationElement message_old;
        Timer t;
        ISimpleAudioVolume volume;
        Guid g = Guid.Empty;
        IntPtr ChatWndPtr;
        Array old_ID;
        bool UIAStarted = false;
        bool SilentState = false;
        bool WechatRunning = false;
        int pid = -1;
        int count = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "[@所有人]微信群消息屏蔽器V" + Properties.Settings.Default.Version;
            textBox1.Text = Properties.Settings.Default.GroupNameRestored;
            textBox1.Select(textBox1.Text.Length, 0);
            if (textBox1.Text == "" || textBox1.Text == null)
            {
                OK.Enabled = false;
                restore.Enabled = false;
            }
            //检测微信运行状态并取句柄
            timer_detect.Start();
            //北大80.90交友群

        }

        private bool DetectWechatProcess()
        {
            string WeChatMainWndForPC = "WeChatMainWndForPC";
            IntPtr WechatPtr = FindWindow(WeChatMainWndForPC, "微信");

            if (WechatPtr != IntPtr.Zero)
                return true;
            else
            {
                Process[] pp = Process.GetProcessesByName("WeChat");
                int pid = -1;
                foreach (Process p in pp)
                {
                    if (p.ProcessName == "WeChat")
                        pid = p.Id;
                }
                if (pid != -1)
                    return true;
                else
                    return false;
            }
        }

        private void timer_detect_Tick(object sender, EventArgs e)
        {
            //用于检测微信进程是否存在的时钟。UIA运行前：若不存在则将 UIAEnabled 设为false，不开始UIA
            //UIA运行中：检测，若不存在则停止UIAutomation, 并继续持续检测
            WechatRunning = DetectWechatProcess();
            if (!WechatRunning)
            {
                this.Text = "[@所有人]微信群消息屏蔽器V" + Properties.Settings.Default.Version + "- 微信未运行！";
                if (UIAStarted)
                {
                    if (t != null)
                    {
                        t.Stop();
                        t.Dispose();
                    }
                }
                else
                {
                    UIAStarted = false;
                    OK.Enabled = false;
                    restore.Enabled = false;
                }
            }
            else
            {
                this.Text = Title;
                OK.Enabled = true;
                restore.Enabled = true;
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            if (!UIAStarted && this.Text == Title)
            {
                string GroupName = textBox1.Text;

                if (GroupName.Length > 0)//检查群名数组不为空
                {
                    ChatWndPtr = new IntPtr();
                    string ChatWnd = "ChatWnd";
                    bool GroupFound = false;
                    if (GroupName.Length > 0)
                    {
                        ChatWndPtr = FindWindow(ChatWnd, GroupName);
                        if (ChatWndPtr == null || ChatWndPtr == IntPtr.Zero) //找不到输入的群
                        {
                            MessageBox.Show("        找不到群\"" + GroupName + "\"的窗口，请检查输入是否正确，并在微信主面板中将该群窗口拖出。", "窗口未找到", MessageBoxButtons.OK);
                            GroupFound = false;
                        }
                        else
                        {
                            GroupFound = true;
                        }
                    }
                    if (GroupFound)
                    {
                        t = new Timer();
                        UIAStarted = true;

                        Process[] pp = Process.GetProcessesByName("WeChat");
                        foreach (Process p in pp)
                        {
                            if (p.ProcessName == "WeChat")
                                pid = p.Id;
                        }

                        //    volume = GetVolumeObject(pid);
                        //  VolumeSuccess = (volume == null) ? false : true;
                        //    if (volume == null) MessageBox.Show("fff");
                        DisposeWeChatGroup(ChatWndPtr);

                        textBox1.Enabled = false;
                        OK.Enabled = false;
                        OK.Text = "屏蔽中...";
                        if (checkBox1.Checked)
                        {
                            Properties.Settings.Default.GroupNameRestored = textBox1.Text;
                            Properties.Settings.Default.Save();
                        }

                    }
                }
            }

        }

        private IntPtr[] GetAllIndependentWindows()
        {
            //检测所有的独立微信聊天窗口
            CUIAutomationClass factory = new CUIAutomationClass();
            IUIAutomationCondition window = factory.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_WindowControlTypeId);
            IUIAutomationCondition ptr = factory.CreatePropertyCondition(UIA_PropertyIds.UIA_ClassNamePropertyId, "ChatWnd");
            IUIAutomationCondition and = factory.CreateAndCondition(window, ptr);
            IUIAutomationElement root = factory.GetRootElement();
            IUIAutomationElementArray chatwnds = root.FindAll(TreeScope.TreeScope_Descendants, and);
            IntPtr[] GroupPtr = new IntPtr[chatwnds.Length];
            for (int i = 0; i < chatwnds.Length; i++)
            {
                IUIAutomationElement chatwnd = chatwnds.GetElement(i);
                GroupPtr[i] = chatwnd.CurrentNativeWindowHandle;
            }
            return GroupPtr;
        }

        private void DisposeWeChatGroup(IntPtr ChatWndPtr)
        {
            //准备工作
            CUIAutomationClass factory = new CUIAutomationClass();
            IUIAutomationCondition namecondition = factory.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "消息");
            IUIAutomationCondition list = factory.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_ListControlTypeId);
            IUIAutomationCondition and2 = factory.CreateAndCondition(namecondition, list);
            IUIAutomationElement ChatWnd2 = factory.ElementFromHandle(ChatWndPtr);
            messagelist = ChatWnd2.FindFirst(UIAutomationClient.TreeScope.TreeScope_Descendants, and2);
            listitem_con = factory.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_ListItemControlTypeId);
            listitems = messagelist.FindAll(UIAutomationClient.TreeScope.TreeScope_Children, listitem_con);
            message_old = listitems.GetElement(listitems.Length - 1);//时间长容易出ArgumentException
            old_ID = message_old.GetRuntimeId();

            FLASHWINFO fInfo = new FLASHWINFO();
            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = ChatWndPtr;
            fInfo.dwFlags = FLASHW_STOP;
            //fInfo.uCount = UInt32.MaxValue;
            fInfo.uCount = 0;
            fInfo.dwTimeout = 0;
            FlashWindowEx(ref fInfo);
            ShowWindow(ChatWndPtr, SW_HIDE);
            //    SendMessage(ChatWndPtr, WM_SETFOCUS, 0, 0);//消除托盘闪动
            RECT rect = new RECT();
            GetWindowRect(ChatWndPtr, ref rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            int x = rect.Left;
            int y = rect.Top;
            PostMessage(ChatWndPtr, WM_LBUTTONDOWN, 0, (int)(x + width / 2) + ((int)(y + height / 2) << 16));
            PostMessage(ChatWndPtr, WM_LBUTTONUP, 0, (int)(x + width / 2) + ((int)(y + height / 2) << 16));

            //不停检测
            t = new Timer();
            t.Interval = 1500;//比较理想的一组参数：t.Interval=200; silent_timer.interval=1200;
            t.Tick += T_Tick;
            t.Enabled = true;

            //问：为什么偶尔会有漏音
            //答：截获新消息后，需要一段机器时间t_m把微信客户端设成静音。在此期间如果恰好又新来了一条消息，则这个消息的声音就会漏出去
            //理论上，程序运行速度无论怎么提高，只能降低这一现象出现的概率（减少t_m），而无法杜绝该现象的发生。
            //当然，为保证程序基本的运行状况，需要保证t_m<t_audio 其中t_audio为微信收到消息后调用声音播放器所需的机器时间
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (UIAStarted)
            {
                //这里如果写try可以防止运行中微信退出，但写上会影响性能。解决方法是改进异常处理机制，出现异常直接退出程序即可。
                listitems = messagelist.FindAll(TreeScope.TreeScope_Children, listitem_con);
                IUIAutomationElement message_new = listitems.GetElement(listitems.Length - 1);
                Array new_ID = message_new.GetRuntimeId();//RuntimeID是
                if (!new_ID.Equals(old_ID))
                {
                    ShowWindow(ChatWndPtr, SW_HIDE);
                    //SendMessage(ChatWndPtr, WM_SETFOCUS, 0, 0);//消除托盘闪动   每Tick3次Send一次，减少系统资源占用
                    RECT rect = new RECT();
                    GetWindowRect(ChatWndPtr, ref rect);
                    int width = rect.Right - rect.Left;                   
                    int height = rect.Bottom - rect.Top;                  
                    int x = rect.Left;
                    int y = rect.Top;
                    PostMessage(ChatWndPtr, WM_LBUTTONDOWN, 0, (int)(x + width / 2) + ((int)(y + height / 2) << 16));
                    PostMessage(ChatWndPtr, WM_LBUTTONUP, 0, (int)(x + width / 2) + ((int)(y + height / 2) << 16));
                }
                message_old = message_new;
            }
            else  //UIAStarted==false  微信退出等情况
            {
                restore.PerformClick();
            }

        }
        private void silent_timer_Tick(object sender, EventArgs e)
        {
            //过1s再关闭SilentState
            if (SilentState == true)
            {
                SilentState = false;
                silent_timer.Stop();
            }
        }

        private void Help_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
        }
        private void exit_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                Properties.Settings.Default.GroupNameRestored = textBox1.Text;
            }
            else
            {
                Properties.Settings.Default.GroupNameRestored = "";
            }
            Properties.Settings.Default.Save();
            // restore.PerformClick();
            timer_closing.Enabled = true;
            timer_closing.Start();
        }

        private void restore_Click(object sender, EventArgs e)
        {
            if (t != null)
            {
                t.Enabled = false;
                t.Dispose();
            }
            if (ChatWndPtr != null)
            {
                ShowWindow(ChatWndPtr, SW_RESTORE);
            }

            UIAStarted = false;
            textBox1.Enabled = true;
            OK.Enabled = true;
            OK.Text = "一键屏蔽";
        }
        private static ISimpleAudioVolume GetVolumeObject(int pid)
        {
            //必须微信之前响过，才能获得Volume对象，否则会失败

            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                int cpid;
                ctl.GetProcessId(out cpid);

                if (cpid == pid)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.ShowInTaskbar = true;
                    this.WindowState = FormWindowState.Normal;
                }
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exit.PerformClick();
        }

        private void 打开屏蔽器ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
        }
        private string GetString(string val, string str, bool all)
        {
            return Regex.Replace(val, @"(^(" + str + ")" + (all ? "*" : "") + "|(" + str + ")" + (all ? "*" : "") + "$)", "");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox1.Text == null)
            {
                OK.Enabled = false;
            }
            else
            {
                if (WechatRunning)
                {
                    OK.Enabled = true;
                    restore.Enabled = true;
                }
            }
        }

        private void timer_closing_Tick(object sender, EventArgs e)
        {
            double delta = 0.10;
            if (this.Opacity - delta >= 0)
            {
                this.Opacity -= delta;
            }
            else
            {
                this.Opacity = 0;
                timer_closing.Stop();
                this.Dispose();
                Application.Exit();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            exit.PerformClick();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                隐藏ToolStripMenuItem.Enabled = false;
                打开屏蔽器ToolStripMenuItem.Enabled = true;
            }
            if (this.WindowState == FormWindowState.Normal)
            {
                this.ShowInTaskbar = true;
                隐藏ToolStripMenuItem.Enabled = true;
                打开屏蔽器ToolStripMenuItem.Enabled = false;
            }
        }

        private void 隐藏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
    public class VolumeMixer
    {
        public static float? GetApplicationVolume(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            float level;
            volume.GetMasterVolume(out level);
            Marshal.ReleaseComObject(volume);
            return level * 100;
        }

        public static bool? GetApplicationMute(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }

        public static void SetApplicationVolume(int pid, float level)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        public static void SetApplicationMute(int pid, bool mute)
        {
            ISimpleAudioVolume volume = VolumeMixer.GetVolumeObject(pid);
            if (volume == null)
                return;
            Guid guid = Guid.Empty;
            volume.SetMute(mute, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        private static ISimpleAudioVolume GetVolumeObject(int pid)
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                int cpid;
                ctl.GetProcessId(out cpid);
                if (cpid == pid)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl2 Session);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig]
        int NotImpl0();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int NotImpl1();

        [PreserveSig]
        int NotImpl2();

        // IAudioSessionControl2
        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetProcessId(out int pRetVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }
}