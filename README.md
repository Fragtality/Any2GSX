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
<br/><br/>

### 1.2 - Installation, Update & Removal

Just Download & Run the **Installer** Binary! It will check and install Requirements like the .NET Runtime or WASM Module.<br/>
Any2GSX will display a **orange Circle** on its SysTray/Notification Area Icon if a **new Version** (both Stable and Development) is available. Your existing Configuration persists through Updates (stored persistently in the *AppConfig.json* File in the Application's Folder).<br/><br/>
On the second Installer Page you can select if Auto-Start should be set up for Any2GSX (recommended for Ease of Use). While it is possible to install the WASM Module to only one specific Sim Version/Variant, it is recommended to just install it to all. If you choose to install the WASM Module to only one Simulator, make sure you select *Update only existing Installations* when updating the App (else the Module will be installed on all Simulators)! The Force Module Update Option is mostly for Troubleshooting to force an Update of the WASM Module.<br/><br/>
You do **not need to remove** the old Version for an Update (unless instructed) - using 'Remove' in the Installer completely removes Any2GSX (including WASM Module and Auto-Start). This also removes your Configuration including Aircraft Profiles and saved Fuel!<br/><br/>

It is highly likely that you need to **Unblock/Exclude** the Installer & App from BitDefender and other AV-/Security-Software.<br/>
The App will be installed to (cannot be changed): `%appdata%\Any2GSX` (`C:\Users\YOURUSERNAME\AppData\Roaming\Any2GSX`)<br/>
**DO NOT** run the Installer or App "as Admin" - it might work, it might fail.<br/><br/>

### 1.3 - Auto-Start

When starting it manually, please do so when MSFS is loading or in the **Main Menu**.<br/>
To automatically start it with **FSUIPC or MSFS**, select the respective Option in the **Installer**. Just re-run it if you want to change if and how Any2GSX is auto started. Selecting one Option (i.e. MSFS) will also check and remove Any2GSX from all other Options (i.e. FSUIPC), so just set & forget.<br/>
For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup (not deemed neccessary), so if you want a Backup, do so yourself.<br/><br/>

#### 1.3.1 - Addon Linker

If you use Addon Linker to start your Addons/Tools, you can also add it there:<br/>
**Program to launch** C:\Users\YOURUSERNAME\AppData\Roaming\Any2GSX\bin\Any2GSX.exe<br/>
**Wait for simconnect** checked<br/>
The Rest can be left at Default.<br/>

### 1.4 - Core Concepts/Components



<br/><br/><br/>

## 2 - Configuration

### 2.2 - GSX Pro

- It is recommended (but not required) to enter your **SimBrief Username** and have **Ignore Time** checked to have correct Information on the VDGS Displays.
- For **Automated staircases** semi-automatic (half-checked) is recommended - but it should work with all Modes.
- It is **not recommended** to use the **Always ask for pushback** Option - use Any2GSX to Answer the Question with Yes, No (default) or answer it manually
- The De-/Boarding Speed of Passengers is dependent on the Passenger Density Setting (GSX In-Game Menu -> GSX Settings -> Timings). Higher Density => faster De/Boarding (But "Extreme" can be to extreme in some Cases).
- Ensure the other two Settings under Timings are on their Default (15s, 1x).
- As with GSX itself, Any2GSX runs best when you have a proper Airport Profile installed!
- Up to everyone's *Preference*, but disabling the **Aural Cues** (GSX In-Game Menu -> GSX Settings -> Audio) and setting **Message verbosity** to "*only Important*" (GSX In-Game Menu -> GSX Settings -> Simulation) can improve Immersion! 😉

<br/><br/>

### 2.3 - Any2GSX

The Configuration is done through the **GUI**, open it by **clicking on the System-Tray/Notification-Icon**. All Settings have **Tooltips** explaining them further. It is recommended to familiarize yourself with the Settings and the general Usage (see [Section 3](#3---usage)) first before starting the first 'serious' Flight with the App!<br/><br/>

The first Time you start the App (or the Config was Reset) it will automatically open the GUI and the '**App Settings**' View - please enter your **SimBrief User** (Name and ID both accepted) for the App to work properly!<br/>
<img src="img/ui-first.png" width="520"><br/><br/>

After this intial Step you might want to check out the 'Plugins' View to check-out which Aircraft Plugins, Channel Definitions or Aircraft Profiles are available to download (see [Section 2.3.4](#234---plugins-view) for Details).<br/><br/>

Since Any2GSX is active for all Aircrafts, I'd recommend to check out the '**Aircraft Profiles**' View next (No, nothing to do with GSX Aircraft Profiles). They are an essential Part of the App, basically the Glue bringing everything together: they determine what Any2GSX Features (GSX Automation, Volume Control, PilotsDeck Integration) should be active for a specific Aircraft and which Aircraft Plugin and/or Audio Channel should be loaded for that. All Automation Settings found in the 'Automation' View are stored per Profile - so together with the Ability to filter on specific IDs, Airlines or Titles/Liveries you can have different Settings for different Airlines having different SOPs to follow (or just having different Operator Preferences for different Airlines). Check [Section 2.3.3](#233---aircraft-profiles-view) for Details.<br/><br/>

#### 2.3.1 - Automation View

**Gate & Doors**
Configure when Doors, Jetway and Stairs are operated - for Example if Jetway/Stairs should be connected on Session Start and when they should be removed.<br/>
The Jetway & Stair Options apply to all Aircrafts, but the Door-Handling Options only apply to Aircraft-Plugins having their own Door-Sync-Code and have implemented these Settings!<br/>
<br/>

**Ground Equipment**
Mostly the Min/Max Values for the Chock and Final LS Delays. The Chock Delay and remove Equipment on Beacon Options only apply to Aircraft Plugins implementing Chock-/Ground-Equip Handling!<br/>
The Final LS Delay applies to all Aircrafts. It's primarily a Timer which can trigger other Events when expired (like removing the Jetway, or starting Pushback with an attached Tug).<br/>
<br/>

<br/><br/>

#### 2.3.2 - Volume Control View

<br/><br/>

#### 2.3.3 - Aircraft Profiles View

<br/><br/>

#### 2.3.4 - Plugins View

<br/><br/>

#### 2.3.5 - App Settings View

<br/><br/><br/>

### 3 - Usage

#### 3.1 - General Service Flow / SOP

#### 3.1.1 - Session Start

<br/><br/>

#### 3.1.2 - Preparation Phase

<br/><br/>

#### 3.1.3 - Departure Phase

<br/><br/>

#### 3.1.4 - Pushback Phase

<br/><br/>

#### 3.1.5 - Taxi-Out Phase

<br/><br/>

#### 3.1.6 - Flight Phase

<br/><br/>

#### 3.1.7 - Taxi-In Phase

<br/><br/>

#### 3.1.8 - Arrival Phase

<br/><br/>

#### 3.1.9 - Turn-Around Phase

<br/><br/>

#### 3.2 - SmartButton Calls

<br/><br/><br/>

## 4 - Addon NOTAMs

<br/><br/><br/>

## 5 - NOTAMs (Usage Tips)

<br/><br/><br/>

## 6 - FCOM (Troubleshooting)

<br/><br/><br/>
