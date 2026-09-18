<div align="center">

# 开普勒内存启动器工坊

**Packed via Axiox Media**

两步桌面工具：用启动链接或游戏程序全名，生成带内存优化与优先级保护的静默启动 EXE。

<p>
  <a href="../README.md"><img src="https://img.shields.io/badge/English-README-e7c07a?style=for-the-badge" alt="English README" /></a>
</p>

<p>
  <a href="#install">安装</a> ·
  <a href="#features">功能</a> ·
  <a href="#requirements">环境</a> ·
  <a href="#architecture">结构</a> ·
  <a href="#documentation">问答</a>
</p>

<p>
  <img src="https://img.shields.io/badge/platform-Windows_10%2F11-0b0d12?style=flat-square" alt="Windows" />
  <img src="https://img.shields.io/badge/python-3.11%2B-e7c07a?style=flat-square" alt="Python" />
  <img src="https://img.shields.io/badge/ui-zh%20%2F%20en-7ee0c6?style=flat-square" alt="i18n" />
  <img src="https://img.shields.io/badge/output-native_launcher_exe-c9a227?style=flat-square" alt="output" />
</p>

</div>

<div align="center">
  <img src="APPCap.png" alt="开普勒内存启动器工坊预览" width="100%" />
</div>

> [!NOTE]
> 工坊本体 EXE 未签名，首次运行可能被 SmartScreen 拦截。生成启动器需要 Windows 自带的 .NET Framework `csc.exe`。

---

## 一览

| 项 | 值 |
|---|---|
| 界面 | 黑金步骤向导，中 / 英 |
| 第一步 | 选协议链接或程序全名 |
| 第二步 | 填写目标并生成 EXE |
| 生成物 | 无窗口、提权、单实例、每分钟驻留优化 |

<a id="install"></a>

## 安装

### 1. GitHub Deploy Desk（推荐）

用 [GitHub Deploy Desk](https://github.com/axioxmedia/github-deployer) 一键部署本仓库。

1. 获取部署台：https://github.com/axioxmedia/github-deployer
2. 把本仓库地址贴进部署台。
3. 在应用内阅读说明后确认部署。

这是支持的安装路径。只有在本地已有源码时，才使用下面的源码 / EXE 步骤。

### 2. 源码运行或冻结 EXE

| 路径 | 命令 |
|---|---|
| 冻结工坊 | 运行 `build_exe.bat` 后得到 `dist\MemOptLauncherDesk.exe` |
| 源码 | `start.bat` 或 `python app.py` |

<a id="features"></a>

## 功能

| 功能 | 说明 |
|---|---|
| 链接模式 | 前缀不锁定 Steam，默认预填 `steam://launch/` |
| 程序全名 | 只要文件名，不要路径；先搜启动器目录树，再扫固定盘根 |
| 内存通道 | 清空工作集、刷新修改页、清除备用列表 |
| 保护名单 | 游戏 + 启动器 + Steam 组件 + 系统核心进程 |
| 运行权重 | 对被启动进程设 `HIGH_PRIORITY_CLASS` |
| 驻留循环 | 游戏在线时每 60 秒一次；退出后等待 30 秒防重启 |

<a id="requirements"></a>

## 环境

| | 最低 | 建议 |
|---|---|---|
| 系统 | Windows 10 | Windows 11 |
| Python（源码 / 冻结） | 3.11 | 3.12 |
| 生成启动器的编译器 | .NET Framework 4.x `csc.exe` | 同左 |

<a id="architecture"></a>

## 结构

FastAPI 在 `127.0.0.1` 提供 `static/`。pywebview 承载向导。`POST /api/launcher/generate` 填入 `launcher_template.cs`，用 `csc` 编成无窗口启动器。

```
向导 → FastAPI → C# 模板 → csc.exe → *_MemOptLauncher.exe
```

<a id="documentation"></a>

## 问答

<details>
<summary>生成出来的 EXE 具体做什么？</summary>

申请管理员、抢单实例互斥量、先做完整内存优化，再打开协议或找到的 exe；之后每分钟裁剪其他进程工作集并拉高游戏优先级。游戏进程消失 30 秒后退出。纯链接模式若无法对应进程名，则保持驻留优化。

</details>

<details>
<summary>启动器必须和游戏放在同一目录吗？</summary>

不必。只提供程序全名。启动器从自己所在目录往下找，再扫固定磁盘根目录。

</details>

<details>
<summary>日志在哪？</summary>

工坊 EXE 旁边的 `memopt_launcher_desk.log`。

</details>

Packed via Axiox Media · [axiox.media](https://axiox.media)
