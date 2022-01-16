# KeyLogger
Useful for embedding with other applications - WinForms or Console.
Global hotkey monitoring is used.
You can handle every keystroke and/or just the hotkey.

## Using
For Console
I do not know how to handle Windows messages using the Windows service, 
but native Service methods OnStop and OnShutdown very useful for calling UnhookWindowsHookEx

If you are using WinForms, it is better to use WndProc and RegisterHotKey/UnregisterHotKey

Windows, .NET Framework 4.8