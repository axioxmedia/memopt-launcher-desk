"""开普勒内存启动器工坊 — forge a silent mem-opt game launcher EXE."""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
import uuid
from pathlib import Path

import httpx
from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse, Response
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field

from axioxmedia import AIO_BRAND, aio_logo_png, aio_watermark, apply_hwnd_icon, axiox_window_title

APP_VERSION = "1.0.0"
APP_TITLE_ZH = "开普勒内存启动器工坊"
APP_TITLE_EN = "KP MemOpt Launcher Desk"
DEFAULT_URL_PREFIX = "steam://launch/"


def app_root() -> Path:
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS)
    return Path(__file__).resolve().parent


ROOT = app_root()
STATIC = ROOT / "static"
TEMPLATE_CS = ROOT / "launcher_template.cs"

app = FastAPI(title=APP_TITLE_ZH, version=APP_VERSION)
app.mount("/assets", StaticFiles(directory=STATIC), name="assets")


def runtime_dir() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent


LOG_FILE = runtime_dir() / "memopt_launcher_desk.log"
LAUNCHERS = runtime_dir() / "launchers"
JOBS_FILE = runtime_dir() / "jobs.json"


def load_jobs() -> dict:
    try:
        return json.loads(JOBS_FILE.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}


def save_job(job_id: str, payload: dict) -> None:
    jobs = load_jobs()
    jobs[job_id] = payload
    try:
        JOBS_FILE.write_text(json.dumps(jobs, ensure_ascii=False, indent=2), encoding="utf-8")
    except OSError:
        pass


def write_log(message: str) -> None:
    try:
        with LOG_FILE.open("a", encoding="utf-8") as fh:
            fh.write(message.rstrip() + "\n")
    except OSError:
        pass


def default_root() -> str:
    if os.name == "nt":
        d = Path("D:/")
        if d.exists():
            return str(Path("D:/Projects/MemOptLaunchers"))
        return str(Path.home() / "MemOptLaunchers")
    for candidate in ("/mnt/d/Projects/MemOptLaunchers", "/media/d/Projects/MemOptLaunchers"):
        if Path(candidate).parent.exists():
            return candidate
    return str(Path.home() / "MemOptLaunchers")


def safe_stem(text: str) -> str:
    cleaned = re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", (text or "").strip())
    cleaned = cleaned.strip(" .")
    return cleaned[:80] or "launcher"


def cs_escape(text: str) -> str:
    return (text or "").replace("\\", "\\\\").replace('"', '\\"')


class GenerateRequest(BaseModel):
    mode: str = Field(pattern="^(url|exe)$")
    target: str
    url_prefix: str = DEFAULT_URL_PREFIX
    dest_root: str = ""


class OpenFolderRequest(BaseModel):
    path: str


@app.get("/")
def index() -> FileResponse:
    return FileResponse(STATIC / "index.html")


@app.get("/brand/logo.png")
def brand_logo() -> Response:
    return Response(content=aio_logo_png(), media_type="image/png")


@app.get("/favicon.ico")
def favicon() -> Response:
    return Response(content=aio_logo_png(), media_type="image/png")


@app.get("/api/defaults")
def api_defaults() -> dict:
    return {
        "version": APP_VERSION,
        "dest": default_root(),
        "url_prefix": DEFAULT_URL_PREFIX,
        "brand": AIO_BRAND,
        "watermark": aio_watermark(),
        "title_zh": APP_TITLE_ZH,
        "title_en": APP_TITLE_EN,
    }


def validate_target(mode: str, target: str, url_prefix: str) -> str:
    text = (target or "").strip()
    if not text:
        raise HTTPException(400, "请先填写启动链接或程序全名")
    if mode == "exe":
        name = Path(text.replace("\\", "/")).name
        if name != text.replace("\\", "/").rstrip("/") and ("/" in text or "\\" in text):
            raise HTTPException(400, "这里只要程序全名，不要路径。例如 KP186F-Win64-Shipping.exe")
        if not name.lower().endswith(".exe"):
            raise HTTPException(400, "程序全名需要以 .exe 结尾")
        if re.search(r'[<>:"/\\|?*\x00-\x1f]', name):
            raise HTTPException(400, "程序全名含有非法字符")
        return name
    prefix = (url_prefix or "").strip() or DEFAULT_URL_PREFIX
    if "://" in text:
        uri = text
    else:
        uri = prefix + text.lstrip("/")
    if "://" not in uri:
        raise HTTPException(400, "启动链接需要带协议前缀，例如 steam://launch/480")
    if any(ch in uri for ch in '<>"|'):
        raise HTTPException(400, "启动链接含有非法字符")
    return uri


