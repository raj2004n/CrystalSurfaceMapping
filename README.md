# Crystal Surface Mapping System

**Automated system orchestration for Raman Spectroscopy mapping using heterogeneous computing environments (Raspberry Pi 5, Windows 10, and Windows 7 Legacy).**

This project automates the laborious crystal surface mapping procedure for the Center of Science at Extreme Condition at the University of Edinburgh, synchronizing control over physical rotors and proprietary spectrometer software.

## ⚠️ Licensing and Prerequisites

**NOTE:** The core functionality relies on the proprietary **WinX32 application** and its accompanying library, **`WinX32Lib`**.

Due to licensing constraints, the `WinX32Lib` cannot be distributed publicly. Therefore, this project **cannot be run** without legitimate ownership and installation of the WinX32 application and its associated development library.

---

## Target Environment and Components

The system operates across three physically distinct, heterogenous computing platforms connected via a LAN:

| Component | Platform | Role | Technology Stack |
| :--- | :--- | :--- | :--- |
| **WinRotor Client** (The Orchestrator) | Raspberry Pi 5 (Linux) | Initiates mapping, coordinates servers, provides user interface. | C\# / .NET (MVVM Architecture) |
| **Kohzu Server** | Windows 10 PC | Direct control over the Kohzu Rotational Stages via serial port. | C\# / .NET (gRPC Server) |
| **WinSpec Server** | Windows 7 (Legacy) PC | Interfaces with the proprietary WinX32 Spectrometer Application. | VB.NET (Custom RPC Server) |

---

## Project Goal and Architectural Solution

### The Problem

Manual crystal surface mapping requires repetitive, time-consuming steps:

1.  Use the rotors to position the sample.
2.  Use the spectrometer to collect a data reading.
3.  Shift the sample to the next location.
4.  Repeat until a complete map is collected.

### The Solution: Server-Client Architecture

The final architecture utilizes a client-server model to bridge the hardware and software silos:

- **`Kohzu Server`:** Runs on Windows 10. Handles low-level serial communication to the rotors hardware.
- **`WinSpec Server`:** Runs on Windows 7. Acts as a wrapper, interfacing directly with the proprietary `WinX32Lib` to control the spectrometer's acquisition sequence.
- **`WinRotor Client`:** Runs on the Raspberry Pi 5. Acts as the central **Orchestrator**, issuing high-level commands to both servers to execute the mapping sequence.

### Communication Protocols

| Server | Protocol | Rationale |
| :--- | :--- | :--- |
| **Kohzu Server** | **gRPC** | Chosen for its modern efficiency, low latency, and ease of implementation with .NET Core. |
| **WinSpec Server** | **Custom RPC (TCP Sockets)** | Necessary due to the incompatibility and lack of official gRPC support on the **Windows 7 (Legacy)** platform running the WinX32 application. |

### Client Design Pattern

The `WinRotor Client` uses the **MVVM (Model-View-ViewModel)** architectural pattern to ensure a clean separation of concerns, maximizing **maintainability, testability, and responsiveness** for future updates.

---

## Challenges

The project faced problems mainly due to legacy systems and strict operating constraints:

- **Outdated Proprietary Manuals:** The **WinX32 Manual** was severely outdated. This led to significant trial-and-error, as methods documented in the manual were often missing from the provided `WinX32Lib`, and vice-versa.
- **Inherent Rotational Stage Errors:** An unpredictable hardware error caused one rotational axis to **fail to settle correctly** for small, absolute position displacements, requiring workarounds in the control logic.
- **Network Isolation:** Both PC hosts lacked an internet connection. All necessary development tools, dependencies, and certificates had to be manually transferred via a USB stick.
- **Steep Learning Curve:** The developer had no prior experience in **C\#**, **server-client communication (gRPC/RPC)**, **MVVM**, or **serial port interfacing**, requiring rapid skill acquisition.

## Future Work

1.  **Implement Step & Glue Acquisition:** Integrate the "Step & Glue" feature to combine multiple spectrometer fields into a single image. This is currently blocked by the aforementioned version mismatch, but remains feasible through continued API function testing.
2.  **Resolve Keyboard Focus Issue (Raspberry Pi):** Investigate and fix a platform-specific issue where the UI keyboard focus is lost after a non-UI button click. The keyboard requires an external terminal input to "wake up" the focus again.
