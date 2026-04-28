### App
- Major App Overhaul addressing various Issues and some QoL Improvements
- Added a Profile Selector in Automation & Volume Control View to quickly change Profiles (when the Sim Session is not running)
- Aircraft Plugins can now be reloaded/changed while a Session Runs without doing an App Restart
  (for Development & Testing Purposes and non-normal Operation!)
- Automation
  - Settings are hidden and Notes change depending on the Plugin Capabilities
	(i.e. no Refuel Rate when the Aircraft/Plugin doesn't support Fuel-Sync)
  - Added Door Options to keep Cargo Doors Open (after Board / Deboard) 
  - Added Options to enable/disable Control over Pax Doors, Service Doors and Aircraft Panels (Refuel, Water, Waste)
  - Added Option to connect Stairs (and do Reposition) on Session Start/Walkaround (when Walkaround is not skipped)
  - Improved Workaround to connect Stairs & Refuel (on the same Side)
  - Display a Notice when the Service Queue is currently in Use
  - More compact Layout for the Service Queue 
  - New Option to select *only* preferred Operators automatically (else manual Selection if none found)
  - Added Support for 'Cockpit Notifications' on ready for Departure, Final received, Chocks placed and ready for Turnaround
  - Final Delay moved to new Category 'LS & Notifications'
  - Added missing Options for SmartButton Requests in the 'Change Parking' Menu (formerly "ClearGate")
  - Added Options for SmartButton Requests in the 'interrupt Pushback' Menu (Pause/Stop/Abort)
  - Added 'Variable Checker' Dialog in the Plugin Options to see the generic Variables and test custom ones
  - Added 'Reload Aircraft' Button in the Plugin Options to reload the current Plugin (for Dev/Testing!)
  - Essential Plugin Options on the generic Plugin are now highlighted (and changed Notes to emphasis their Importance)
  - Plugin Options have shorter Labels, Units (if applicable) and Tooltips now
- Volume Control
  - Added 'Swap Channels' Button to change the App Mappings (i.e. swap "CPT" with "FO")
  - Startup Settings (Volume, Unmute) are also swapped
  - The Swap is purely text-based - i.e. it can also be used to swap individual Channels (VHF1 <> VHF2)
- Aircraft Profiles View
  - Reworked the Layout so that adding or loading a Profile should be less confusing
  - The Profile matched by the App on Session Start is now preselected in the List
  - Switching Profiles while in Session is now guarded by a Message Box asking for Confirmation
- Plugins View
  - The Dialog shown when Installing a Plugin now needs Confirmation before it can be closed - RTFM :joy:
  - Changed Version-Check for new/updates Plugins to have a 5 Minute Cooldown after the last Check (i.e. after Startup)
  - Opening the Plugins View will trigger the Check (if the Cooldown has passed)
- App Settings View
  - Reworked to Layout to separate Settings in different Categories (like Automation View)
  - Added Settings for Refuel/Water/Lavatory Panel Delays
  - The Speed Treshold for Taxi-In can now be configured in the GUI
  - The Operator Select Timeout can now configured in the GUI
  - The Saved Fuel can now be edited (and the List refreshes while a Session is active)
- Rewrite of the Departure Queue System
  - Editing the Queue during a Sim Session is now fully supported (yet still some Changes like Order won't apply until the next Departure Phase)
  - New Constraint 'Preferred Operator' - calling the Service only if the Handling (!) Operator was on the Preferred Operator List
  - New Service Option to delay the Call x Seconds after the last Service Call
- Rewrite of Ground Equipment Handling
  - Equipment is now mostly based on Time defined by the Equipment Delay (formerly Chock Delay)
  - Improved Handling of Equipment Dependencies (i.e. GPU needs to be removed before Chocks)
  - New Option to remove Chocks when the Tug attaches during Boarding
- Rewrite of the Menu Handling System
  - Improved Failure Handling and Selection Accuracy
  - Support for the Select Refuel Level Dialog
	(needs to be enabled in the Plugin Options, only selects SimBrief or Custom Fuel, still not recommended to be used on Aircrafts having Fuel-Sync)
  - The App now sets the Menu to 'disabled' to minimize Situations where it briefly flashes up
  - The GSX Toolbar (and Menu) can now be automatically enabled by the App (and the Toolbar State is detected)
  - An already enabled Toolbar/Menu is not disabled (unless configured otherwise in the App Settings)
  - New Option to enable the Toolbar/Menu when Selections are needed
	(eliminating the Need to manually enable the Toolbar for Pushback, Gate-Selection, etc.)
  - Note that the Menu will *not* be enabled when PilotsDeck Integration is enabled for the Profile
  - Automatic Refresh after Timeout/Selection can now be enabled/disabled individually for EFB App und PilotsDeck Integration
  - Automatic Refresh Delay now configurable (default 5s)
- Fixes/Improvements for Handling Turnarounds with Departure Services started during Deboard
- Fixes/Improvements for OFP Import on Arrival (to trigger Departure Services)
  - New Simbrief OFP can be published as soon as the App switched to Taxi-In Phase (after Touchdown)
  - Triggering Ready for Departure can happen as soon as Deboarding is requested (respectively the App switched to Arrival Phase)
  - The Initial Turn Delay still applies - Checks for a new OFP Id still only happen if expired
- The Boarding Completed Check to skip to the Pushback Phase now uses Zero Fuel Weight (Total - FOB) instead of Total Weight
- Door Messages from GSX are automatically disabled for Aircrafts/Plugins with Door-Sync
- Cargo Changes are now applied over Time to smooth out big Changes (5% per Second, for Aircrafts that support Payload-Sync)
- Aircraft Plugins can now sync their own OFP Values to the App (i.e. Planned Fuel or Pax) and the App notifies on Import/Unload
- Added Support for Defuel Operations (GSX 3.9.4)
  - Works for both fixed and dynamic Rate
  - Note that the Skip Refuel/Tankering Setting has to be disabled (else the Service will not be called)
  - The Decrease FOB Setting has been removed (as it was only a Workaround for Defuel)
- Added Support for new Vehicle L-Vars for Stairs only (GSX 3.9.4)
- Added Support for new Reposition L-Var/Notification (GSX 3.8.3)
- Default Value for the Operator Selection increased to 25s (applied to existing Profiles)
- Default Value for the Refuel Disconnect Timeout increased to 30s (applied to existing Profiles)
- Update-Checks and -Download through CDN
  - The App now uses jsdelivr as CDN to minimize Problems with GitHub Requests timing out
  - The CDN Cache is "busted" as appropiate to prevent delayed Update Notices
  - Plugins are downloaded (and cached) through CDN, the App/Installer still needs Download from GitHub (too big)
- Improvements & Fixes to Session Start & Stop Handling solving secondary Issues after App/Session Restart
- The Position of the App Window is now saved and restored on startup

<br/>

### Components
- Changes to the WASM Module/Community Package for the Features - Version bumped to 0.4
- Rewrite of the Notification System for EFB App / PilotsDeck Integration
  - More & detailed Status Messages of App Operations and GSX Events (OFP Detection, GSX Restart/Refresh, Menu Calls, Service State changes, etc)
  - EFB App has an additional Field for the Aircraft/Plugin Connection (and splits Phase and Status Messages in two Lines)
- Some Fixes/Improvements on the GSX Profile - Version bumped to 1.0

<br/>

### Installer
- Option to start the Application after Installation (automatically selected when the Sim is running)
- Option to install/update the GSX Profile for PilotsDeck (through the Plugin's Profile Manager)
- Set .NET 10.0.7 as Target

<br/>

### Plugins
- The Plugin Repository now has its own [Changelog](https://github.com/Fragtality/Any2GSX-Plugins/blob/master/CHANGELOG.md) listing the Updates/Changes to the Plugins
- All existing Plugins (B777, A350, A340, A330, A300): adapted to new Features and Changes in the Plugin Interface
- New Plugin: Fenix A320 - which will be the Replacement to Fenix2GSX (see 'Migrating from Fenix2GSX' in the Readme)

<br/>

### Release Management
- There won't be a Distinction between a Release/Stable and Beta/Dev Version as with Fenix2GSX
  - There will be a (Github) Release called "Latest Build" always being updated when Changes are pushed
  - The Any2GSX-Installer-latest.exe in the Project Files can still be used - it is the same Thing
  - Recent Changes are now tracked in CHANGES.md (which will also be added the Description of the latest Release)
  - When the Version increases, the Changes will be added to the CHANGELOG.md
  - The Installer will be scanned by VirusTotal every Time an Update is pushed - in Case you need a second Opinion on your Scanner saying its Malware :wink:
