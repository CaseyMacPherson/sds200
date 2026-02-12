# SDS200-CLI: Cross-Platform Scanner Controller

A high-performance, terminal-based user interface (TUI) for the **Uniden SDS200 Digital Radio Scanner**. This application allows you to control and monitor your scanner over a local network (UDP) or direct USB (Serial) connection using a modern, reactive C# implementation.

---

## 🚀 Features
* **Dual-Stack Communication:** Effortlessly switch between USB Serial and Ethernet UDP modes at startup.
* **Live ASCII Dashboard:** High-contrast display featuring frequency, modulation, alpha tags, and signal strength.
* **Cross-Platform Support:** Built on .NET 8, tested on Windows 11, Ubuntu 22.04, and macOS.
* **Hardware Emulation:** Physical radio controls mapped to intuitive keyboard shortcuts.
* **Input Validation:** Real-time frequency validation to prevent radio "No Good" (NG) errors.

---

## 🛠 Prerequisites

1.  **.NET 8.0 SDK** or later.
2.  **Uniden SDS200** hardware.
3.  **Permissions (Linux Users):** Ensure your user has access to the serial port.
    ```bash
    sudo usermod -a -G dialout $USER
    ```
    *(Note: You may need to log out and back in for this to take effect.)*

---

## 📡 Scanner Configuration

Ensure your hardware is set up to communicate with the application:

### Option A: Serial (USB)
1.  Connect the SDS200 "USB" port to your computer.
2.  On the radio: `Menu` -> `Settings` -> `Set Serial Port` -> `Set Baud Rate`.
3.  Select **115200 bps**.
4.  When prompted on the radio screen, ensure it is in **Serial Mode** (do not select Mass Storage).

### Option B: UDP (Network)
1.  Connect the SDS200 Ethernet port to your local network.
2.  On the radio: `Menu` -> `Settings` -> `Wired Lan Settings`.
3.  Note the **IP Address**.
4.  Default Communication Port: **50536**.

---

## ⌨️ Controls

The interface uses the following hotkeys for rapid control:

| Key | Action | Protocol Command |
| :--- | :--- | :--- |
| **`S`** | **Scan** | `KEY,S,P` |
| **`H`** | **Hold** | `KEY,H,P` |
| **`F`** | **Tune Freq** | `FRE,[Input]` |
| **`A`** | **Avoid** | `KEY,A,P` |
| **`Up/Down`** | **Scroll** | `KEY,U,P` / `KEY,D,P` |
| **`+` / `-`** | **Volume** | `VOL,[v]` |
| **`ESC`** | **Exit** | Disconnect & Close |

---

## 🏗 Installation & Build

1.  **Clone the project:**
    ```bash
    git clone [https://github.com/yourusername/sds200-cli.git](https://github.com/yourusername/sds200-cli.git)
    cd sds200-cli
    ```

2.  **Restore NuGet Packages:**
    ```bash
    dotnet restore
    ```

3.  **Build and Run:**
    ```bash
    dotnet run
    ```

---

## ⚠️ Troubleshooting

* **Serial Port Not Found:** Verify the USB cable is a "Data" cable and not just a "Power" cable. Check Device Manager (Windows) or `ls /dev/tty*` (Linux).
* **UDP Lag:** If the signal meter is stuttering, check for network congestion or firewall rules blocking UDP traffic on port 50536.
* **Spectre Rendering:** If the UI looks "broken," ensure your terminal supports ANSI escape sequences (Windows Terminal, iTerm2, and VS Code Terminal are recommended).

---

## 🧬 Architecture

The project follows an interface-driven approach:
* `IScannerBridge`: Abstraction for communication protocols.
* `UnidenParser`: Stateless logic to handle raw SDS200 protocol strings.
* `Spectre.Console`: Handles the "Live Display" rendering loop.

---
*Created with the collaboration of a Systems Architect, RF Engineer, QA Lead, and UX Designer.*