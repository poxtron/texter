using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Forces the modern msftedit.dll (RichEdit 5.0) instead of riched20.dll (2.0).
// Grammarly specifically hooks into RichEdit window classes; 5.0 is the version
// used by WordPad / modern apps and has the best Grammarly compatibility.
class RichEdit50 : RichTextBox
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string path);

    static RichEdit50() { LoadLibrary("msftedit.dll"); }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassName = "RICHEDIT50W"; return cp; }
    }
}

class TexterForm : Form
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    const int WM_HOTKEY    = 0x0312;
    const int HOTKEY_ID    = 9001;
    const int MOD_CTRL     = 0x0002;
    const int MOD_SHIFT    = 0x0004;
    const int MOD_NOREPEAT = 0x4000;
    const int VK_5         = 0x35;
    const int DWMWA_CORNER = 33;
    const int CORNER_ROUND = 2;

    RichEdit50 _tb;
    NotifyIcon _tray;

    public TexterForm()
    {
        SuspendLayout();

        FormBorderStyle = FormBorderStyle.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        BackColor       = Color.FromArgb(43, 43, 43);
        Opacity         = 0.97;
        Size            = new Size(660, 360);

        // header bar
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

        // RichEdit 5.0 text area
        _tb = new RichEdit50 {
            Dock        = DockStyle.Fill,
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.FromArgb(212, 212, 212),
            Font        = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.None,
            Padding     = new Padding(14, 10, 14, 10),
            WordWrap    = true,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };
        _tb.KeyDown += OnKeyDown;

        Controls.Add(_tb);
        Controls.Add(header);

        // system tray
        _tray = new NotifyIcon {
            Text = "Texter  (Ctrl+Shift+5)", Icon = BuildTrayIcon(), Visible = true,
        };
        var ctx = new ContextMenuStrip();
        ctx.Items.Add("Show / Hide  (Ctrl+Shift+5)", null, (s, e) => Toggle());
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("Exit", null, (s, e) => Application.Exit());
        _tray.ContextMenuStrip = ctx;
        _tray.DoubleClick += (s, e) => Toggle();

        ResumeLayout();

        HandleCreated += (s, e) => {
            int pref = CORNER_ROUND;
            DwmSetWindowAttribute(Handle, DWMWA_CORNER, ref pref, sizeof(int));

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

    // ── keyboard ─────────────────────────────────────────────────────────────

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Shift && e.KeyCode == Keys.Enter)
        {
            CopyAndClose();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.SuppressKeyPress = true;
        }
    }

    void CopyAndClose()
    {
        var text = _tb.Text.TrimEnd('\r', '\n');
        if (!string.IsNullOrWhiteSpace(text))
            Clipboard.SetText(text);
        _tb.Clear();
        Hide();
    }

    // ── show / hide ──────────────────────────────────────────────────────────

    void Toggle()
    {
        if (Visible) Hide();
        else         BringUp();
    }

    void BringUp()
    {
        var ws = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(
            ws.X + (ws.Width  - Width)  / 2,
            ws.Y + (ws.Height - Height) / 2
        );
        Show();
        SetForegroundWindow(Handle);
        _tb.Focus();
        _tb.Select(_tb.TextLength, 0);  // cursor at end
    }

    // ── hotkey message ────────────────────────────────────────────────────────

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            Toggle();
        base.WndProc(ref m);
    }

    // ── cleanup ───────────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        _tray.Visible = false;
        base.OnFormClosing(e);
    }

    // ── tray icon ────────────────────────────────────────────────────────────

    static Icon BuildTrayIcon()
    {
        var bmp = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (var b = new SolidBrush(Color.FromArgb(86, 156, 214)))
            using (var path = RoundedRect(new Rectangle(2, 2, 60, 60), 10))
                g.FillPath(b, path);
            using (var b = new SolidBrush(Color.White))
            {
                g.FillRectangle(b, 13, 13, 38, 10);
                g.FillRectangle(b, 27, 13, 10, 38);
            }
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TexterForm());
    }
}
