using REDXMem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SNIPER_HOLD_LOGIC
{
    public partial class Form1 : Form
    {

        // Constants for hook types and Windows messages
        private const int WH_KEYBOARD_LL = 13;       // Low-level keyboard hook
        private const int WH_MOUSE_LL = 14;          // Low-level mouse hook
        private const int WM_KEYDOWN = 0x0100;       // Key down message
        private const int WM_XBUTTONDOWN = 0x020B;   // Extended mouse button down message (Mouse 4 & 5)
        private const int WM_LBUTTONDOWN = 0x0201;   // Left mouse button down
        private const int WM_RBUTTONDOWN = 0x0204;   // Right mouse button down
        private const int WM_MBUTTONDOWN = 0x0207;   // Middle mouse button down

        // Delegates for hook callbacks
        private static LowLevelKeyboardProc _keyboardProc;
        private static LowLevelMouseProc _mouseProc;

        // Hook handles to track current hooks
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;

        // Variables for tracking the first hotkey (linked to guna2Button1)
        private Keys selectedKey = Keys.None;
        private int selectedMouseButton = -1;
        private bool waitingForKeybind = false;


        // Delegate types for hooks
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();
            // Assign hook callback methods
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            // Set global keyboard and mouse hooks
            _keyboardHookID = SetGlobalHook(WH_KEYBOARD_LL, _keyboardProc);
            _mouseHookID = SetGlobalHook(WH_MOUSE_LL, _mouseProc);

            // Ensure hooks are removed when the form closes
            this.FormClosed += Form1_FormClosed;
        }




        private bool isHoldKeyPressed = false;

        private void HandleUnifiedHotkey(string hotkeyName, Keys? key = null, int? mouseButton = null, bool pressed = false)
        {
            // hotkey: Hold logic
            if (waitingForKeybind)
            {
                selectedKey = key ?? Keys.None;
                selectedMouseButton = mouseButton ?? -1;
                waitingForKeybind = false;

                button2.Invoke((MethodInvoker)(() => button2.Text = $"{hotkeyName}"));
                Status.Invoke((MethodInvoker)(() => Status.Text = $"Hotkey for Sniper Load set to: {hotkeyName}"));
                return;
            }

            bool isMatch = (key.HasValue && selectedKey == key.Value && selectedKey != Keys.None) ||
                           (mouseButton.HasValue && selectedMouseButton == mouseButton.Value && selectedMouseButton != -1);

            if (isMatch)
            {
                if (pressed)
                {
                    if (!isHoldKeyPressed)
                    {
                        isHoldKeyPressed = true;
                        checkBox1.Invoke((MethodInvoker)(() => checkBox1.Checked = true)); // Enable while held
                    }
                }
                else
                {
                    if (isHoldKeyPressed)
                    {
                        isHoldKeyPressed = false;
                        checkBox1.Invoke((MethodInvoker)(() => checkBox1.Checked = false)); // Disable on release
                    }
                }
            }

          

        }



        // Callback for low-level keyboard hook
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Detect key press or release
                if (wParam == (IntPtr)WM_KEYDOWN)
                    HandleUnifiedHotkey(key.ToString(), key: key, pressed: true);
                else if (wParam == (IntPtr)0x0101) // WM_KEYUP
                    HandleUnifiedHotkey(key.ToString(), key: key, pressed: false);

              
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }




        // Callback for low-level mouse hook
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int buttonId = -1;
                string btnName = "";
                bool pressed = false;

                switch ((int)wParam)
                {
                    case 0x0201: // WM_LBUTTONDOWN
                        buttonId = 0; btnName = "Left Mouse"; pressed = true; break;
                    case 0x0202: // WM_LBUTTONUP
                        buttonId = 0; btnName = "Left Mouse"; pressed = false; break;
                    case 0x0204: // WM_RBUTTONDOWN
                        buttonId = 1; btnName = "Right Mouse"; pressed = true; break;
                    case 0x0205: // WM_RBUTTONUP
                        buttonId = 1; btnName = "Right Mouse"; pressed = false; break;
                    case 0x0207: // WM_MBUTTONDOWN
                        buttonId = 2; btnName = "Middle Mouse"; pressed = true; break;
                    case 0x0208: // WM_MBUTTONUP
                        buttonId = 2; btnName = "Middle Mouse"; pressed = false; break;
                    case 0x020B: // WM_XBUTTONDOWN
                        int mouseDataDown = Marshal.ReadInt32((IntPtr)((long)lParam + 8));
                        int xButtonDown = mouseDataDown >> 16;
                        buttonId = 2 + xButtonDown;
                        btnName = buttonId == 3 ? "Mouse4" : "Mouse5";
                        pressed = true;
                        break;
                    case 0x020C: // WM_XBUTTONUP
                        int mouseDataUp = Marshal.ReadInt32((IntPtr)((long)lParam + 8));
                        int xButtonUp = mouseDataUp >> 16;
                        buttonId = 2 + xButtonUp;
                        btnName = buttonId == 3 ? "Mouse4" : "Mouse5";
                        pressed = false;
                        break;
                }

                if (buttonId != -1)
                {
                    HandleUnifiedHotkey(btnName, mouseButton: buttonId, pressed: pressed);
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }


        // Set a global Windows hook
        private static IntPtr SetGlobalHook(int hookId, Delegate proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                ProcessModule curModule = curProcess.MainModule;

                IntPtr hook = SetWindowsHookEx(hookId, proc, GetModuleHandle(curModule.ModuleName), 0);
                if (hook == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to set hook {hookId}");
                }

                return hook;
            }
        }

        // Remove hooks when form is closed
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }

        // DLL Imports for hook management
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);



















        private long aobAddress = 0;
        private string originalBytesHex = null;

        private readonly string originalPattern = "01 00 00 00 9A 99 99 3E FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F 01";
        private readonly string patchPattern =    "01 00 00 00 9A 99 99 3E FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40 00 00 B0 3F 00 80 80 3F 01";

        private async void button1_Click(object sender, EventArgs e)
        {
            // Reset old stored values
            aobAddress = 0;
            originalBytesHex = null;

            Status.Text = "Searching and applying patch...";
            Status.ForeColor = Color.Orange;

            REDX memoryfast = new REDX();
            string[] processName = { "HD-Player" };
            bool success = memoryfast.SetProcess(processName);

            if (!success)
            {
                Status.Text = "Emulator Not Found!!";
                Status.ForeColor = Color.Crimson;
                return;
            }

            var result = await memoryfast.AoBScan(originalPattern);
            var matches = result.ToList();


            if (matches.Count != 1)
            {
                Status.Text = $"Error! values found: {matches.Count()} ";
                Status.ForeColor = Color.Crimson;
                return;
            }

            aobAddress = matches[0];

            int originalLength = originalPattern.Split(' ').Length;
            originalBytesHex = memoryfast.ReadString(aobAddress, originalLength);

          
            Status.Text = $"Sniper scope loaded | Bind a key to go";
            Console.Beep(1000, 100);
            Status.ForeColor = Color.LimeGreen;

        }

        private async void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            if (aobAddress == 0 || string.IsNullOrEmpty(originalBytesHex))
            {
                Status.Text = "Load sniper scope first!";
                Status.ForeColor = Color.Crimson;
                ((System.Windows.Forms.CheckBox)sender).Checked = false;
                return;
            }

            REDX memoryfast = new REDX();
            string[] processName = { "HD-Player" };
            bool success = memoryfast.SetProcess(processName);

            if (!success)
            {
                Status.Text = "Emulator Not Found!!";
                Status.ForeColor = Color.Crimson;
                ((System.Windows.Forms.CheckBox)sender).Checked = false;
                return;
            }

            bool result;
            var toggle = (System.Windows.Forms.CheckBox)sender;

            if (toggle.Checked)
            {
                // Toggle ON = apply patch (sniper on)
                result = memoryfast.AobReplace(aobAddress, patchPattern);

                if (result)
                {
                    Status.Text = "Sniper scope applied";
                    Status.ForeColor = Color.LimeGreen;
                }
                else
                {
                    Status.Text = "Sniper scope failed!";
                    Status.ForeColor = Color.Crimson;
                }

              
            }
            else
            {
                // Toggle OFF = restore original bytes (sniper off)
                result = memoryfast.AobReplace(aobAddress, originalBytesHex);

                if (result)
                {
                    Status.Text = "Sniper scope restored";
                    Status.ForeColor = Color.Orange;
                }
                else
                {
                    Status.Text = "Restore failed!";
                    Status.ForeColor = Color.Crimson;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Status.Text = "Press any key or mouse button...";
            button2.Text = "...";
            selectedKey = Keys.None;
            selectedMouseButton = -1;
            waitingForKeybind = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
