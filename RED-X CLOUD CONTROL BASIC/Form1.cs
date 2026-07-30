// RED-X CLOUD CONTROL - Form1.cs
// Session code + WebSocket relay client (no WiFi IP needed)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

namespace RED_X_CLOUD_CONTROL_BASIC
{
    public partial class Form1 : Form
    {
        // ─── Relay WebSocket ───
        private ClientWebSocket relayWs;
        private Thread relayThread;
        private string sessionCode;
        private bool webConnected = false;


        // RELAY SERVER URL - update after deploying on Glitch
        private const string RELAY_URL = "wss://lucas-cheats-relay.onrender.com";

        // Console window handle
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Constants for Windows API interaction
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 256;

        // Hook variables
        private static IntPtr hookID = IntPtr.Zero;

        private Form1.LowLevelKeyboardProc hookCallback;

        private bool waitPressKey;
        private bool waitPressKey1;
        private bool waitPressKey2;
        private bool waitPressKey3;
        private bool waitPressKey4;

        private const int WM_NCLBUTTONDOWN = 161;
        private const int HT_CAPTION = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private bool isAnimating = false;
        private readonly tenzo32 TXCmem = new tenzo32();

        // ── Aimbot DRAG ──
        private readonly string[] TaskName = { "HD-Player" };
        private readonly int ReadOffset = 0xE8;
        private readonly int WriteOffset = 0xB4;
        private readonly string AimbotPattern = "FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43";

        // ── Aimbot HEAD ──
        private readonly int HeadReadOffset  = 0xB8;
        private readonly int HeadWriteOffset = 0xB4;
        private readonly string AimbotHeadPattern = "FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43";

        public Form1()
        {
            InitializeComponent();

            this.hookCallback = new Form1.LowLevelKeyboardProc(this.HookCallback);
            Form1.hookID = this.SetHook(this.hookCallback);

            Application.ApplicationExit += new EventHandler(this.Application_ApplicationExit);
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            Form1.UnhookWindowsHookEx(Form1.hookID);
            relayWs?.Abort();
        }

        private IntPtr SetHook(Form1.LowLevelKeyboardProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (currentProcess.MainModule)
            {
                IntPtr moduleHandle = Form1.GetModuleHandle((string)null);
                return Form1.SetWindowsHookEx(13, proc, moduleHandle, 0U);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)256)
            {
                KeysConverter keysConverter = new KeysConverter();
                string str = keysConverter.ConvertToString((Keys)Marshal.ReadInt32(lParam));

                if (this.waitPressKey)
                {
                    ((Control)this.bindBtn).ForeColor = Color.Red;
                    ((Control)this.bindBtn).Text = str.Equals("Escape") ? "None" : str;
                    this.waitPressKey = false;
                }
                else if (this.waitPressKeyHead)
                {
                    ((Control)this.bindBtnHead).ForeColor = Color.Red;
                    ((Control)this.bindBtnHead).Text = str.Equals("Escape") ? "None" : str;
                    this.waitPressKeyHead = false;
                }
                else
                {
                    Keys keys = (Keys)keysConverter.ConvertFromString(((Control)this.bindBtn).Text.Replace("...", ""));
                    if (keys != Keys.None && (Keys)Marshal.ReadInt32(lParam) == keys)
                    {
                        checkBox1.Checked = !checkBox1.Checked;
                    }

                    Keys keysHead = (Keys)keysConverter.ConvertFromString(((Control)this.bindBtnHead).Text.Replace("...", ""));
                    if (keysHead != Keys.None && (Keys)Marshal.ReadInt32(lParam) == keysHead)
                    {
                        checkBoxHead.Checked = !checkBoxHead.Checked;
                    }
                }
            }
            return Form1.CallNextHookEx(Form1.hookID, nCode, wParam, lParam);
        }

        // Drag aimbot dictionaries
        private readonly Dictionary<long, byte[]> OriginalValue1 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> OriginalValue2 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> ReplacedValue1 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> ReplacedValue2 = new Dictionary<long, byte[]>();

        // Head aimbot dictionaries
        private readonly Dictionary<long, byte[]> HeadOriginalValue1 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> HeadOriginalValue2 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> HeadReplacedValue1 = new Dictionary<long, byte[]>();
        private readonly Dictionary<long, byte[]> HeadReplacedValue2 = new Dictionary<long, byte[]>();