def render_csharp(mode: str, target: str) -> str:
    raw = TEMPLATE_CS.read_text(encoding="utf-8")
    process_name = target if mode == "exe" else ""
    if mode == "url" and target.lower().startswith("steam://"):
        process_name = "steam.exe"
    self_exe = f"{safe_stem(Path(target).stem if mode == 'exe' else 'Protocol')}_MemOptLauncher.exe"
    mutex_key = re.sub(r"[^A-Za-z0-9_]", "_", Path(target).stem if mode == "exe" else target)[:48]
    mutex = f"Local\\KPMemOpt_{mutex_key}"
    return (
        raw.replace("{{MODE}}", mode)
        .replace("{{TARGET}}", cs_escape(target))
        .replace("{{PROCESS_NAME}}", cs_escape(process_name))
        .replace("{{SELF_EXE}}", cs_escape(self_exe))
        .replace("{{MUTEX}}", cs_escape(mutex))
    )


def find_csc() -> str | None:
    names = (
        Path(r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
        Path(r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"),
        Path(r"C:\Windows\Microsoft.NET\Framework64\v3.5\csc.exe"),
    )
    for path in names:
        if path.exists():
            return str(path)
    return shutil.which("csc")


def find_cl() -> str | None:
    return shutil.which("cl")


def find_gxx() -> str | None:
    return shutil.which("g++") or shutil.which("x86_64-w64-mingw32-g++")


def compile_csharp(src: Path, out_exe: Path) -> str:
    csc = find_csc()
    if not csc:
        raise HTTPException(
            500,
            "本机没有找到 csc.exe。请安装 .NET Framework 4.x（Windows 自带）后再生成。",
        )
    cmd = [
        csc,
        "/nologo",
        "/optimize+",
        "/target:winexe",
        "/platform:x64",
        "/out:" + str(out_exe),
        str(src),
    ]
    write_log("compile " + " ".join(cmd))
    proc = subprocess.run(cmd, capture_output=True, text=True, timeout=90)
    blob = (proc.stdout or "") + "\n" + (proc.stderr or "")
    write_log(blob)
    if proc.returncode != 0 or not out_exe.exists():
        raise HTTPException(500, "编译失败：\n" + blob.strip()[:1200])
    return "csc"


@app.post("/api/launcher/generate")
def api_generate(req: GenerateRequest) -> dict:
    target = validate_target(req.mode, req.target, req.url_prefix)
    if not TEMPLATE_CS.exists():
        raise HTTPException(500, "缺少 launcher_template.cs")
    job_id = uuid.uuid4().hex[:8]
    dest = Path(req.dest_root.strip() or default_root())
    work = dest / job_id
    try:
        work.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        raise HTTPException(400, f"无法创建输出目录：{exc}") from exc
    stem = safe_stem(Path(target).stem if req.mode == "exe" else "Protocol")
    filename = f"{stem}_MemOptLauncher.exe"
    out_exe = work / filename
    src = work / "Launcher.cs"
    src.write_text(render_csharp(req.mode, target), encoding="utf-8")
    compiler = compile_csharp(src, out_exe)
    size = out_exe.stat().st_size
    write_log(f"generated {out_exe} {size}b via {compiler}")
    payload = {
        "ok": True,
        "job_id": job_id,
        "filename": filename,
        "path": str(out_exe),
        "bytes": size,
        "compiler": compiler,
        "mode": req.mode,
        "target": target,
    }
    save_job(job_id, payload)
    return payload


@app.get("/api/launcher/download/{job_id}")
def api_download(job_id: str) -> FileResponse:
    if not re.fullmatch(r"[0-9a-f]{8}", job_id):
        raise HTTPException(400, "无效任务")
    meta = load_jobs().get(job_id) or {}
    exe = Path(meta["path"]) if meta.get("path") else None
    if exe is None or not exe.exists():
        raise HTTPException(404, "找不到已生成的 EXE")
    return FileResponse(exe, filename=exe.name, media_type="application/vnd.microsoft.portable-executable")


@app.post("/api/folder/open")
def api_open_folder(req: OpenFolderRequest) -> dict:
    path = Path(req.path)
    if not path.exists():
        raise HTTPException(404, "路径不存在")
    folder = path if path.is_dir() else path.parent
    try:
        if os.name == "nt":
            subprocess.Popen(["explorer", "/select,", str(path)] if path.is_file() else ["explorer", str(folder)])
        elif sys.platform == "darwin":
            subprocess.Popen(["open", str(folder)])
        else:
            subprocess.Popen(["xdg-open", str(folder)])
    except OSError as exc:
        raise HTTPException(500, str(exc)) from exc
    return {"ok": True}


def show_error(message: str) -> None:
    write_log(message)
    if os.name == "nt":
        try:
            import ctypes

            ctypes.windll.user32.MessageBoxW(0, message, APP_TITLE_EN, 0x10)
            return
        except Exception:
            pass
    print(message, file=sys.stderr)


def _free_port(preferred: int = 8787) -> int:
    import socket

    for port in (preferred, 8788, 8789, 8790, 0):
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            sock.bind(("127.0.0.1", port))
            chosen = int(sock.getsockname()[1])
        except OSError:
            chosen = -1
        finally:
            sock.close()
        if chosen > 0:
            return chosen
    raise RuntimeError("没有可用的本地端口")


def ensure_stdio() -> None:
    if sys.stdout is None:
        sys.stdout = LOG_FILE.open("a", encoding="utf-8")
    if sys.stderr is None:
        sys.stderr = LOG_FILE.open("a", encoding="utf-8")


def run_server(host: str, port: int, reload: bool = False) -> None:
    import uvicorn

    ensure_stdio()
    if reload:
        uvicorn.run(app, host=host, port=port, reload=True, log_level="warning", log_config=None)
        return
    config = uvicorn.Config(
        app,
        host=host,
        port=port,
        log_level="warning",
        log_config=None,
        lifespan="on",
        access_log=False,
    )
    server = uvicorn.Server(config)
    server.install_signal_handlers = False
    server.run()


def wait_ready(url: str, server_error: list[str], timeout: float = 30.0) -> None:
    import time

    deadline = time.time() + timeout
    while time.time() < deadline:
        if server_error:
            raise RuntimeError(server_error[0])
        try:
            with httpx.Client(timeout=0.8, trust_env=False) as http:
                if http.get(url).status_code < 500:
                    return
        except httpx.HTTPError:
            time.sleep(0.2)
    extra = f"\n服务线程错误：{server_error[0]}" if server_error else ""
    raise RuntimeError(f"本地服务启动超时：{url}{extra}\n日志：{LOG_FILE}")


def run_desktop() -> None:
    import threading
    import traceback
    import webbrowser

    write_log(f"start frozen={getattr(sys, 'frozen', False)} meipass={getattr(sys, '_MEIPASS', '')}")
    write_log(f"static={STATIC} exists={STATIC.exists()}")

    port = _free_port()
    url = f"http://127.0.0.1:{port}"
    write_log(f"bind {url}")
    server_error: list[str] = []

    def _serve() -> None:
        try:
            run_server("127.0.0.1", port, reload=False)
        except Exception:
            server_error.append(traceback.format_exc())
            write_log(server_error[-1])

    thread = threading.Thread(target=_serve, name="uvicorn", daemon=True)
    thread.start()
    wait_ready(f"{url}/api/defaults", server_error)

    try:
        import webview

        window = webview.create_window(
            title=axiox_window_title(),
            url=url,
            width=1100,
            height=780,
            min_size=(880, 640),
            background_color="#0b0d12",
        )

        def paint_chrome(_=None) -> None:
            if os.name != "nt":
                return
            try:
                import ctypes

                hwnd = int(window.native.Handle.ToInt32())
                apply_hwnd_icon(hwnd)
                value = ctypes.c_int(1)
                for attr in (20, 19):
                    ctypes.windll.dwmapi.DwmSetWindowAttribute(
                        hwnd, attr, ctypes.byref(value), ctypes.sizeof(value)
                    )
            except Exception as exc:
                write_log(f"dark titlebar skipped: {exc}")

        try:
            window.events.shown += paint_chrome
        except Exception:
            pass
        webview.start()
        return
    except Exception:
        write_log(traceback.format_exc())
        webbrowser.open(url)
        while thread.is_alive():
            thread.join(timeout=0.5)


if __name__ == "__main__":
    import multiprocessing
    import traceback

    multiprocessing.freeze_support()
    ensure_stdio()
    try:
        desktop = "--web" not in sys.argv and os.environ.get("DEPLOY_DESK_WEB") != "1"
        if desktop:
            run_desktop()
        else:
            run_server("127.0.0.1", _free_port(8787), reload=not getattr(sys, "frozen", False))
    except Exception:
        show_error("启动失败：\n\n" + traceback.format_exc() + f"\n\n日志文件：{LOG_FILE}")
        raise
