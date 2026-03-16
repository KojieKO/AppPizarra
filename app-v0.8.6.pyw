# app-v0.8.6-pro.pyw
# -*- coding: utf-8 -*-
"""
Pizarras Pro v0.8.6 (Mover robusto + Poda agresiva + Log detallado)
- Mueve PDFs a la raíz seleccionada con reintentos (replace -> copy+delete).
- Si en la carpeta origen ya no queda ningún PDF, borra TODO lo demás y elimina la carpeta.
- Log detallado SIEMPRE en %TEMP% (en .pyw no hay consola).
- Pop-up si hay fallos al mover.
"""

import sys, os, time, threading, shutil, platform, ctypes, math, stat, traceback, tempfile
import datetime as _dt

# ==========================================================
# === CONFIGURACIÓN ESTÉTICA ===============================
# ==========================================================

APP_TITLE = "Pizarras Pro v0.8.6"
SCAN_INTERVAL_MS = 2500
TARGET_FOLDER_NAME = "Archivo de pizarra"

COL_CANVAS = "#F1F5F9"
COL_CARD = "#FFFFFF"
COL_TEXT_MAIN = "#0F172A"
COL_TEXT_DIM = "#64748B"
COL_ACCENT = "#2563EB"
COL_BORDER = "#E2E8F0"

FONT_FAMILY = "Segoe UI Variable Text" if os.name == "nt" else "Inter"
FONT_UI = (FONT_FAMILY, 10)
FONT_BOLD = (FONT_FAMILY, 10, "bold")
FONT_TITLE = (FONT_FAMILY, 16, "bold")

try:
    import tkinter as tk
    from tkinter import ttk, messagebox
except Exception:
    sys.exit()

# ==========================================================
# === LOG (DETALLADO + FLUSH + THREAD-SAFE) ================
# ==========================================================

LOG_ENABLED = True
LOG_RESET_ON_START = True   # True = borra el log al abrir la app; False = acumula
LOG_FILE_NAME = "pizarras_pro.log"

_LOG_LOCK = threading.Lock()

def _log_path():
    return os.path.join(tempfile.gettempdir(), LOG_FILE_NAME)

def log(msg: str, level: str = "INFO"):
    if not LOG_ENABLED:
        return
    try:
        ts = _dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
        line = f"[{ts}] [{level}] {msg}\n"
        with _LOG_LOCK:
            # buffering=1 -> line-buffered (flush por línea)
            with open(_log_path(), "a", encoding="utf-8", buffering=1) as f:
                f.write(line)
    except Exception:
        # Si hasta el logger falla, no rompemos la app.
        pass

def _reset_log_if_needed():
    if not LOG_ENABLED or not LOG_RESET_ON_START:
        return
    try:
        with _LOG_LOCK:
            with open(_log_path(), "w", encoding="utf-8") as f:
                f.write("")  # truncar
    except Exception:
        pass

# ==========================================================
# === UTILIDADES ===========================================
# ==========================================================

def _is_windows():
    return platform.system() == "Windows"

def _open_path_default(pth: str):
    try:
        if _is_windows():
            os.startfile(pth)
        elif platform.system() == "Darwin":
            os.system(f"open '{pth}'")
        else:
            os.system(f"xdg-open '{pth}'")
    except Exception as e:
        log(f"_open_path_default ERROR: {pth} -> {type(e).__name__}: {e}", "ERROR")

def _make_writable(path: str):
    try:
        os.chmod(path, stat.S_IWRITE)
    except Exception:
        pass

def _safe_stat_size(path: str) -> int:
    try:
        return os.stat(path).st_size
    except Exception:
        return -1

def _move_file_robusto(src: str, dst: str, retries: int = 8, wait: float = 0.25):
    """
    Mover robusto:
    - Intenta os.replace (rápido/atómico).
    - Si falla, copy2 + remove (fallback).
    - Reintenta por locks típicos (Explorer/AV/visor PDF).
    """
    last_err = None
    os.makedirs(os.path.dirname(dst), exist_ok=True)

    for attempt in range(1, retries + 1):
        try:
            # 1) rename/replace
            try:
                os.replace(src, dst)
                return
            except OSError:
                pass

            # 2) fallback: copiar y borrar origen
            shutil.copy2(src, dst)
            _make_writable(src)
            os.remove(src)
            return

        except Exception as e:
            last_err = e
            log(f"MOVE attempt {attempt}/{retries} FAIL: {src} -> {dst} :: {type(e).__name__}: {e}", "WARN")
            time.sleep(wait)

    raise last_err if last_err else RuntimeError("Move failed (unknown)")

def find_target_roots():
    found = []
    if _is_windows():
        bitmask = ctypes.windll.kernel32.GetLogicalDrives()
        for letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ":
            if bitmask & 1:
                root = f"{letter}:/"
                try:
                    candidate = os.path.join(root, TARGET_FOLDER_NAME)
                    if os.path.isdir(candidate):
                        found.append(candidate)
                except Exception as e:
                    log(f"find_target_roots ERROR: {root} -> {type(e).__name__}: {e}", "ERROR")
            bitmask >>= 1
    return list(dict.fromkeys(found))

def scan_pdfs(root_dir: str):
    items = []
    corruptos = []
    for dirpath, _, files in os.walk(root_dir):
        for name in files:
            if name.lower().endswith(".pdf"):
                full = os.path.join(dirpath, name)
                try:
                    st = os.stat(full)
                    dt = _dt.datetime.fromtimestamp(st.st_mtime)
                    data = {
                        "path": full,
                        "size": st.st_size,
                        "datetime": dt,
                        "date": dt.strftime("%d/%m/%Y"),
                        "time": dt.strftime("%H:%M:%S"),
                    }
                    if st.st_size == 0:
                        corruptos.append(full)
                    items.append(data)
                except Exception as e:
                    log(f"scan_pdfs ERROR: {full} -> {type(e).__name__}: {e}", "ERROR")
    return items, corruptos

def _hay_pdfs_en_arbol(folder_path: str) -> bool:
    for dp, _, files in os.walk(folder_path):
        for f in files:
            if f.lower().endswith(".pdf"):
                return True
    return False

def _on_rm_error(func, path, exc_info):
    """
    Handler para shutil.rmtree:
    - Quita read-only y reintenta.
    """
    try:
        os.chmod(path, stat.S_IWRITE)
        func(path)
    except Exception:
        try:
            et = "".join(traceback.format_exception(*exc_info))
        except Exception:
            et = str(exc_info)
        log(f"rmtree onerror FAIL: func={getattr(func,'__name__','?')} path={path} exc={et}", "ERROR")

def _poda_agresiva(folder_path: str, root_stop: str) -> bool:
    """
    Si ya no quedan PDFs en folder_path (en ningún subnivel):
      - borra TODO el árbol folder_path
      - luego intenta borrar padres vacíos hasta root_stop (sin tocar root_stop)
    """
    folder_path = os.path.abspath(folder_path)
    root_stop = os.path.abspath(root_stop)

    if not os.path.isdir(folder_path):
        log(f"PODA skip (no dir): {folder_path}", "DEBUG")
        return True

    if folder_path == root_stop:
        log(f"PODA skip (root_stop): {folder_path}", "DEBUG")
        return False

    if _hay_pdfs_en_arbol(folder_path):
        log(f"PODA skip (aún hay PDFs): {folder_path}", "DEBUG")
        return False

    log(f"PODA begin: {folder_path}", "INFO")

    for attempt in range(1, 11):
        try:
            try:
                shutil.rmtree(folder_path, onerror=_on_rm_error)
            except FileNotFoundError:
                log(f"PODA ok (ya no existía): {folder_path}", "INFO")
                return True
            except Exception as e:
                log(f"PODA rmtree ERROR attempt {attempt}/10: {folder_path} -> {type(e).__name__}: {e}", "WARN")
                log(traceback.format_exc(), "DEBUG")

            time.sleep(0.25)

            if os.path.isdir(folder_path):
                time.sleep(0.25)
                continue

            # borrar padres vacíos hasta root_stop
            cur = os.path.dirname(folder_path)
            while cur and os.path.abspath(cur) != root_stop:
                try:
                    if os.path.isdir(cur) and not os.listdir(cur):
                        os.rmdir(cur)
                        log(f"PODA parent removed: {cur}", "DEBUG")
                        cur = os.path.dirname(cur)
                        continue
                except Exception:
                    pass
                break

            log(f"PODA ok: {folder_path}", "INFO")
            return True

        except Exception as e:
            log(f"PODA ERROR attempt {attempt}/10: {folder_path} -> {type(e).__name__}: {e}", "WARN")
            time.sleep(0.25)

    try:
        if os.path.isdir(folder_path):
            log(f"PODA FAIL FINAL: sigue existiendo {folder_path}. Contenido: {os.listdir(folder_path)[:50]}", "ERROR")
    except Exception:
        log(f"PODA FAIL FINAL: sigue existiendo {folder_path} (no se pudo listar)", "ERROR")

    return not os.path.isdir(folder_path)

