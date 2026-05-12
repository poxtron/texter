using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// ── RichEdit 5.0 ─────────────────────────────────────────────────────────────

class RichEdit50 : RichTextBox
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string path);
    static RichEdit50() { LoadLibrary("msftedit.dll"); }
    protected override CreateParams CreateParams {
        get { var cp = base.CreateParams; cp.ClassName = "RICHEDIT50W"; return cp; }
    }
}

// ── Hotkey ────────────────────────────────────────────────────────────────────

struct Hotkey
{
    public Keys Key;
    public bool Ctrl, Shift, Alt;

    public static Hotkey Parse(string s)
    {
        var h = new Hotkey();
        foreach (var part in s.Split('+'))
        {
            var p = part.Trim();
            if      (p == "Ctrl")  h.Ctrl  = true;
            else if (p == "Shift") h.Shift = true;
            else if (p == "Alt")   h.Alt   = true;
            else h.Key = NameToKey(p);
        }
        return h;
    }

    static Keys NameToKey(string s)
    {
        if (s.Length == 1 && s[0] >= '0' && s[0] <= '9')
            return (Keys)((int)Keys.D0 + (s[0] - '0'));
        if (s == "Enter") return Keys.Return;
        Keys k;
        return Enum.TryParse(s, true, out k) ? k : Keys.None;
    }

    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Ctrl)  parts.Add("Ctrl");
        if (Alt)   parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(KeyToName(Key));
        return string.Join("+", parts.ToArray());
    }

    static string KeyToName(Keys k)
    {
        if (k >= Keys.D0 && k <= Keys.D9) return ((int)k - (int)Keys.D0).ToString();
        if (k == Keys.Return) return "Enter";
        return k.ToString();
    }

    public bool Matches(KeyEventArgs e)
    {
        return e.KeyCode == Key && e.Control == Ctrl && e.Shift == Shift && e.Alt == Alt;
    }

    public int WinMod {
        get {
            int m = 0x4000;
            if (Ctrl)  m |= 0x0002;
            if (Alt)   m |= 0x0001;
            if (Shift) m |= 0x0004;
            return m;
        }
    }

    public int Vk { get { return (int)Key; } }
}

// ── AppSettings ───────────────────────────────────────────────────────────────

class AppSettings
{
    static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Texter", "settings.txt");

    public bool   UseActiveMonitor = true;
    public string ToggleHotkey     = "Ctrl+Shift+5";
    public string CopyCloseKey     = "Shift+Enter";
    public string CancelKey        = "Escape";
    public string FontFamily       = "Segoe UI";
    public float  FontSize         = 11f;
    public Color  TextColor        = Color.FromArgb(212, 212, 212);
    public Color  BgColor          = Color.FromArgb(28, 28, 28);
    public int    Transparency     = 97;

    public static AppSettings Load()
    {
        var s = new AppSettings();
        try {
            if (!File.Exists(FilePath)) return s;
            var ln = File.ReadAllLines(FilePath);
            if (ln.Length > 0) s.UseActiveMonitor = ln[0] == "Active";
            if (ln.Length > 1) s.ToggleHotkey     = ln[1];
            if (ln.Length > 2) s.CopyCloseKey     = ln[2];
            if (ln.Length > 3) s.CancelKey        = ln[3];
            if (ln.Length > 4) {
                var p = ln[4].Split('|');
                if (p[0].Length > 0) s.FontFamily = p[0];
                float f;
                if (p.Length > 1 && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out f) && f > 0)
                    s.FontSize = f;
            }
            if (ln.Length > 5) { var c = ParseColor(ln[5]); if (!c.IsEmpty) s.TextColor = c; }
            if (ln.Length > 6) { var c = ParseColor(ln[6]); if (!c.IsEmpty) s.BgColor   = c; }
            if (ln.Length > 7) {
                int t;
                if (int.TryParse(ln[7], out t) && t >= 10 && t <= 100) s.Transparency = t;
            }
        } catch { }
        return s;
    }

    public void Save()
    {
        try {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
            File.WriteAllLines(FilePath, new[] {
                UseActiveMonitor ? "Active" : "Primary",
                ToggleHotkey, CopyCloseKey, CancelKey,
                FontFamily + "|" + FontSize.ToString(CultureInfo.InvariantCulture),
                TextColor.R + "," + TextColor.G + "," + TextColor.B,
                BgColor.R   + "," + BgColor.G   + "," + BgColor.B,
                Transparency.ToString()
            });
        } catch { }
    }

    static Color ParseColor(string s)
    {
        var p = s.Split(',');
        if (p.Length != 3) return Color.Empty;
        int r, g, b;
        if (int.TryParse(p[0].Trim(), out r) &&
            int.TryParse(p[1].Trim(), out g) &&
            int.TryParse(p[2].Trim(), out b))
            return Color.FromArgb(r, g, b);
        return Color.Empty;
    }
}

