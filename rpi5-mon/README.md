# 🍓 rpi5-mon

[![Platform](https://img.shields.io/badge/Platform-Raspberry%20Pi%205-C51A4A?logo=raspberry-pi)](https://www.raspberrypi.com/products/raspberry-pi-5/)
[![SoC](https://img.shields.io/badge/SoC-BCM2712%20(ARM64)-blue)](https://www.broadcom.com/)
[![Shell](https://img.shields.io/badge/Language-Bash-green.svg)](https://www.gnu.org/software/bash/)
[![Termux](https://img.shields.io/badge/Optimized%20for-Termux%20Mobile-16A085.svg)](https://termux.dev/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A lightweight, zero-dependency, ultra-clean hardware & telemetry dashboard built specifically for the **Raspberry Pi 5** (BCM2712). 

Engineered with a strict **37-column terminal width**, `rpi5-mon` delivers full-fidelity system monitoring without horizontal wrapping on narrow mobile screens (such as **Termux** in portrait mode) while looking sharp and modern on any desktop SSH terminal.

---

## 📸 Preview

```text
 Raspberry Pi 5 (BCM2712)
 15:04:18 · Up: 34m   · Load: 0.56
─────────────────────────────────────
 CPU   4% [░░░░░░░░░░] 50.7°C  2.80GHz
 GPU   0% [░░░░░░░░░░] 50.7°C   960MHz
 VPU   0% [░░░░░░░░░░]  910MHz  hevc
 FAN  29% [██░░░░░░░░] 2624 RPM  PWM
 NVM  36% [███░░░░░░░] 32.8°C  143G free
 RAM   9% [░░░░░░░░░░] 0.7/7.7G 7171M free
 SWP   0% [░░░░░░░░░░] 0.0/0.4G
─────────────────────────────────────
 [Ctrl+C] Exit  ·  Refresh: 1s
```

---

## ✨ Features

- **⚡ CPU Telemetry (ARM Cortex-A76):**
  - Instantaneous load percentage calculated via kernel `/proc/stat` deltas.
  - Core frequency scaling readout (including support for 2.80GHz+ overclocks).
  - CPU / SoC package temperature formatted with color thresholds.

- **🎮 GPU Telemetry (VideoCore VII / V3D):**
  - Hardware GPU utilization calculated from `/sys/devices/platform/axi/*.v3d/gpu_stats` (runtime/timestamp deltas).
  - Live V3D clock rate via `vcgencmd measure_clock v3d` (e.g. 960MHz).

- **🎬 VPU Telemetry (Hardware Video Codec):**
  - Monitors the dedicated 4Kp60 HEVC decode engine (`rpi-hevc-dec` / `1000800000.codec`).
  - Displays hardware codec clock (910MHz) when idle.
  - Dynamically displays real-time frame/slice decode throughput (`fps` / `irq/s`) and active load bar during video playback.

- **🌪️ Active Cooler Telemetry:**
  - Automatic detection of the official Raspberry Pi Active Cooler.
  - Reports PWM duty cycle percentage and real tachometer fan speed in **RPM** (`fan1_input`).

- **💾 NVMe / Storage Telemetry:**
  - Auto-detects PCIe M.2 NVMe SSDs and extracts composite drive temperature from `hwmon`.
  - Reports root filesystem storage usage percentage and available gigabytes.
  - Automatically falls back to standard `DSK` metrics if booting from MicroSD.

- **🧠 RAM & Swap (Dual Metrics):**
  - Single-pass `/proc/meminfo` parser.
  - Reports used/total gigabytes alongside exact free megabytes.

- **⚠️ Thermal & Voltage Throttling Alert:**
  - Continuously checks `vcgencmd get_throttled` for under-voltage flags, Arm frequency capping, or thermal throttling events.
  - Immediately displays an in-line warning indicator (`!THROTTLED!`) if an alarm state is tripped.

- **📱 Mobile-First Ergonomics:**
  - Strict 37-character column constraint. No awkward line wrapping on smartphone screens.
  - Uses ANSI 256/16-color styling, smooth block progress bars (`[████░░░░░░]`), and cursor hiding with clean exit traps.

- **🪶 Zero Dependencies:**
  - Pure POSIX/Bash. No Python packages, no node_modules, no compilation step.

---

## 🚀 Installation

### Option 1: Quick Install (User Binary)

```bash
# Download and install into ~/.local/bin
mkdir -p ~/.local/bin
curl -sSL https://raw.githubusercontent.com/<YOUR_USER>/rpi5-mon/main/rpi5-mon -o ~/.local/bin/rpi5-mon
chmod +x ~/.local/bin/rpi5-mon

# Ensure ~/.local/bin is in your PATH (usually default in Debian/Raspberry Pi OS)
export PATH="$HOME/.local/bin:$PATH"
```

### Option 2: System-Wide Install

```bash
sudo cp rpi5-mon /usr/local/bin/
sudo chmod +x /usr/local/bin/rpi5-mon
```

### Option 3: Clone from Git

```bash
git clone https://github.com/<YOUR_USER>/rpi5-mon.git ~/Binaries/rpi5-mon
cd ~/Binaries/rpi5-mon
chmod +x rpi5-mon
```

---

## 📖 Usage

### Live Interactive Dashboard
Refreshes every second (default):
```bash
rpi5-mon
```

### Custom Refresh Rate
Pass the refresh interval in seconds:
```bash
rpi5-mon 2
```

### Single Snapshot Mode
Print one snapshot and immediately exit (ideal for SSH login banners / MOTD or automated scripts):
```bash
rpi5-mon --once
# or
rpi5-mon -1
```

### Help
```bash
rpi5-mon --help
```

---

## ⚙️ Requirements

- **Device:** Raspberry Pi 5 Model B (4GB / 8GB / 16GB)
- **OS:** Raspberry Pi OS 64-bit (Debian 12 Bookworm) or compatible Linux kernel 6.x+
- **Utilities:** `bash`, `vcgencmd`, `awk`, `df`

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
