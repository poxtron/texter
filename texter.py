import tkinter as tk
from tkinter import font as tkfont
import keyboard
import pyperclip
import pystray
from PIL import Image, ImageDraw
import threading
import sys


def _make_tray_icon():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([2, 2, 61, 61], radius=10, fill="#569cd6")
    d.rectangle([14, 13, 50, 22], fill="white")   # T top bar
    d.rectangle([27, 13, 37, 51], fill="white")   # T stem
    return img


class TexterApp:
    def __init__(self):
        self.root = tk.Tk()
        self._build_window()
        self._build_ui()
        self._register_hotkey()
        self._start_tray()

    # ── window ──────────────────────────────────────────────────────────────

    def _build_window(self):
        self.root.withdraw()
        self.root.overrideredirect(True)          # no title bar / borders
        self.root.attributes("-topmost", True)
        self.root.attributes("-alpha", 0.97)
        self.root.configure(bg="#2b2b2b")
        self.root.title("Texter")

        w, h = 640, 340
        sw = self.root.winfo_screenwidth()
        sh = self.root.winfo_screenheight()
        self.root.geometry(f"{w}x{h}+{(sw - w) // 2}+{(sh - h) // 2}")

    def _build_ui(self):
        # 1-px border
        border = tk.Frame(self.root, bg="#4a4a4a")
        border.pack(fill=tk.BOTH, expand=True, padx=1, pady=1)

        # header bar
        header = tk.Frame(border, bg="#323232", height=30)
        header.pack(fill=tk.X)
        header.pack_propagate(False)

        tk.Label(
            header, text="Texter", bg="#323232", fg="#aaaaaa",
            font=("Segoe UI", 9, "bold"),
        ).pack(side=tk.LEFT, padx=10, pady=5)

        tk.Label(
            header,
            text="Enter = new line    Shift+Enter = copy & close    Esc = cancel",
            bg="#323232", fg="#5a5a5a",
            font=("Segoe UI", 8),
        ).pack(side=tk.RIGHT, padx=10, pady=5)

        # text area
        mono = tkfont.Font(family="Segoe UI", size=11)
        self.text = tk.Text(
            border,
            font=mono,
            bg="#1e1e1e",
            fg="#d4d4d4",
            insertbackground="#569cd6",
            selectbackground="#264f78",
            selectforeground="#ffffff",
            relief=tk.FLAT,
            padx=14,
            pady=10,
            wrap=tk.WORD,
            undo=True,
            highlightthickness=0,
            borderwidth=0,
        )
        self.text.pack(fill=tk.BOTH, expand=True)

        self.text.bind("<Shift-Return>", self._copy_and_close)
        self.text.bind("<Escape>", lambda _e: self._hide())

    # ── hotkey ───────────────────────────────────────────────────────────────

    def _register_hotkey(self):
        keyboard.add_hotkey(
            "ctrl+shift+5",
            lambda: self.root.after(0, self._toggle),
        )

    # ── show / hide ──────────────────────────────────────────────────────────

    def _toggle(self):
        if self.root.state() == "withdrawn":
            self._show()
        else:
            self._hide()

    def _show(self):
        self.root.deiconify()
        self.root.lift()
        self.root.focus_force()
        self.text.focus_set()

    def _hide(self):
        self.root.withdraw()

    def _copy_and_close(self, _event=None):
        content = self.text.get("1.0", "end-1c")
        if content.strip():
            pyperclip.copy(content)
        self.text.delete("1.0", tk.END)
        self._hide()
        return "break"     # prevent Shift+Enter from inserting a newline

    # ── system tray ──────────────────────────────────────────────────────────

    def _start_tray(self):
        menu = pystray.Menu(
            pystray.MenuItem(
                "Show / Hide  (Ctrl+Shift+5)",
                lambda: self.root.after(0, self._toggle),
                default=True,
            ),
            pystray.Menu.SEPARATOR,
            pystray.MenuItem("Exit", self._quit),
        )
        self.tray = pystray.Icon("Texter", _make_tray_icon(), "Texter", menu)
        threading.Thread(target=self.tray.run, daemon=True).start()

    def _quit(self):
        keyboard.unhook_all()
        self.tray.stop()
        self.root.after(0, self.root.destroy)

    # ─────────────────────────────────────────────────────────────────────────

    def run(self):
        self.root.mainloop()


if __name__ == "__main__":
    app = TexterApp()
    app.run()