        public bool Aimbot = false;
        private bool waitPressKeyHead = false;
        private bool AimbotHeadToggle = false;

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TXCmem.getTask(TaskName))
                {
                    sta.Text = "STATUS: Emulator Not Found!!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                Process targetProcess = Process.GetProcessesByName("HD-Player").FirstOrDefault();
                if (targetProcess == null)
                {
                    sta.Text = "STATUS: Emulator Not Found!!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                TXCmem.OpenProcess(targetProcess.Id);
                sta.Text = "STATUS: Activating....";
                sta.ForeColor = Color.Green;
                var stopwatch = Stopwatch.StartNew();

                OriginalValue1.Clear(); OriginalValue2.Clear();
                ReplacedValue1.Clear(); ReplacedValue2.Clear();

                IEnumerable<long> addresses = await TXCmem.Trace(AimbotPattern);
                if (addresses == null || !addresses.Any())
                {
                    sta.Text = "STATUS: Error !!!!!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                foreach (long addr in addresses)
                {
                    long readAddr = addr + ReadOffset;
                    long writeAddr = addr + WriteOffset;

                    byte[] readBytes = TXCmem.TraceHead(readAddr.ToString("X"), 4);
                    byte[] writeBytes = TXCmem.TraceHead(writeAddr.ToString("X"), 4);

                    if (readBytes == null || writeBytes == null)
                    {
                        sta.Text = "STATUS: Failed to read memory.";
                        sta.ForeColor = Color.Red;
                        continue;
                    }

                    int readValue = BitConverter.ToInt32(readBytes, 0);
                    int writeValue = BitConverter.ToInt32(writeBytes, 0);

                    OriginalValue1[writeAddr] = writeBytes;
                    OriginalValue2[readAddr] = readBytes;

                    TXCmem.SetHeadBytes(writeAddr.ToString("X"), "int", readValue.ToString());
                    TXCmem.SetHeadBytes(readAddr.ToString("X"), "int", writeValue.ToString());

                    ReplacedValue1[writeAddr] = BitConverter.GetBytes(readValue);
                    ReplacedValue2[readAddr] = BitConverter.GetBytes(writeValue);
                }

                sta.Text = $"STATUS: Aimbot loaded | Time: {stopwatch.Elapsed.TotalSeconds:F2}s";
                sta.ForeColor = Color.Green;
            }
            catch (Exception)
            {
                sta.Text = "STATUS: ERROR";
                sta.ForeColor = Color.Red;
            }
        }

        public void AimbotOFF()
        {
            RestoreValues1(OriginalValue1);
            RestoreValues1(OriginalValue2);
            sta.Text = "STATUS: Aimbot disabled";
            sta.ForeColor = Color.Red;
        }

        public void AimbotON()
        {
            RestoreValues1(ReplacedValue1);
            RestoreValues1(ReplacedValue2);
            sta.Text = "STATUS: Aimbot Enabled <3";
            sta.ForeColor = Color.Green;
        }

        private void RestoreValues1(Dictionary<long, byte[]> dictionary)
        {
            foreach (var entry in dictionary)
            {
                int value = BitConverter.ToInt32(entry.Value, 0);
                TXCmem.SetHeadBytes(entry.Key.ToString("X"), "int", value.ToString());
            }
        }

        private bool AimbotToggle = false;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!AimbotToggle) { AimbotOFF(); AimbotToggle = true; }
            else { AimbotON(); AimbotToggle = false; }
        }

        private void bindBtn_Click(object sender, EventArgs e)
        {
            bindBtn.ForeColor = Color.Red;
            bindBtn.Text = "...";
            waitPressKey = true;
        }

        // ══════════════════════════════════════════
        //  AIMBOT HEAD — Load / ON / OFF
        // ══════════════════════════════════════════
        private async void LoadAimbotHead()
        {
            try
            {
                if (!TXCmem.getTask(TaskName))
                {
                    sta.Text = "STATUS: Emulator Not Found!!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                Process targetProcess = Process.GetProcessesByName("HD-Player").FirstOrDefault();
                if (targetProcess == null)
                {
                    sta.Text = "STATUS: Emulator Not Found!!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                TXCmem.OpenProcess(targetProcess.Id);
                sta.Text = "STATUS: Activating Head...";
                sta.ForeColor = Color.Green;
                var stopwatch = Stopwatch.StartNew();

                HeadOriginalValue1.Clear(); HeadOriginalValue2.Clear();
                HeadReplacedValue1.Clear(); HeadReplacedValue2.Clear();

                IEnumerable<long> addresses = await TXCmem.Trace(AimbotHeadPattern);
                if (addresses == null || !addresses.Any())
                {
                    sta.Text = "STATUS: Head Pattern Not Found!";
                    sta.ForeColor = Color.Red;
                    return;
                }

                foreach (long addr in addresses)
                {
                    long readAddr  = addr + HeadReadOffset;
                    long writeAddr = addr + HeadWriteOffset;

                    byte[] readBytes  = TXCmem.TraceHead(readAddr.ToString("X"),  4);
                    byte[] writeBytes = TXCmem.TraceHead(writeAddr.ToString("X"), 4);

                    if (readBytes == null || writeBytes == null)
                    {
                        sta.Text = "STATUS: Failed to read head memory.";
                        sta.ForeColor = Color.Red;
                        continue;
                    }

                    int readValue  = BitConverter.ToInt32(readBytes,  0);
                    int writeValue = BitConverter.ToInt32(writeBytes, 0);

                    HeadOriginalValue1[writeAddr] = writeBytes;
                    HeadOriginalValue2[readAddr]  = readBytes;

                    TXCmem.SetHeadBytes(writeAddr.ToString("X"), "int", readValue.ToString());
                    TXCmem.SetHeadBytes(readAddr.ToString("X"),  "int", writeValue.ToString());

                    HeadReplacedValue1[writeAddr] = BitConverter.GetBytes(readValue);
                    HeadReplacedValue2[readAddr]  = BitConverter.GetBytes(writeValue);
                }

                sta.Text = $"STATUS: Head loaded | {stopwatch.Elapsed.TotalSeconds:F2}s";
                sta.ForeColor = Color.Green;
            }
            catch (Exception)
            {
                sta.Text = "STATUS: HEAD ERROR";
                sta.ForeColor = Color.Red;
            }
        }

        public void AimbotHeadOFF()
        {
            RestoreValuesHead(HeadOriginalValue1);
            RestoreValuesHead(HeadOriginalValue2);
            sta.Text = "STATUS: Head Aimbot disabled";
            sta.ForeColor = Color.Red;
        }

        public void AimbotHeadON()
        {
            RestoreValuesHead(HeadReplacedValue1);
            RestoreValuesHead(HeadReplacedValue2);
            sta.Text = "STATUS: Head Aimbot Enabled <3";
            sta.ForeColor = Color.Green;
        }

        private void RestoreValuesHead(Dictionary<long, byte[]> dictionary)
        {
            foreach (var entry in dictionary)
            {
                int value = BitConverter.ToInt32(entry.Value, 0);
                TXCmem.SetHeadBytes(entry.Key.ToString("X"), "int", value.ToString());
            }
        }

        private void checkBoxHead_CheckedChanged(object sender, EventArgs e)
        {
            if (!AimbotHeadToggle) { AimbotHeadOFF(); AimbotHeadToggle = true; }
            else { AimbotHeadON(); AimbotHeadToggle = false; }
        }

        private void bindBtnHead_Click(object sender, EventArgs e)
        {
            bindBtnHead.ForeColor = Color.Red;
            bindBtnHead.Text = "...";
            waitPressKeyHead = true;
        }

        // ─── Generate session code (e.g. LC-A1B2C3) ───
        private string GenerateSessionCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            var code = new char[6];
            for (int i = 0; i < 6; i++)
                code[i] = chars[rng.Next(chars.Length)];
            return "LC-" + new string(code);
        }

        // ─── Print styled console banner ───
        private void PrintConsoleBanner(string code)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("  ██╗     ██╗   ██╗ ██████╗ █████╗ ███████╗");
            Console.WriteLine("  ██║     ██║   ██║██╔════╝██╔══██╗██╔════╝");
            Console.WriteLine("  ██║     ██║   ██║██║     ███████║███████╗");
            Console.WriteLine("  ██║     ██║   ██║██║     ██╔══██║╚════██║");
            Console.WriteLine("  ███████╗╚██████╔╝╚██████╗██║  ██║███████║");
            Console.WriteLine("  ╚══════╝ ╚═════╝  ╚═════╝╚═╝  ╚═╝╚══════╝");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("            C H E A T S   v1.0");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  ╔════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ║          LUCAS CHEATS  v1.0               ║");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  ╠════════════════════════════════════════════╣");
            Console.WriteLine("  ║                                            ║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  ║  SESSION CODE:  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {code,-10}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("           ║");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  ║                                            ║");
            Console.WriteLine("  ║  Enter this code in Web Panel to connect  ║");
            Console.WriteLine("  ║  This window closes after connection.      ║");
            Console.WriteLine("  ║                                            ║");
            Console.WriteLine("  ╚════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Waiting for web connection...");
            Console.WriteLine("  [*] This window will close automatically.");
            Console.WriteLine();
            Console.ResetColor();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Size = new System.Drawing.Size(1, 1);
            this.Location = new System.Drawing.Point(-32000, -32000);
            this.Show();

            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "LUCAS CHEATS";

            if (!RunKeyAuthLogin())
            {
                Application.Exit();
                return;
            }

            sessionCode = GenerateSessionCode();
            PrintConsoleBanner(sessionCode);

            relayThread = new Thread(() => StartRelayConnection(sessionCode));
            relayThread.IsBackground = true;
            relayThread.Start();
        }

        private bool RunKeyAuthLogin()
        {
            var auth = new KeyAuth("streamers", "xyk3sgyp9e", "1.0");

            PrintAuthBanner();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Connecting to KeyAuth...");
            Console.ResetColor();

            bool inited = auth.Init();
            if (!inited)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed to connect: {auth.Message}");
                Console.ResetColor();
                Console.WriteLine("\n  Press any key to exit...");
                Console.ReadKey(true);
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  KeyAuth connected.\n");
            Console.ResetColor();

            int attempts = 0;
            while (attempts < 3)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  Username : ");
                Console.ResetColor();
                string user = Console.ReadLine()?.Trim() ?? "";

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  Password : ");
                Console.ResetColor();
                string pass = ReadPassword();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Authenticating...");
                Console.ResetColor();

                if (auth.Login(user, pass))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  {auth.Message}");
                    Console.ResetColor();
                    Thread.Sleep(900);
                    Console.Clear();
                    return true;
                }

                attempts++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  {auth.Message}");
                Console.ResetColor();

                if (attempts < 3)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  {3 - attempts} attempt(s) remaining.\n");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  Too many failed attempts. Exiting...");
            Console.ResetColor();
            Thread.Sleep(2000);
            return false;
        }

        private void PrintAuthBanner()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("  ██╗     ██╗   ██╗ ██████╗ █████╗ ███████╗");
            Console.WriteLine("  ██║     ██║   ██║██╔════╝██╔══██╗██╔════╝");
            Console.WriteLine("  ██║     ██║   ██║██║     ███████║███████╗");
            Console.WriteLine("  ██║     ██║   ██║██║     ██╔══██║╚════██║");
            Console.WriteLine("  ███████╗╚██████╔╝╚██████╗██║  ██║███████║");
            Console.WriteLine("  ╚══════╝ ╚═════╝  ╚═════╝╚═╝  ╚═╝╚══════╝");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("            C H E A T S   v1.0");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  ╔════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ║         LOGIN — CLOUD CONTROL v1.0        ║");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  ╚════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.ResetColor();
        }

        private string ReadPassword()
        {
            var pass = new StringBuilder();
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    pass.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass.Remove(pass.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass.ToString();
        }

        // ─── WebSocket connection to relay ───
        private async void StartRelayConnection(string code)
        {
            try
            {
                relayWs = new ClientWebSocket();
                var uri = new Uri(RELAY_URL);

                // Connect to relay
                await relayWs.ConnectAsync(uri, CancellationToken.None);

                // Register as EXE
                await SendRelayMessage($"{{\"type\":\"register_exe\",\"code\":\"{code}\"}}");

                UpdateConsole("[+] Connected to relay server!");
                UpdateConsole($"[+] Session code: {code}");
                UpdateConsole("[*] Waiting for web to connect...");

                // Listen for messages
                var buffer = new byte[4096];
                while (relayWs.State == WebSocketState.Open)
                {
                    var result = await relayWs.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleRelayMessage(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateConsole($"[!] Relay error: {ex.Message}");
                UpdateConsole("[!] Retrying in 5 seconds...");
                await Task.Delay(5000);
                StartRelayConnection(code); // Retry
            }
        }

        // ─── Handle incoming messages from relay ───
        private async Task HandleRelayMessage(string raw)
        {
            try
            {
                // Simple JSON parsing without external libraries
                if (raw.Contains("\"web_connected\""))
                {
                    UpdateConsole("[+] Web panel connected! Closing terminal...");
                    webConnected = true;
                    await Task.Delay(1500);
                    HideConsole();
                }
                else if (raw.Contains("\"command\""))
                {
                    string action = ExtractJsonValue(raw, "action");
                    string responseText = "";

                    this.Invoke((MethodInvoker)(() =>
                    {
                        switch (action.ToLower())
                        {
                            case "load":
                                // ── AIMBOT DRAG ──
                                button1.PerformClick();
                                Application.DoEvents();
                                Thread.Sleep(100);
                                responseText = sta.Text;
                                break;

                            case "loadhead":
                                // ── AIMBOT HEAD ──
                                LoadAimbotHead();
                                Application.DoEvents();
                                Thread.Sleep(100);
                                responseText = sta.Text;
                                break;

                            case "toggle":
                                checkBox1.Checked = !checkBox1.Checked;
                                Application.DoEvents();
                                Thread.Sleep(50);
                                responseText = sta.Text;
                                break;

                            case "togglehead":
                                checkBoxHead.Checked = !checkBoxHead.Checked;
                                Application.DoEvents();
                                Thread.Sleep(50);
                                responseText = sta.Text;
                                break;

                            // ─────────────────────────────────────────
                            //  BIND KEY ─ target select karne ke baad hi
                            // ─────────────────────────────────────────
                            case "bind":
                                // Direct inline - PerformClick on hidden btn unreliable
                                bindBtn.ForeColor = Color.Red;
                                bindBtn.Text = "...";
                                waitPressKey = true;
                                responseText = "Press a key for Drag bind...";
                                break;

                            case "bindhead":
                                // Direct inline - hidden button PerformClick fix
                                bindBtnHead.ForeColor = Color.Red;
                                bindBtnHead.Text = "...";
                                waitPressKeyHead = true;
                                responseText = "Press a key for Head bind...";
                                break;

                            case "location":
                                button3.PerformClick();
                                Application.DoEvents();
                                Thread.Sleep(50);
                                responseText = sta.Text;
                                break;
                            case "exit":
                                Application.Exit();
                                break;
                        }
                    }));

                    // Send response back to web
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        string escaped = responseText.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        await SendRelayMessage($"{{\"type\":\"response\",\"text\":\"{escaped}\"}}");
                    }
                }
            }
            catch { }
        }

        // ─── Simple JSON value extractor ───
        private string ExtractJsonValue(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        // ─── Send raw JSON string message to relay ───
        private async Task SendRelayMessage(string json)
        {
            if (relayWs?.State != WebSocketState.Open) return;
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await relayWs.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // ─── Console helpers ───
        private void UpdateConsole(string line)
        {
            try
            {
                Console.ForegroundColor = line.StartsWith("[+]") ? ConsoleColor.Green :
                                          line.StartsWith("[!]") ? ConsoleColor.Red :
                                          ConsoleColor.DarkGray;
                Console.WriteLine("  " + line);
                Console.ResetColor();
            }
            catch { }
        }

        private void HideConsole()
        {
            try
            {
                IntPtr hwnd = GetConsoleWindow();
                if (hwnd != IntPtr.Zero)
                    ShowWindow(hwnd, SW_HIDE);
                FreeConsole();
            }
            catch { }
        }

        // ─── Exit button ───
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }



        // === DLL Injection Methods ===
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 0x04;

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, IntPtr dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        private void ExtractEmbeddedResource(string resourceName, string outputPath)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) { MessageBox.Show("Failed to find embedded resource: " + resourceName); return; }
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    stream.CopyTo(fileStream);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string processName = "HD-Player";
            string dllName = "gncbvs.dll";
            string dllResourceName = "RED_X_CLOUD_CONTROL_BASIC.gncbvs.dll";
            string tempDllPath = Path.Combine(Path.GetTempPath(), dllName);

            if (Process.GetProcessesByName(processName).Length == 0)
            {
                sta.Text = "STATUS: Emulator Not Found!!";
                sta.ForeColor = Color.Red;
                return;
            }

            Process targetProcess = Process.GetProcessesByName(processName)[0];
            foreach (ProcessModule module in targetProcess.Modules)
            {
                if (module.ModuleName.Equals(dllName, StringComparison.OrdinalIgnoreCase))
                {
                    sta.Text = "STATUS: CHAMES MENU ALREADY INJECTED BEFORE";
                    sta.ForeColor = Color.Orange;
                    return;
                }
            }

            ExtractEmbeddedResource(dllResourceName, tempDllPath);

            IntPtr hProcess = OpenProcess(
                PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                false, targetProcess.Id);

            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)tempDllPath.Length, MEM_COMMIT, PAGE_READWRITE);

            IntPtr bytesWritten;
            WriteProcessMemory(hProcess, allocMemAddress, Encoding.ASCII.GetBytes(tempDllPath), (uint)tempDllPath.Length, out bytesWritten);
            CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);

            sta.Text = "STATUS: CHAMES MENU INJECTED";
            sta.ForeColor = Color.Green;
        }
    }
}
