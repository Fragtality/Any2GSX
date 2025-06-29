# Any2GSX
<img src="img/icon.png" width="128"><br/>
Generalized Version of [Fenix2GSX](https://github.com/Fragtality/Fenix2GSX) bringing GSX Automation and App Volume Control to all Aircrafts! <br/>

- **GSX Automation** (ie. calling Services, skipping Questions) can be enabled for all Aircrafts
- **App Volume Control** available to all Aircrafts (if there is a Channel Definition available)
- **Aircraft Plugin System** to enable Fuel-, Payload- and Equipment Sync for specific Aircrafts
- **SmartButton** Control for every Aircraft to call the next Service / trigger the next Call (the INT/RAD Thingy known from Fenix2GSX)
- **EFB App** for MSFS2024 to check on the App Status, SmartButton Trigger and GSX Menu
- **PilotsDeck** Integration bringing the GSX Menu to your StreamDeck (replacing the GSX Script known from my PilotsDeck Profiles)

<br/><br/>

## 1 - Introduction

### 1.1 - Requirements

- Windows 10/11
- MSFS 2020/2024
- A properly working and updated GSX Installation (not needed when only Volume Control is used)
- Capability to actually read the Readme up until and beyond this Point :stuck_out_tongue_winking_eye:
- The Installer will install the following Software automatically:
  - .NET 8 Desktop Runtime (x64) - Reboot your System if it was installed for the first Time
  - Any2GSX' CommBus WASM Module

<br/>

[Download Any2GSX-Installer-latest.exe](https://github.com/Fragtality/Any2GSX/raw/refs/heads/master/Any2GSX-Installer-latest.exe)

(Currently only Development Builds available)
<br/>

### 1.2 - Installation, Update & Removal

Just [Download](https://github.com/Fragtality/Any2GSX/raw/refs/heads/master/Any2GSX-Installer-latest.exe) & Run the **Installer** Binary! It will check and install all Requirements the App (or remove it). Your existing Configuration persists through Updates.<br/>
On the second Installer Page you can select if Auto-Start should be set up for Any2GSX (recommended for Ease of Use).<br/>
You do **not need to remove** the old Version for an Update (unless instructed) - using 'Remove' in the Installer completely removes Any2GSX (including WASM Module and Auto-Start). This also removes your Configuration including Aircraft Profiles and saved Fuel!<br/><br/>

It is highly likely that you need to **Unblock/Exclude** the Installer & App from BitDefender and other AV-/Security-Software.<br/>
**DO NOT** run the Installer or App "as Admin" - it might work, it might fail.<br/><br/>

<br/>

Any2GSX will display a **orange Circlr** on its SysTray/Notification Area Icon if a **new Version** (both Stable and Development) is available. There is no Version Pop-Up and there will never be.
<br/><br/>

### 1.3 - Auto-Start

When starting it manually, please do so when MSFS is loading or in the **Main Menu**.<br/>
To automatically start it with **FSUIPC or MSFS**, select the respective Option in the **Installer**. Just re-run it if you want to change if and how Any2GSX is auto started. Selecting one Option (i.e. MSFS) will also check and remove Any2GSX from all other Options (i.e. FSUIPC), so just set & forget.<br/>
For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup (not deemed neccessary), so if you want a Backup, do so yourself.<br/><br/>

#### 1.3.1 - Addon Linker

If you use Addon Linker to start your Addons/Tools, you can also add it there:<br/>
**Program to launch** C:\Users\YOURUSERNAME\AppData\Roaming\Any2GSX\bin\Any2GSX.exe<br/>
**Wait for simconnect** checked<br/>
The Rest can be left at Default.<br/>

<br/><br/><br/>

## 2 - Configuration

### 2.2 - GSX Pro

- It is recommended (but not required) to enter your **SimBrief Username** and have **Ignore Time** checked to have correct Information on the VDGS Displays.
- For **Automated staircases** semi-automatic (half-checked) is recommended - but it should work with all Modes.
- It is **not recommended** to use the **Always ask for pushback** Option - use Any2GSX to Answer the Question with Yes, No (default) or answer it manually
- The De-/Boarding Speed of Passengers is dependant on the Passenger Density Setting (GSX In-Game Menu -> GSX Settings -> Timings). Higher Density => faster De/Boarding (But "Extreme" can be to extreme in some Cases).
- Ensure the other two Settings under Timings are on their Default (15s, 1x).
- As with GSX itself, Any2GSX runs best when you have a proper Airport Profile installed!
- Up to everyone's *Preference*, but disabling the **Aural Cues** (GSX In-Game Menu -> GSX Settings -> Audio) and setting **Message verbosity** to "*only Important*" (GSX In-Game Menu -> GSX Settings -> Simulation) can improve Immersion! 😉

<br/><br/>

### 2.3 - Any2GSX

The Configuration is done through the **GUI**, open it by **clicking on the System-Tray/Notification-Icon**. All Options have **ToolTips** explaining them further.<br/>
Everything is stored persistently in the *AppConfig.json* File in the Application's Folder - so backup that File in order to backup your Settings!<br/>

<br/><br/>
<img src="img/ui1.png"><br/><br/>
