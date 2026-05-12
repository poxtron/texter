using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class RichEdit50 : RichTextBox
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string path);
    static RichEdit50() { LoadLibrary("msftedit.dll"); }
    protected override CreateParams CreateParams {
        get { var cp = base.CreateParams; cp.ClassName = "RICHEDIT50W"; return cp; }
    }
}

class TexterForm : Form
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, int mod, int vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int sz);

    const int WM_HOTKEY = 0x0312, HOTKEY_ID = 9001;
    const int MOD_CTRL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000, VK_5 = 0x35;

    readonly RichEdit50 _tb;
    readonly NotifyIcon _tray;

    public TexterForm()
    {
        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true; ShowInTaskbar = false;
        BackColor = Color.FromArgb(43, 43, 43);
        Opacity = 0.97;
        Size = new Size(660, 360);

        var header = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.FromArgb(50, 50, 50) };
        header.Controls.Add(new Label {
            Text = "Texter", ForeColor = Color.FromArgb(170, 170, 170),
            Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(12, 8),
        });
        var hint = new Label {
            Text = "Enter = new line    Shift+Enter = copy & close    Esc = cancel",
            ForeColor = Color.FromArgb(85, 85, 85), Font = new Font("Segoe UI", 8), AutoSize = true,
        };
        header.Controls.Add(hint);
        header.Resize += (s, e) =>
            hint.Location = new Point(header.Width - hint.Width - 12, (header.Height - hint.Height) / 2);

        _tb = new RichEdit50 {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 28, 28), ForeColor = Color.FromArgb(212, 212, 212),
            Font = new Font("Segoe UI", 11), BorderStyle = BorderStyle.None,
            Padding = new Padding(14, 10, 14, 10), WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };
        _tb.KeyDown += OnKeyDown;

        Controls.Add(_tb);
        Controls.Add(header);

        _tray = new NotifyIcon { Text = "Texter  (Ctrl+Shift+5)", Icon = BuildTrayIcon(), Visible = true };
        var ctx = new ContextMenuStrip();
        ctx.Items.Add("Show / Hide  (Ctrl+Shift+5)", null, (s, e) => Toggle());
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("Exit", null, (s, e) => Application.Exit());
        _tray.ContextMenuStrip = ctx;
        _tray.DoubleClick += (s, e) => Toggle();

        ResumeLayout();

        HandleCreated += (s, e) => {
            int r = 2;
            DwmSetWindowAttribute(Handle, 33, ref r, sizeof(int));
            if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_CTRL | MOD_SHIFT | MOD_NOREPEAT, VK_5))
                _tray.ShowBalloonTip(4000, "Texter — hotkey conflict",
                    "Ctrl+Shift+5 is already in use. Open Texter from the tray icon instead.",
                    ToolTipIcon.Warning);
        };
    }

    // start hidden with no taskbar flash
    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated) { CreateHandle(); value = false; }
        base.SetVisibleCore(value);
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Shift && e.KeyCode == Keys.Enter) { CopyAndClose(); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Escape)      { Hide();         e.SuppressKeyPress = true; }
    }

    void CopyAndClose()
    {
        var t = _tb.Text.TrimEnd('\r', '\n');
        if (!string.IsNullOrWhiteSpace(t)) Clipboard.SetText(t);
        _tb.Clear();
        Hide();
    }

    void Toggle() { if (Visible) Hide(); else BringUp(); }

    void BringUp()
    {
        var ws = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(ws.X + (ws.Width - Width) / 2, ws.Y + (ws.Height - Height) / 2);
        Show();
        SetForegroundWindow(Handle);
        _tb.Focus();
        _tb.Select(_tb.TextLength, 0);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) Toggle();
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        _tray.Visible = false;
        base.OnFormClosing(e);
    }

    static Icon BuildTrayIcon()
    {
        var bmp = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (var path = new GraphicsPath())
            using (var blue = new SolidBrush(Color.FromArgb(86, 156, 214)))
            {
                path.AddArc( 2,  2, 20, 20, 180, 90);
                path.AddArc(42,  2, 20, 20, 270, 90);
                path.AddArc(42, 42, 20, 20,   0, 90);
                path.AddArc( 2, 42, 20, 20,  90, 90);
                path.CloseFigure();
                g.FillPath(blue, path);
            }
            using (var white = new SolidBrush(Color.White))
            {
                g.FillRectangle(white, 13, 13, 38, 10);
                g.FillRectangle(white, 27, 13, 10, 38);
            }
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TexterForm());
    }
}
