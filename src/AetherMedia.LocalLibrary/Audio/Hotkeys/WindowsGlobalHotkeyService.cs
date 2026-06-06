// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AetherMedia.LocalLibrary.Audio.Hotkeys;

/// <summary>
/// Windows implementation of <see cref="IGlobalHotkeyService"/> using
/// <c>RegisterHotKey</c> against a message-only window. The window pumps
/// messages on its own background thread; <c>WM_HOTKEY</c> dispatches into
/// <see cref="IGlobalHotkeyService.HotkeyTriggered"/> on that thread.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int WM_QUIT   = 0x0012;

    private readonly object _gate = new();
    private readonly Dictionary<int, HotkeyCommand> _registered = new();
    private Thread? _thread;
    private IntPtr _hwnd;
    private bool _disposed;
    private int _nextId = 1;

    /// <inheritdoc/>
    public bool IsActive { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<HotkeyCommand>? HotkeyTriggered;

    /// <inheritdoc/>
    public void Register(IReadOnlyList<HotkeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsGlobalHotkeyService is Windows-only.");

        EnsureMessageLoop();

        lock (_gate)
        {
            // Drop existing.
            foreach (var id in _registered.Keys.ToArray())
                UnregisterHotKey(_hwnd, id);
            _registered.Clear();

            foreach (var b in bindings)
            {
                var id = _nextId++;
                if (RegisterHotKey(_hwnd, id, (uint)b.Modifiers, (uint)b.KeyCode))
                    _registered[id] = b.Command;
            }
            IsActive = _registered.Count > 0;
        }
    }

    /// <inheritdoc/>
    public void UnregisterAll()
    {
        lock (_gate)
        {
            if (_hwnd == IntPtr.Zero) return;
            foreach (var id in _registered.Keys)
                UnregisterHotKey(_hwnd, id);
            _registered.Clear();
            IsActive = false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterAll();
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread?.Join(1000);
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    private void EnsureMessageLoop()
    {
        if (_thread is not null) return;

        var ready = new ManualResetEventSlim(false);
        _thread = new Thread(() =>
        {
            _hwnd = CreateMessageWindow();
            ready.Set();
            if (_hwnd == IntPtr.Zero) return;

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WM_HOTKEY && _registered.TryGetValue((int)msg.wParam, out var cmd))
                    HotkeyTriggered?.Invoke(this, cmd);
            }
        }) { IsBackground = true, Name = "AetherMedia.GlobalHotkeyLoop" };
        _thread.Start();
        ready.Wait();
    }

    private static IntPtr CreateMessageWindow()
    {
        // HWND_MESSAGE = (HWND)-3 — a message-only window has no UI, no input
        // focus, no taskbar entry; it just receives WM_HOTKEY.
        var msgOnly = new IntPtr(-3);
        return CreateWindowExW(
            dwExStyle: 0,
            lpClassName: "Static",
            lpWindowName: "AetherMedia.HotkeySink",
            dwStyle: 0,
            X: 0, Y: 0, nWidth: 0, nHeight: 0,
            hWndParent: msgOnly,
            hMenu: IntPtr.Zero, hInstance: IntPtr.Zero, lpParam: IntPtr.Zero);
    }

    // ── P/Invoke ────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int X, int Y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