# ==========================================================
# === INTERFAZ PRINCIPAL ===================================
# ==========================================================

class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP_TITLE)
        self.geometry("1000x680")
        self.minsize(800, 500)
        self.configure(bg=COL_CANVAS)

        self.selected_root = tk.StringVar()
        self.items = []
        self.current_roots = []
        self.last_corrupt_check = None
        self._cancel_move = False
        self.sort_column = "datetime"
        self.sort_reverse = True

        self._apply_styles()
        self._build_layout()
        self.after(500, self.auto_scan_loop)

    def _apply_styles(self):
        s = ttk.Style()
        s.theme_use("clam")
        s.configure("TFrame", background=COL_CANVAS)
        s.configure("TLabel", background=COL_CANVAS, foreground=COL_TEXT_MAIN, font=FONT_UI)
        s.configure("TButton", font=FONT_BOLD, background=COL_CARD, foreground=COL_TEXT_MAIN, borderwidth=0, padding=(12, 6))
        s.map("TButton", background=[("active", COL_BORDER)])
        s.configure("Accent.TButton", background=COL_ACCENT, foreground="#FFFFFF")
        s.map("Accent.TButton", background=[("active", "#1D4ED8")], foreground=[("active", "#FFFFFF")])
        s.configure("Treeview", background=COL_CARD, foreground=COL_TEXT_MAIN, fieldbackground=COL_CARD, rowheight=35, font=FONT_UI, borderwidth=0)
        s.map("Treeview", background=[("selected", "#EFF6FF")], foreground=[("selected", COL_ACCENT)])
        s.configure("Treeview.Heading", background=COL_CARD, foreground=COL_TEXT_DIM, font=FONT_BOLD, borderwidth=0, padding=5)
        s.configure("TCombobox", fieldbackground=COL_CARD, background=COL_CARD, arrowsize=15)

    def _build_layout(self):
        header = ttk.Frame(self, padding=(30, 20))
        header.pack(fill="x")
        ttk.Label(header, text="Mis Pizarras Pro", font=FONT_TITLE).pack(side="left")
        self.status_var = tk.StringVar(value="Cargando...")
        ttk.Label(header, textvariable=self.status_var, foreground=COL_TEXT_DIM, font=(FONT_FAMILY, 9)).pack(side="right")

        selector_card = ttk.Frame(self, padding=(30, 0))
        selector_card.pack(fill="x")
        inner = tk.Frame(selector_card, bg=COL_CARD, padx=15, pady=10, highlightthickness=1, highlightbackground=COL_BORDER)
        inner.pack(fill="x")
        tk.Label(inner, text="UBICACIÓN", bg=COL_CARD, fg=COL_TEXT_DIM, font=(FONT_FAMILY, 8, "bold")).pack(side="left", padx=(0, 10))
        self.root_combo = ttk.Combobox(inner, textvariable=self.selected_root, state="readonly", width=40)
        self.root_combo.pack(side="left")
        self.root_combo.bind("<<ComboboxSelected>>", lambda e: self.trigger_scan())

        main_area = ttk.Frame(self, padding=(30, 15))
        main_area.pack(fill="both", expand=True)
        container = tk.Frame(main_area, bg=COL_CARD, highlightthickness=1, highlightbackground=COL_BORDER)
        container.pack(fill="both", expand=True)

        scrollbar = ttk.Scrollbar(container)
        scrollbar.pack(side="right", fill="y")
        self.tree = ttk.Treeview(container, columns=("path", "size", "date"), show="headings", yscrollcommand=scrollbar.set)
        self.tree.heading("path", text="   ARCHIVO", anchor="w", command=lambda: self.set_sort("path"))
        self.tree.heading("size", text="TAMAÑO", anchor="e", command=lambda: self.set_sort("size"))
        self.tree.heading("date", text="FECHA", anchor="center", command=lambda: self.set_sort("datetime"))
        self.tree.column("path", width=450, anchor="w")
        self.tree.column("size", width=100, anchor="e")
        self.tree.column("date", width=150, anchor="center")
        self.tree.pack(fill="both", expand=True)
        scrollbar.config(command=self.tree.yview)
        self.tree.bind("<Double-1>", self._on_double)

        footer = ttk.Frame(self, padding=(30, 15, 30, 20))
        footer.pack(fill="x", side="bottom")
        ttk.Button(footer, text="Refrescar", command=self.trigger_scan).pack(side="left", padx=(0, 10))
        ttk.Button(footer, text="Abrir Carpeta", command=self._on_open_folder).pack(side="left")
        self.btn_move = ttk.Button(footer, text="Mover a raíz", style="Accent.TButton", command=self._on_move)
        self.btn_move.pack(side="right")

    def set_sort(self, col):
        if self.sort_column == col:
            self.sort_reverse = not self.sort_reverse
        else:
            self.sort_column = col
            self.sort_reverse = True
        self._refresh_ui_list()

    def _on_move(self):
        root = self.selected_root.get()
        if not root or "No se" in root:
            return

        selection = self.tree.selection()
        if selection:
            paths = [self.tree.item(i, "values")[0] for i in selection]
            pregunta = f"¿Mover {len(paths)} pizarras seleccionadas?"
        else:
            root_abs = os.path.abspath(root)
            paths = [r["path"] for r in self.items if os.path.dirname(os.path.abspath(r["path"])) != root_abs]
            if not paths:
                messagebox.showinfo("Mover", "Todo está ya en la raíz.")
                return
            pregunta = f"¿Mover {len(paths)} pizarras encontradas a la raíz?"

        if messagebox.askyesno("Confirmar", pregunta):
            self._cancel_move = False
            self._process_move_sequential(paths, root)

    def _process_move_sequential(self, paths, root_dir):
        total = len(paths)
        show_prog = total > 5

        prog_win = None
        pb = None
        perc_lbl = None

        if show_prog:
            prog_win = tk.Toplevel(self)
            prog_win.title("Progreso")
            prog_win.geometry("400x160")
            prog_win.resizable(False, False)
            prog_win.configure(bg=COL_CARD)
            prog_win.grab_set()
            prog_win.protocol("WM_DELETE_WINDOW", lambda: [setattr(self, "_cancel_move", True), prog_win.destroy()])
            x = self.winfo_x() + (self.winfo_width() // 2) - 200
            y = self.winfo_y() + (self.winfo_height() // 2) - 80
            prog_win.geometry(f"+{x}+{y}")
            ttk.Label(prog_win, text="Procesando archivos...", font=FONT_BOLD, background=COL_CARD).pack(pady=(20, 5))
            perc_lbl = ttk.Label(prog_win, text="0%", font=FONT_BOLD, background=COL_CARD, foreground=COL_ACCENT)
            perc_lbl.pack()
            pb = ttk.Progressbar(prog_win, orient="horizontal", length=300, mode="determinate")
            pb.pack(pady=10)

        def run():
            moved = 0
            failed = 0
            last_err = ""

            log(f"MOVE SESSION begin: total={total} root_dir={os.path.abspath(root_dir)}", "INFO")

            for i, src in enumerate(paths):
                if self._cancel_move:
                    log("MOVE SESSION cancelled by user", "WARN")
                    break

                src_abs = os.path.abspath(src)
                folder_to_check = os.path.dirname(src_abs)

                try:
                    base = os.path.basename(src_abs)
                    dst = os.path.join(root_dir, base)
                    k = 1
                    while os.path.exists(dst):
                        n_f, ext = os.path.splitext(base)
                        dst = os.path.join(root_dir, f"{n_f} ({k}){ext}")
                        k += 1

                    size_before = _safe_stat_size(src_abs)
                    t0 = time.perf_counter()
                    log(f"MOVE begin: src={src_abs} dst={os.path.abspath(dst)} size={size_before}", "INFO")

                    _move_file_robusto(src_abs, dst)

                    dt_ms = int((time.perf_counter() - t0) * 1000)
                    size_after = _safe_stat_size(dst)
                    log(f"MOVE ok:   dst={os.path.abspath(dst)} size={size_after} dt_ms={dt_ms}", "INFO")

                    moved += 1
                    time.sleep(0.10)

                    if os.path.abspath(folder_to_check) != os.path.abspath(root_dir):
                        ok = _poda_agresiva(folder_to_check, root_dir)
                        log(f"PODA result: folder={os.path.abspath(folder_to_check)} ok={ok}", "DEBUG")

                except Exception as e:
                    failed += 1
                    last_err = f"{type(e).__name__}: {e}"
                    log(f"MOVE FAIL: src={src_abs} -> {last_err}", "ERROR")
                    log(traceback.format_exc(), "DEBUG")

                if show_prog and pb is not None and perc_lbl is not None:
                    current_perc = math.ceil(((i + 1) / total) * 100)
                    self.after(0, lambda v=current_perc: [pb.configure(value=v), perc_lbl.configure(text=f"{v}%")])

            log(f"MOVE SESSION end: moved={moved} failed={failed}", "INFO")

            def finish():
                if show_prog and prog_win is not None and prog_win.winfo_exists():
                    prog_win.destroy()
                self.trigger_scan()
                if failed:
                    messagebox.showerror(
                        "Finalizado con errores",
                        f"Movidos: {moved}\nFallidos: {failed}\n\nÚltimo error:\n{last_err}\n\nLog:\n{_log_path()}"
                    )
                else:
                    messagebox.showinfo(
                        "Finalizado",
                        f"Movidos: {moved} archivos.\n\nLog:\n{_log_path()}"
                    )

            self.after(0, finish)

        threading.Thread(target=run, daemon=True).start()

    def _on_double(self, event):
        item = self.tree.identify_row(event.y)
        if item:
            _open_path_default(self.tree.item(item, "values")[0])

    def _on_open_folder(self):
        root = self.selected_root.get()
        sel = self.tree.selection()
        if sel:
            _open_path_default(os.path.dirname(self.tree.item(sel[0], "values")[0]))
        elif root and os.path.isdir(root):
            _open_path_default(root)

    def auto_scan_loop(self):
        def task():
            roots = find_target_roots()
            self.after(0, lambda: self._update_roots_ui(roots))
        threading.Thread(target=task, daemon=True).start()
        self.after(SCAN_INTERVAL_MS, self.auto_scan_loop)

    def _update_roots_ui(self, roots):
        if roots != self.current_roots:
            self.current_roots = roots
            self.root_combo["values"] = roots if roots else ["No se detectan unidades"]
            if roots and not self.selected_root.get():
                self.selected_root.set(roots[0])
            self.trigger_scan()

    def trigger_scan(self):
        root = self.selected_root.get()
        if not root or "No se" in root:
            return
        self.status_var.set("Escaneando...")
        threading.Thread(target=self._scan_worker, args=(root,), daemon=True).start()

    def _scan_worker(self, root):
        items, corruptos = scan_pdfs(root)
        self.after(0, lambda: self._refresh_ui(items, corruptos))

    def _refresh_ui(self, items, corruptos):
        self.items = items
        self.status_var.set(f"{len(items)} pizarras encontradas")
        self._refresh_ui_list()

        if corruptos and self.last_corrupt_check != corruptos:
            self.last_corrupt_check = corruptos
            if messagebox.askyesno("Corruptos", f"¿Borrar {len(corruptos)} archivos de 0 KB?"):
                for c in corruptos:
                    try:
                        os.remove(c)
                        log(f"CORRUPT deleted: {os.path.abspath(c)}", "INFO")
                    except Exception as e:
                        log(f"CORRUPT delete FAIL: {os.path.abspath(c)} -> {type(e).__name__}: {e}", "ERROR")
                self.trigger_scan()

    def _refresh_ui_list(self):
        for i in self.tree.get_children():
            self.tree.delete(i)

        sorted_items = sorted(
            self.items,
            key=lambda x: x[self.sort_column] if isinstance(x[self.sort_column], _dt.datetime) else str(x[self.sort_column]).lower(),
            reverse=self.sort_reverse
        )

        for r in sorted_items:
            size_txt = "0 KB (Error)" if r["size"] == 0 else f"{r['size']//1024} KB"
            self.tree.insert("", "end", values=(r["path"], size_txt, f"{r['date']} {r['time']}"))

if __name__ == "__main__":
    if _is_windows():
        try:
            ctypes.windll.shcore.SetProcessDpiAwareness(1)
        except Exception:
            pass

    _reset_log_if_needed()
    log("=== APP START ===", "INFO")
    log(f"LOG FILE: {_log_path()}", "INFO")

    App().mainloop()

    log("=== APP END ===", "INFO")