const AXIOXMEDIA_BRAND = "Axiox Media";
const AIO_WATERMARK = "axioxmedia";

const I18N = {
  zh: {
    appTitle: "开普勒内存启动器工坊",
    appSubtitle: "两步生成带内存优化与优先级保护的启动 EXE",
    step1: "方式",
    step2: "目标",
    step3: "取走",
    step1Title: "选择启动方式",
    step1Lead: "决定生成出来的启动器是打开协议链接，还是按程序全名查找并拉起游戏。",
    modeUrlKicker: "Protocol",
    modeUrlTitle: "启动链接",
    modeUrlBody: "任意协议前缀。默认预填 steam://launch/，可改成 steam://run/ 或其他商店协议。",
    modeExeKicker: "Process",
    modeExeTitle: "游戏程序全名",
    modeExeBody: "只填文件名，例如 KP186F-Win64-Shipping.exe。不需要路径，启动器会在磁盘根目录树里查找。",
    next: "下一步",
    back: "上一步",
    generate: "生成 EXE",
    step2UrlTitle: "填写启动链接",
    step2UrlLead: "前缀可改。可只填应用 ID，也可直接贴完整 URI。",
    step2ExeTitle: "填写程序全名",
    step2ExeLead: "只要根目录树里能找到这个文件名即可，不必和启动器放在同一文件夹。",
    prefixLabel: "协议前缀",
    urlRestLabel: "链接内容",
    urlRestPh: "480 或完整 steam://launch/480",
    exeLabel: "程序全名（不是路径）",
    exePh: "KP186F-Win64-Shipping.exe",
    exeHint: "启动器会先看自己所在目录及子目录，再扫固定磁盘根目录。游戏不必和启动器放在同一文件夹。",
    destLabel: "输出目录",
    step3Title: "取走启动器",
    step3Lead: "把这个 EXE 放到任意位置运行。它会先清内存，再启动目标，并每分钟继续优化、拉高游戏优先级。",
    again: "再生成一个",
    openFolder: "打开所在文件夹",
    download: "下载 EXE",
    compiling: "正在编译启动器…",
    previewPrefix: "将写入启动器的链接：",
    needTarget: "请先填好这一步再生成。",
    genOk: "已生成。",
  },
  en: {
    appTitle: "KP MemOpt Launcher Desk",
    appSubtitle: "Two steps to forge a memory-optimizing launch EXE",
    step1: "Mode",
    step2: "Target",
    step3: "Take",
    step1Title: "Pick how it should start",
    step1Lead: "The forged launcher either opens a protocol URI or finds a game by file name.",
    modeUrlKicker: "Protocol",
    modeUrlTitle: "Launch URI",
    modeUrlBody: "Any prefix. Default is steam://launch/ — change it to steam://run/ or another store protocol.",
    modeExeKicker: "Process",
    modeExeTitle: "Game exe name",
    modeExeBody: "File name only, e.g. KP186F-Win64-Shipping.exe. No path. The launcher searches drive roots.",
    next: "Next",
    back: "Back",
    generate: "Forge EXE",
    step2UrlTitle: "Enter the launch URI",
    step2UrlLead: "The prefix is editable. Paste an app id or a full URI.",
    step2ExeTitle: "Enter the exe file name",
    step2ExeLead: "Only the file name is required. It does not have to live next to the launcher.",
    prefixLabel: "Protocol prefix",
    urlRestLabel: "URI body",
    urlRestPh: "480 or full steam://launch/480",
    exeLabel: "Exe file name (not a path)",
    exePh: "KP186F-Win64-Shipping.exe",
    exeHint: "The launcher searches its own folder tree first, then fixed drive roots.",
    destLabel: "Output folder",
    step3Title: "Take the launcher",
    step3Lead: "Run this EXE anywhere. It trims memory, starts the target, then keeps optimizing and boosting priority every minute.",
    again: "Forge another",
    openFolder: "Open folder",
    download: "Download EXE",
    compiling: "Compiling launcher…",
    previewPrefix: "URI written into the launcher: ",
    needTarget: "Fill this step before generating.",
    genOk: "Ready.",
  },
};

const LAST = 2;
let currentStep = 0;
let uiLang = "zh";
let mode = "url";
let lastJob = null;

function detectUiLang() {
  const saved = localStorage.getItem("aio.uiLang");
  if (saved === "zh" || saved === "en") return saved;
  return (navigator.language || "").toLowerCase().startsWith("zh") ? "zh" : "en";
}

function t(key) {
  return (I18N[uiLang] && I18N[uiLang][key]) || I18N.zh[key] || key;
}

function applyI18n() {
  document.documentElement.lang = uiLang === "zh" ? "zh-CN" : "en";
  document.querySelectorAll("[data-i18n]").forEach((el) => {
    const key = el.getAttribute("data-i18n");
    if (key) el.textContent = t(key);
  });
  document.querySelectorAll("[data-i18n-placeholder]").forEach((el) => {
    const key = el.getAttribute("data-i18n-placeholder");
    if (key) el.setAttribute("placeholder", t(key));
  });
  document.querySelectorAll("#uiLangSwitch button").forEach((btn) => {
    btn.classList.toggle("on", btn.getAttribute("data-ui-lang") === uiLang);
  });
  syncStep2Copy();
  renderStepNav();
  refreshUrlPreview();
}