// ── HotkeyBox ─────────────────────────────────────────────────────────────────

class HotkeyBox : TextBox
{
    Hotkey _h;

    public Hotkey Value {
        get { return _h; }
        set { _h = value; Text = value.ToString(); }
    }

    public HotkeyBox() { ReadOnly = true; Cursor = Cursors.IBeam; Width = 150; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
            e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            return;
        _h = new Hotkey { Key = e.KeyCode, Ctrl = e.Control, Shift = e.Shift, Alt = e.Alt };
        Text = _h.ToString();
    }
}

// ── SettingsForm ──────────────────────────────────────────────────────────────

class SettingsForm : Form
{
    readonly RadioButton  _rbActive, _rbPrimary;
    readonly HotkeyBox    _hbToggle, _hbCopyClose, _hbCancel;
    string        _fontFamily;
    float         _fontSize;
    Panel         _swatchText, _swatchBg;
    NumericUpDown _nudTrans;

    public SettingsForm(AppSettings s)
    {
        Text = "Texter Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 400);
        Font = new Font("Segoe UI", 9);

        // ── Monitor ──────────────────────────────────────────────────────────
        var grpMon = new GroupBox { Text = "Monitor", Left = 12, Top = 8, Width = 336, Height = 72 };
        _rbActive  = new RadioButton { Text = "Active monitor (where the cursor is)", AutoSize = true,
                                       Location = new Point(12, 18), Checked =  s.UseActiveMonitor };
        _rbPrimary = new RadioButton { Text = "Primary monitor", AutoSize = true,
                                       Location = new Point(12, 44), Checked = !s.UseActiveMonitor };
        grpMon.Controls.Add(_rbActive);
        grpMon.Controls.Add(_rbPrimary);

        // ── Keyboard shortcuts ────────────────────────────────────────────────
        var grpKeys = new GroupBox { Text = "Keyboard shortcuts", Left = 12, Top = 90, Width = 336, Height = 112 };
        _hbToggle    = new HotkeyBox { Value = Hotkey.Parse(s.ToggleHotkey) };
        _hbCopyClose = new HotkeyBox { Value = Hotkey.Parse(s.CopyCloseKey) };
        _hbCancel    = new HotkeyBox { Value = Hotkey.Parse(s.CancelKey)    };
        AddRow(grpKeys, "Open / hide",  _hbToggle,    16);
        AddRow(grpKeys, "Copy & close", _hbCopyClose, 46);
        AddRow(grpKeys, "Cancel",       _hbCancel,    76);

        // ── Appearance ────────────────────────────────────────────────────────
        _fontFamily = s.FontFamily;
        _fontSize   = s.FontSize;

        var grpApp = new GroupBox { Text = "Appearance", Left = 12, Top = 212, Width = 336, Height = 144 };

        var btnFont = new Button {
            Text = FormatFont(_fontFamily, _fontSize),
            Width = 190, Height = 23, Location = new Point(130, 18),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        btnFont.Click += (sender, e) => {
            using (var dlg = new FontDialog { Font = SafeFont(_fontFamily, _fontSize), ShowEffects = false })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _fontFamily  = dlg.Font.Name;
                _fontSize    = dlg.Font.Size;
                btnFont.Text = FormatFont(_fontFamily, _fontSize);
            }
        };
        grpApp.Controls.Add(new Label { Text = "Font:", AutoSize = true, Location = new Point(10, 22) });
        grpApp.Controls.Add(btnFont);

        _swatchText          = MakeSwatch(s.TextColor);
        _swatchText.Location = new Point(130, 50);
        grpApp.Controls.Add(new Label { Text = "Text color:", AutoSize = true, Location = new Point(10, 53) });
        grpApp.Controls.Add(_swatchText);

        _swatchBg          = MakeSwatch(s.BgColor);
        _swatchBg.Location = new Point(130, 82);
        grpApp.Controls.Add(new Label { Text = "Background:", AutoSize = true, Location = new Point(10, 85) });
        grpApp.Controls.Add(_swatchBg);

        _nudTrans = new NumericUpDown {
            Minimum = 10, Maximum = 100, Value = s.Transparency,
            Width = 58, Location = new Point(130, 113),
        };
        grpApp.Controls.Add(new Label { Text = "Transparency:", AutoSize = true, Location = new Point(10, 116) });
        grpApp.Controls.Add(_nudTrans);
        grpApp.Controls.Add(new Label { Text = "%", AutoSize = true, Location = new Point(194, 116) });

        // ── Buttons ───────────────────────────────────────────────────────────
        var btnOK     = new Button { Text = "OK",     Width = 80, Height = 26, DialogResult = DialogResult.OK     };
        var btnCancel = new Button { Text = "Cancel", Width = 80, Height = 26, DialogResult = DialogResult.Cancel };
        btnOK.Location     = new Point(ClientSize.Width - 90,  ClientSize.Height - 36);
        btnCancel.Location = new Point(ClientSize.Width - 178, ClientSize.Height - 36);
        AcceptButton = btnOK;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[] { grpMon, grpKeys, grpApp, btnOK, btnCancel });
    }

    static void AddRow(GroupBox grp, string label, HotkeyBox box, int y)
    {
        grp.Controls.Add(new Label { Text = label + ":", AutoSize = true, Location = new Point(10, y + 3) });
        box.Location = new Point(130, y);
        grp.Controls.Add(box);
    }

    static Panel MakeSwatch(Color c)
    {
        var p = new Panel { BackColor = c, Width = 26, Height = 22,
                            BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
        p.Click += (s, e) => {
            using (var dlg = new ColorDialog { Color = p.BackColor, FullOpen = true })
                if (dlg.ShowDialog() == DialogResult.OK)
                    p.BackColor = dlg.Color;
        };
        return p;
    }

    static string FormatFont(string family, float size)
    {
        return family + ", " + (int)size + "pt";
    }

    static Font SafeFont(string family, float size)
    {
        try   { return new Font(family, size); }
        catch { return SystemFonts.DefaultFont; }
    }

    public void Apply(AppSettings s)
    {
        s.UseActiveMonitor = _rbActive.Checked;
        s.ToggleHotkey     = _hbToggle.Value.ToString();
        s.CopyCloseKey     = _hbCopyClose.Value.ToString();
        s.CancelKey        = _hbCancel.Value.ToString();
        s.FontFamily       = _fontFamily;
        s.FontSize         = _fontSize;
        s.TextColor        = _swatchText.BackColor;
        s.BgColor          = _swatchBg.BackColor;
        s.Transparency     = (int)_nudTrans.Value;
    }
}

