using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsKeyLogger
{
    public class KeyLogger
    {
        private bool _disposed = false;

        // Callback fucntion https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelKeyboardProc _lowLevelProc;

        private readonly Action _everyKeyHandler;
        private readonly Action _hotKeyHandler;

        // Monitor keyboard input events https://docs.microsoft.com/en-us/windows/win32/winmsg/about-hooks#wh_keyboard_ll
        public const int WM_KEYBOARD_LL = 13;

        // See WinAPI constants http://pinvoke.net/default.aspx/Constants.WM
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;

        private IntPtr _hookID = IntPtr.Zero;

        private bool _controlKeyIsDown = false;

        public Keys ControlKey { get; }
        public Keys LetterKey { get; }

        public KeyLogger(Keys controlKey, Keys letterKey, Action hotKeyHandler)
        {
            ControlKey = controlKey;
            LetterKey = letterKey;

            _hotKeyHandler = hotKeyHandler;

            _lowLevelProc = HookCallback;
        }

        public KeyLogger(Keys controlKey, Keys letterKey, Action everyKeyHandler, Action hotKeyHandler)
        {
            ControlKey = controlKey;
            LetterKey = letterKey;

            _everyKeyHandler = everyKeyHandler;
            _hotKeyHandler = hotKeyHandler;

            _lowLevelProc = HookCallback;
        }

        /// <summary>
        /// Starts listening to the keyboard.
        /// </summary>
        public void Start() => _hookID = SetHook(_lowLevelProc);

        /// <summary>
        /// This method must be called before closing the application.
        /// </summary>
        public void Stop() => UnhookWindowsHookEx(_hookID);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WM_KEYBOARD_LL,
                                        proc,
                                        GetModuleHandle(curModule.ModuleName),
                                        0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                WasKeyDawn(lParam);
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                WasKeyUp(lParam);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void WasKeyUp(IntPtr lParam)
        {
            var key = ReadKey(lParam);

            if (key == ControlKey)
            {
                _controlKeyIsDown = false;
            }
        }

        private void WasKeyDawn(IntPtr lParam)
        {
            var key = ReadKey(lParam);

            _everyKeyHandler?.Invoke();

            if (key == ControlKey)
            {
                _controlKeyIsDown = true;
            }
            else if (_controlKeyIsDown && key == LetterKey)
            {
                _hotKeyHandler?.Invoke();
            }
        }

        private Keys ReadKey(IntPtr lParam)
        {
            var vkCode = Marshal.ReadInt32(lParam);

            return (Keys)vkCode;
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnhookWindowsHookEx(_hookID);
                }
                _disposed = true;
            }
        }

        ~KeyLogger() => Dispose(false);

        #endregion

        #region DllImport

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}