function hideAllStages() {
  for (let i = 0; i <= LAST; i++) {
    document.getElementById("stage" + i)?.classList.remove("on");
  }
}

function goStep(n) {
  currentStep = n;
  hideAllStages();
  document.getElementById("stage" + n)?.classList.add("on");
  renderStepNav();
  applyI18n();
}

function renderStepNav() {
  const labels = [t("step1"), t("step2"), t("step3")];
  const nav = document.getElementById("stepNav");
  nav.innerHTML = labels
    .map((label, i) => {
      const cls = i === currentStep ? "on" : i < currentStep ? "ok" : "";
      return `<button type="button" class="step-pill ${cls}" data-step="${i}">${String(i + 1).padStart(2, "0")} ${label}</button>`;
    })
    .join("");
  nav.querySelectorAll("button").forEach((btn) => {
    btn.addEventListener("click", () => {
      const i = Number(btn.getAttribute("data-step"));
      if (i <= currentStep) goStep(i);
    });
  });
}

function setMode(next) {
  mode = next;
  document.getElementById("modeUrl").classList.toggle("on", mode === "url");
  document.getElementById("modeExe").classList.toggle("on", mode === "exe");
  document.getElementById("urlBlock").hidden = mode !== "url";
  document.getElementById("exeBlock").hidden = mode !== "exe";
  syncStep2Copy();
}

function syncStep2Copy() {
  const title = document.getElementById("step2Title");
  const lead = document.getElementById("step2Lead");
  if (!title || !lead) return;
  if (mode === "url") {
    title.textContent = t("step2UrlTitle");
    lead.textContent = t("step2UrlLead");
  } else {
    title.textContent = t("step2ExeTitle");
    lead.textContent = t("step2ExeLead");
  }
}

function composedUri() {
  const prefix = document.getElementById("urlPrefix").value.trim() || "steam://launch/";
  const rest = document.getElementById("urlRest").value.trim();
  if (!rest) return "";
  if (rest.includes("://")) return rest;
  return prefix + rest.replace(/^\/+/, "");
}

function refreshUrlPreview() {
  const el = document.getElementById("urlPreview");
  if (!el) return;
  const uri = composedUri();
  el.textContent = uri ? t("previewPrefix") + uri : "";
}

function currentTarget() {
  if (mode === "url") return composedUri();
  return document.getElementById("exeName").value.trim();
}

async function generate() {
  const hint = document.getElementById("genHint");
  const target = currentTarget();
  if (!target) {
    hint.classList.add("err");
    hint.textContent = t("needTarget");
    return;
  }
  hint.classList.remove("err");
  hint.textContent = "";
  document.getElementById("loading").hidden = false;
  try {
    const res = await fetch("/api/launcher/generate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        mode,
        target: mode === "url" ? document.getElementById("urlRest").value.trim() || composedUri() : target,
        url_prefix: document.getElementById("urlPrefix").value.trim(),
        dest_root: document.getElementById("dest").value.trim(),
      }),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      hint.classList.add("err");
      hint.textContent = data.detail || res.statusText;
      return;
    }
    lastJob = data;
    document.getElementById("resultName").textContent = data.filename;
    document.getElementById("resultPath").textContent = data.path;
    document.getElementById("resultBytes").textContent = Math.round(data.bytes / 1024) + " KB";
    document.getElementById("resultCompiler").textContent = data.compiler;
    document.getElementById("resultMode").textContent = data.mode + " · " + data.target;
    document.getElementById("downloadBtn").href = "/api/launcher/download/" + data.job_id;
    hint.textContent = t("genOk");
    goStep(2);
  } catch (err) {
    hint.classList.add("err");
    hint.textContent = String(err);
  } finally {
    document.getElementById("loading").hidden = true;
  }
}

async function boot() {
  uiLang = detectUiLang();
  document.getElementById("uiLangSwitch").addEventListener("click", (ev) => {
    const btn = ev.target.closest("[data-ui-lang]");
    if (!btn) return;
    uiLang = btn.getAttribute("data-ui-lang");
    localStorage.setItem("aio.uiLang", uiLang);
    applyI18n();
  });
  document.getElementById("modeUrl").addEventListener("click", () => setMode("url"));
  document.getElementById("modeExe").addEventListener("click", () => setMode("exe"));
  document.getElementById("next1").addEventListener("click", () => {
    setMode(mode);
    goStep(1);
  });
  document.getElementById("back1").addEventListener("click", () => goStep(0));
  document.getElementById("generate").addEventListener("click", generate);
  document.getElementById("again").addEventListener("click", () => goStep(0));
  document.getElementById("urlPrefix").addEventListener("input", refreshUrlPreview);
  document.getElementById("urlRest").addEventListener("input", refreshUrlPreview);
  document.getElementById("openFolder").addEventListener("click", async () => {
    if (!lastJob) return;
    await fetch("/api/folder/open", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ path: lastJob.path }),
    });
  });
  try {
    const res = await fetch("/api/defaults");
    const data = await res.json();
    if (data.version) document.getElementById("appVersion").textContent = "v" + data.version;
    if (data.dest) document.getElementById("dest").value = data.dest;
    if (data.url_prefix) document.getElementById("urlPrefix").value = data.url_prefix;
  } catch (_) {
    /* keep markup defaults */
  }
  setMode("url");
  applyI18n();
}

boot();