// ── TexterForm ────────────────────────────────────────────────────────────────

class TexterForm : Form
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, int mod, int vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int sz);

    const int WM_HOTKEY = 0x0312, HOTKEY_ID = 9001;

    readonly RichEdit50 _tb;
    readonly NotifyIcon _tray;
    readonly Label      _hint;
    AppSettings         _cfg;
    Hotkey              _copyCloseHk, _cancelHk;

    public TexterForm()
    {
        _cfg = AppSettings.Load();

        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true; ShowInTaskbar = false;
        BackColor = Color.FromArgb(43, 43, 43);
        Size = new Size(660, 360);

        var header = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.FromArgb(50, 50, 50) };
        header.Controls.Add(new Label {
            Text = "Texter", ForeColor = Color.FromArgb(170, 170, 170),
            Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(12, 8),
        });
        _hint = new Label { ForeColor = Color.FromArgb(85, 85, 85), Font = new Font("Segoe UI", 8), AutoSize = true };
        header.Controls.Add(_hint);

        Action reposition = () => {
            if (header.Width > 0)
                _hint.Location = new Point(header.Width - _hint.Width - 12, (header.Height - _hint.Height) / 2);
        };
        header.Resize     += (s, e) => reposition();
        _hint.SizeChanged += (s, e) => reposition();

        _tb = new RichEdit50 {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(14, 10, 14, 10), WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };
        _tb.KeyDown += OnKeyDown;

        Controls.Add(_tb);
        Controls.Add(header);

        _tray = new NotifyIcon { Icon = BuildTrayIcon(), Visible = true };
        var ctx = new ContextMenuStrip();
        ctx.Items.Add("Show / Hide", null, (s, e) => Toggle());
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("Settings...", null, (s, e) => OpenSettings());
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("Exit", null, (s, e) => Application.Exit());
        _tray.ContextMenuStrip = ctx;
        _tray.DoubleClick += (s, e) => Toggle();

        ResumeLayout();
        ApplySettings(false);

        HandleCreated += (s, e) => {
            int r = 2;
            DwmSetWindowAttribute(Handle, 33, ref r, sizeof(int));
            RegisterToggleHotkey();
        };
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated) { CreateHandle(); value = false; }
        base.SetVisibleCore(value);
    }

    // ── settings ──────────────────────────────────────────────────────────────

    void OpenSettings()
    {
        using (var dlg = new SettingsForm(_cfg))
        {
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                UnregisterHotKey(Handle, HOTKEY_ID);
                dlg.Apply(_cfg);
                _cfg.Save();
                ApplySettings(true);
            }
        }
    }

    void ApplySettings(bool reRegister)
    {
        _copyCloseHk = Hotkey.Parse(_cfg.CopyCloseKey);
        _cancelHk    = Hotkey.Parse(_cfg.CancelKey);
        _hint.Text   = string.Format("Enter = new line    {0} = copy & close    {1} = cancel",
                           _cfg.CopyCloseKey, _cfg.CancelKey);
        var tip = "Texter  (" + _cfg.ToggleHotkey + ")";
        _tray.Text = tip.Length > 63 ? tip.Substring(0, 60) + "..." : tip;

        _tb.Font      = SafeFont(_cfg.FontFamily, _cfg.FontSize);
        _tb.ForeColor = _cfg.TextColor;
        _tb.BackColor = _cfg.BgColor;
        Opacity       = _cfg.Transparency / 100.0;

        if (reRegister) RegisterToggleHotkey();
    }

    void RegisterToggleHotkey()
    {
        var hk = Hotkey.Parse(_cfg.ToggleHotkey);
        if (!RegisterHotKey(Handle, HOTKEY_ID, hk.WinMod, hk.Vk))
            _tray.ShowBalloonTip(4000, "Texter — hotkey conflict",
                _cfg.ToggleHotkey + " is already in use. Use the tray icon instead.",
                ToolTipIcon.Warning);
    }

    static Font SafeFont(string family, float size)
    {
        try   { return new Font(family, size); }
        catch { return new Font("Segoe UI", 11); }
    }

    // ── keyboard ──────────────────────────────────────────────────────────────

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if      (_copyCloseHk.Matches(e)) { CopyAndClose(); e.SuppressKeyPress = true; }
        else if (_cancelHk.Matches(e))    { Hide();         e.SuppressKeyPress = true; }
    }

    void CopyAndClose()
    {
        var t = _tb.Text.TrimEnd('\r', '\n');
        if (!string.IsNullOrWhiteSpace(t)) Clipboard.SetText(t);
        _tb.Clear();
        Hide();
    }

    // ── show / hide ───────────────────────────────────────────────────────────

    void Toggle() { if (Visible) Hide(); else BringUp(); }

    void BringUp()
    {
        var screen = _cfg.UseActiveMonitor ? Screen.FromPoint(Cursor.Position) : Screen.PrimaryScreen;
        var ws = screen.WorkingArea;
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

    // ── tray icon ─────────────────────────────────────────────────────────────

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
