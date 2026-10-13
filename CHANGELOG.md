# Changelog


## v0.5.0

**BREAKING CHANGES**
<br/>
Backup the Application Directory if you want to go back!
<br/>
:exclamation: Requires GSX' Remote Control Server to be enabled: The Installer will modify the CouatlAddons.ini File accordingly
<br/>
:exclamation: All Service Queues will be reset to default: Change again to your Preference (new System)
<br/>
:exclamation: Plugin Interface Changes: Plugins require to be updated before they can work


### APP
- Added Support for GSX WebSocket API in the Backend for Menu Calls, Settings, HandlerData (GSX Aircraft/Airport Profile) and GSX State (Service and Vehicle State remain on L-Vars/SimConnect)
- Port Allocation now looks for free Ports between Min and Max (App Settings) - hopefully minimizing Issues with the App not starting until Reboot
- Fixed Audio Control unmuting an already unmuted Channel on Startup when a Calculator Code is defined for that Channel
- Changed Audio Control to only synch Volume when the Aircraft is powered (i.e. sudden powerloss does not mute everything)
- Fixed first Open of App Window to force a full reset/rescan of Audio Sessions
- Fixed Cargo Doors closing and opening during De-/Boarding (with Self-Loaders and multiple Trips enabled)
- Added Support for multiple Trips on the Fuel Truck - the Finish-Fuel-On-Hose-Disconnect Option is only applied when multiple Trips are not set in GSX
- Added a Notification/Info for EFB and PilotsDeck when waiting for the next Truck
- Added Workaround for SimBrief actual Pax/Bag Count reporting negative Values (resulting in negative Payload)
- Fixed negative Bag Weights with zero planned Bags after Randomization
- Added Workaround for GSX not detecting Fuel Increase when configured/calculated Rate has a high Value
- Fixed Issue with the Refuel Timer getting stopped too early when the Plugin does its own progressive Refuel (i.e. Fenix with Refuel Panel)
- Changed Door Open/Close Handling for Stairs and Catering: now based on Vehicle State only to trigger individual Doors (i.e. like with Loaders for Cargo Doors)
  - Generally the Door Logic was generalized in the App so that Plugins need less individual (but same) Code
  - This includes Handling of Standard Doors on certain Actions (dynamically detected through HandlerData) even on the generic Plugin
  - Because of that some Door Handling Options are now visible/configurable for Profiles using the generic Plugin
- Added Plugin Option to overwrite the preferred Door in the GSX Aircraft Profile (used for Jetways)
- Added Support to disable the 'Default Fuel system' GSX Aircraft Setting automatically for Plugins providing Fuel-Sync (looking at you, iniBuilds)
  - The Fuel Dialog will still be automatically answered for Aircrafts with that Setting enabled and having no Fuel-Sync
- Added Feature to manipulate the GSX Fuel Acceleration Setting to roughly match the configured Refuel Rate (only for Aircrafts with Fuel-Sync through Plugin)
- Fixed Calls to reopen the Pushback/Direction Menu not working and being too soon (opening the Menu when Selection was not yet available)
- Changes on some Menu Calls when to Enable the Toolbar or Hide the Menu 
- Added Detection for selected Gate in the In-Flight Menu, preventing the Open Menu on Taxi-In (if the Change parking or service Page was opened once)
- Improved Detection if and what Type of Operator was selected
- Changes to the Service State Handling (i.e. requested but aborted Services are counted as completed to advance in the Service Queue)
- Added Checks for certain GSX Settings to Menu Calls where appropriate (i.e. no Toolbar when Attach Tug on Boarding and Ignore Crew on De/Boarding are checked)
- Fixed Skip to Turn Button being visible before Preparation Phase
- Added Button to skip to Preparation Phase while in Turnaround Phase (to allow flying a 'first Leg' again)
- Added Option to call Pushback when the Final LS is received (regardless if Tug was attached before)
- Replaced System to handle Stair Conflicts (i.e. Refuel on Stair Side) with a new System to account for GSX Pathing/Blocking Changes:
  - Now uses the Positions from HandlerData to detect Fuel/Water/Lavatory Conflicts on Stair Side (only if there are rear Stairs)
  - Removed Plugin Option for Refuel on Stair Side and Profile Option to connect Stairs and Fuel (detected automatically and handled differently)
  - Stairs are not blocked anymore in Walkaround/Preparation for Aircrafts with Fuel on that Side
  - The Menu Question to remove Stairs is automatically handled by the App (always answering with 'Yes' to removed)
  - Removed Stairs are automatically re-attached during Departure by the App (or through GSX) when no conflicting Services called or queued
  - With the Exception of Refuel Active being the last one called left and the next queued Service requires Stairs (i.e. Cleaning or Boarding)
  - The Profile Setting to call Jetway/Stairs during Departure is changed to true in all Profiles as it is recommended with the new System (only the first Time when Config is updated/converted)
  - The Setting can be disabled if preferred, leaving Stairs removed until GSX automatically triggers them for a Service requiring them
- Reworked the Service Automation Queue:
  - New UI Element for Service Queue with interactive List Items allowing direct Manipulation
  - Queue is now "forward looking": the whole Queue is scanned for the next Service eligible to be called (so not strictly by order anymore)
  - The SmartButton for the next Service still calls the next queued Service - so following the Order
  - Stair Conflicts are Part of that Evaluation to momentarily pass affected Services (looking forward in the Queue)
  - Service Activation changed: controls if and which Dependency to use when to call a Service - Skip, Manual, Independent, By Departure Time or Dependent on previous Services
  - Service Dependency added: for Activation by previous Services - (Minimum) State of previous Services for the last Called, all Called, previous queued or all previous queued
  - Service Cancellation improved: can be cancelled by Max Run Time or Dependent on previous Services like the Activation (or Never)
  - Constraints are now independent Flags/Checkboxes, i.e. "Turn-on-Non-Hub" is now "Only Turn" combined with "Only Non-Hub"
  - Call Cargo Flag is now inversely displayed - displayed as a Constraint to only call on Pax Aircrafts
  - Added Button to reset the Service Queue to default Settings (only visible when the Queue is not in Use)
  - Old Profiles will automatically have their Departure Queue reset to the new Defaul on load/import/install
- Added new "Flight Briefing" Category under Automation View to pre-select Push & Gate and display Information about the Flight/Session:
  - The OFP Data (i.e. Weights) imported by Any2GSX (the Button under Monitor was thus removed)
  - The Time to Departure (TTD) as seen by the App
  - The current "Agent" handling Ground Services (showing if Services are externally controlled or called by A2G)
  - Pre-Selection of the Pushback Direction (as reported/display in by GSX) for automatic Selection by the App
  - The Static Options like QuickEdit and Straigh Pull/Push are also included
  - When a Direction is preselected, the Menu/Toolbar will not be enabled for Pushback (except in Icing Conditions)
  - Pre-Selection of the Arrival Gate for automatic Selection by the App on Taxi-In
  - Can be entered as free Text, i.e. "D44", even as soon as in the Departure Phase
  - Once landed can be selected from a (filtered) List (as the Arrival Airport Data is only then provided by GSX)
  - Either Way, the App will attempt to set the Gate via the respective API Command and if that fails, initiates a Menu Search to select the first Match 
  - Since GSX doesn't understand its own (customized) Names (duh), the List-Approach is not automatically more reliable
- Added Options to set GSX Settings which can improve Immersion (under App Settings):
  - Basically a "currated View" of GSX Settings found in the Menu
  - Activating a Setting (in A2G) persistently changes the Setting in GSX!
  - If a GSX Setting is already set to the recommended Value, the Profile Setting will automatically be set
- Added Options under App Settings to set some GSX Settings related to Loading:
  - Different Pax Densities depending if the Gate has a Jetway or Stairs only
  - Different Load Scale Values depending if the Aircraft is a Pax or Cargo Aircraft (only applied if there a multiple Trips)
  - Different multiple Train Tri Values depending if the Aircraft is a Pax or Cargo Aircraft
  - Hack/Option to set the GSX GPU connected for internal GPUs (for VDGS Info)
- Added generic Plugin Options for GSX -> Aircraft Door Mapping (visible when the Plugin has Door-Synch) as a common Way to configure Differences to the default Mapping (i.e. different Door for Front Catering)
- Added generic Plugin Option for triggering Parking Brakes on Session Start (used if the Aircraft has no Chocks and the Menu is not opening when the Aircraft is unsecured)
- Added generic Plugin Option to enforce Underground Refuel regardless of Setting in the Airport Profile
- Added Support for InputEvents/B-Vars and SimEvents/K-Vars in the Variable Checker (to assisst Lua/Binary Plugin Development: the Generic Plugin still only understand SimVars and LocalVars!)
- Added Call to enforce 'Auto' Value for the 'Automated staircases' GSX Setting (Attention: persistently!)
- Removed Application Config Option for Operator Select Timeout - now uses the Value configured in GSX 
- Removed Plugin Option for GSX Fuel Dialog (provided through HandlerData from GSX API)
- Added Detection for GSX' Reposition Setting to skip repositioning by the App
- Fixed the Sim Session Initialization to be triggered when the App was restarted while being not in a Session
- Added App Setting for GSX hard Reset: when enabled (default) the Binary is killed and started (old Behaviour), else the GSX API Restart Command is used
  - Using the Sys-Tray to Restart both the App and GSX will still use the hard Reset regardless of Setting
- Added a Notification/Info for EFB and PilotsDeck when a Service is cancelled
- Added Label for the current GSX Aircraft Profile in the Monitor View
- Changed Scrolling in Aircraft Options to scroll the whole Page instead of the individual Group Boxes
- Updated SimConnect Libraries


### EFB / WASM / PilotsDeck
- EFB: Added Support for Horizontal/Landscape Layout on the EFB App
- EFB: Added Status of external Control and Handler to the EFB App
- WASM: Fixed Issue with Unregistration for WASM (2) Events
- WASM: Fixed Issues with Events having more than one Flag (i.e. JS and WASM aka DEFAULT)
- EFB/WASM: Removed Messages for EFB Updates (less Spam in the Dev/Coherent Console)
- PilotsDeck: Added Pre Selection for Pushback Direction (does need PilotsDeck Integration enabled)
- PilotsDeck: Fixes to Menu Open/Close Button when not used with the Integration
- PilotsDeck: Menu is now cleared on Hide/2 Events when not used with the Integration
- EFB/WASM: Updated to SDK 1.8.1


### Installer
- Added Check to add/set the remote_server_enabled Setting in CouatlAddons.ini
  - The File is backed up as CouatlAddons.ini.bak if modified
- Fixed Issue with removing the CommBus Module un Uninstall when multiple Variants are installed (i.e. Store and Steam 2024)


### Plugin Interface
- Added Globals to use InputEvents (B-Vars) in Lua Plugins: UseInput, SubInput, ReadInput, WriteInput
- Renamed SendInput Method to WriteInput and requires the InputEvent to be registered (UseInput)
- Added ReadEvent Global for Lua Plugins to read last received Value of a registered SimEvent
- Added SetGenericSetting and SetPluginSetting Globals for Lua Plugins to change Settings dynamically (will be saved to Config)
- Removed Method GetIsFuelOnStairSide (detected through HandlerData)
- Changed Method GetIsCargo to be true when the generic Plugin Option is set -or- HandlerData reports a Cargo Aircraft
- Changed Method GetSettingFuelDialog to use HandlerData (generic Plugin Option removed)
- Reworked Door Handling: Methods added or changed to move common Logic from Plugins to App
  - New Enum AircraftDoor to define Doors generally (L1-L8, R1-R8, Cargo Front/Rear/MainBulk) used in all Plugin Methods to adress Doors
  - Mapping between GSX Door to Aircraft Door via GsxDoorMapping Dictionary to select AircraftDoor to be passed to Plugin Methods
  - The Dictionary is build with the Information from the GetAircraftDefaultDoor Plugin Method - Plugins are expected to report their Mapping for the Type/Variant via this Function
  - The new generic Plugin Options for Door Mapping will override the reported Plugin Value - as a common Way for Users to customize for their Profile (i.e. Stuff like Front Catering was configured to be on R2 instead of R1)
  - Added Methods to evaluate Handler Data - i.e. preferred Exit for Jetway, Rear Pax Exit present or Door is Standard or Custom (the later can be overriden by a Plugin, if needed)
  - Added Support to handle Standard Doors (interactive Points) - automatically registered (read through HandlerData) and Methods to Get/Set them directly by GsxDoor or Index
  - Added Methods for Plugins (Lua too with GetAircraftPlugin()) to register, get and set interactive point goal Variables by Index
  - Added Plugin Methods GetDoorCustom and SetDoorCustom used by the Aircraft Controller to check/set individual Doors on Service State/Vehicle Changes
  - Added GetDoor/SetDoor Base Methods which will use the Custom or Interactive Point Method depending on Type
  - Plugins can still (if needed) override the Methods for Events like Door triggered, Loader/Stair attached or Jetway/Stair State change - but now need to return a bool Value (true = handled)
  - If these Events are not overriden/handled by the Plugin, the Aircraft Controller will use the SetDoor/GetDoor Methods on Custom Doors to check and set their State (since Standard Doors are handled by GSX on these Events)
  - Methods like GetHasOpenDoors, SetCargoDoors, DoorsAllClose all use the new GetDoor/SetDoor Methods so they operate both Door Types
  - The Aircraft Controller closes all Doors on Session Start (if on Ground, no Engines running) and checks if Doors with Jetway/Stairs attached need to be re-opened in Preparation Phase
  - As DoorsAllClose is now working on any Aircraft, the Plugin Capability was removed
  - Added Methods to get Fuel/Lav/Water Panel State - used by GetHasOpenDoors to indicate open Doors (which can be closed through DoorsAllClose calling the Set Methods for the Panels)
  - Added Methods to get/set the Panel State for Fuel Operator (for separate Panel where the Refuel is controlled)
  - Added Method OnRefuelHoseConnection to notify Plugin about the GSX Fuel Hose State separately (i.e. Refuel in progress Messages)
  - Added Methods to get/set the Bulk Cargo Door separately (the default Implementation calls GetDoor/SetDoor for GSX' CargoDoor3Main)
  - The Methods are used in SetCargoDoors and DoorsAllClose as an additional Call so that Plugins can operate the Bulk Door when there is both a Main and Bulk Door
  - SetCargoDoors only opens/closes the Bulk Door when the new Profile Setting DoorCargoOperateBulk is true (default)
  - DoorsAllClose will close the Bulk Door if AircraftDoor mapped to CargoDoor3Main is not CargoBulk (effectively: Bulk is always closed regardless of Setting)
  - The GsxDoorMapping will map CargoDoor3Main to CargoBulk for Passenger Aircraft and to CargoMain on Cargo Aircraft
  - The GetHasOpenDoors Method is no longer overridable by Plugins, their check Logic is to be placed in CheckOpenDoors (used by GetHasOpenDoors together with the GetCargoDoorBulk and GetPanelX Functions)
  - The None Label was renamed to Standard (but same Value of 1) in the DoorTypeSynced Enum to indicate the Door Types synced in the Plugin Capabilities
  - The Door Types synced are now displayed when the DoorsSynced Property is greater than Standard
- Improved Exception/Error Logging for Lua Plugins
- C# Namespace Changes: moved GSX Interfaces and Classes to Interfaces.GSX and moved GsxConstants from App to Interface


### API
- Refactor of API and CommBus Message Handling and Parsing
  - Separated in dedicated Tasks listening on separate Ports to prevent future interference
  - Added Local Variable 'L:ANY2GSX_API_PORT' holding the active API Port used (besides the .port File, refreshed on every Session Start)
  - The API should basically work the same, just with dynamic Port Allocation
- Added additional Fields in status Request: profileGsx, aircraftPath, aircraftTitle, ofpId, servicesQueued, servicesCalled, servicesSkipped 
- Added Field handler to setExternal Method (to be displayed in EFB/UI)
- Added Method queueService to queue a Service in the Departure Queue with Fields to configure the new Service Queue Properties
- Changed Method callService to ignore the old activation Field and queue the Service as Independent (more or less calling it directly)
- Added Method cancelService to cancel a called Service
- Added Method setSavedFob to set/change saved FOB for Title/Livery (and force to set to Aircraft if Title matches and Phase is before Departure or in Turn)
- Added Method setBagCount to change the Bag Count independently from the Pax Count - setPaxCount will still set the Bag Count
- Added Method callPushback to request Pushback and optionally set Label for automatic Direction Selection (when in Pushback Phase and Pushback not running)
- Added Method setPushDirection to preselect Push Direction (but not call Pushback - i.e. during Departure)
- Added Method setGateArrival to automatically select a Gate in the Taxi-In Phase (via API gate.select or menu.search method)
  - Can be set already during Departure and is not related to the External Control State - applies at the Destination as if the User has set it
- Added Method smartCall to trigger a SmartButton Request externally (i.e. during Pushback to confirm Engine Start or pause/resume - if configured in the Profile Settings)
- Added Method setProfile to set a Setting in the current A2G Profile (permanently, but not directly reflected in the UI)
- Added Method getProfile to get a Setting from the current A2G Profile
- Added gates Endpoint returning a Dictionary of all Gates at the Airport (uiGateName => GsxPayloadHandlerGateInfo)
- Added pushback Endpoint returning the current Pushback State, Status, available Directions, pre-selected Direction, bypassPin and indication of the Release-Brake-Tracker

<br/><br/>
## v0.4.14

### App
- Fixed Input for Final Delay Min/Max Values

<br/><br/>
## v0.4.13

### App
- Updated NuGet Packages
- Updated MSFS SimConnect SDK


### Module
- Compiled with SDK 1.8.0


### Installer
- Set .NET 10.0.12 as Target

<br/><br/>
## v0.4.12

### App
- Import Simbrief actual Pax/Bag Count from OFP
  - Can be disabled in the OFP Import Options to restore prior Behaviour
- Added Option to prevent the App to set Brakes or Chocks on Session Start
- Fixed Stairs not called on Arrival (when not to configured to call Deboard on Arrival)
- Fixed Departure Phase not being triggered when GSX GPU (Plugin Options) was set to 'NoJetway' or 'Always' (i.e. FBW Profile)
- Fixed Jetway/Stairs reported as Available when being in the Unknown State
- Updated SimConnect SDK
- Updated Libraries

### Installer
- Set .NET 10.0.11 as Target

### Components
- Build WASM/Community Package with latest SDK

<br/><br/>
## v0.4.11

### App
- Some temporary Tweaks/Fixes on the (classic) Menu Call/Handling Stuff
- Fixed manual Operator Selection not possible due to Menu being disabled instantly
- Optimized Menu Handling on Arrival: Close/Disable Menu after (successful) Follow and Operator Selection
- Optimized Keep Direction Menu Open Feature
- Set .NET 10.0.10 as Target
- Updated SimConnect SDK
- Updated Libraries

<br/><br/>
## v0.4.10

### App
- (More) Adjustments to Jetway Operating State
- Added Handling for separate Operator Selection for Refuel
- Added Checks to detect externally answered FollowMe Question (although Skip FollowMe is enabled in A2G)
- Added some more Log Messages
- Fixed Operator Selection being skipped and Menu being reset because of previous Operator Selection in certain Situations
- Updated NuGet Packages

### Installer
- Set .NET 10.0.9 as Target

<br/><br/>
## v0.4.9

### App
- Added Button in Automation View to skip to Turnaround Phase (only available in Preparation Phase)
- Prevent Menu Calls in Settings Menu
- Added Workaround for repeating Jetway Calls when GSX reports Jetway available on Stands with no Jetway
- Skip automatic Selection of Operator when there is only one (GSX 4.0.3+)
- Remove 'Stand' from captured Gate
- Added some extra Log Messages
- Added some API Stuff to control Departure Services externally

<br/><br/>
## v0.4.8

### App
- Added ApuRunning to ReadyDeparture State in Plugin Interface
- Changed Menu Title for Additional Services (after GSX 4.0.0)
- Refined Gate Number Capture (Numbers only)
- Fixed Cargo Doors not opening on Boarding Service Activation when App was restarted before during Boarding

<br/><br/>
## v0.4.7

### App
- Fixed Issue on continously resetting the SmartButton

<br/><br/>
## v0.4.6

### App
- Changes due to GSX Behavior Change (Fix?) on Stair Vehicle Status
- Fixed Interface Method 'OnStairVehicleChange' not being calledd
- Fixed Type in Plugin Interface

<br/><br/>
## v0.4.5

### App
- Fixed Crash/Exception on Session Init when showing Simbrief reminder MessageBox
  - Generally removed Message Box centering to rule out similiar Situations
- Fixed Profile Matches getting lost when Profile is renamed
- Updated .NET Libraries

### Installer
- Set .NET 10.0.8 as Target

<br/><br/>
## v0.4.4

### App
- Added Option (App Setting) to enable/disable opening the Menu on Taxi-In

<br/><br/>
## v0.4.3

### App
- Fix Departure Phase Skip triggered to early (e.g. planned ZFW already reached at Boarding Start)
- Fix Service with manual Activation being called when configured as first Service in the Queue
- Startup Volumes (in Volume View) are now normalized (and limited) to 0-100% - regardless of the raw Value Range the Channel Definition is using
- Added generic Plugin Option to control wether GSX' Progressive Fuel should be disabled (default) or enabled

<br/><br/>
## v0.4.2

### App
- Revert Update-Distribution via jsdelivr and use githubcontent again (but with a new Approach)
- Update Notification Link now points to the GitHub (latest) Release
- Fixed Settings for Cockpit Notification Final Received and Chocks Placed not checked
- Reworked Parts of the Menu Handling to improve GSX Toolbar/Menu Usage (when to enable, close or disable)
  - Ditched the 'Disable User-enabled Menu' App Setting (too complicated)
  - The App will disable the Toolbar/Menu for all its Menu Calls
  - For manual Selection it will still *enable* the Toolbar/Menu
	- Unless 'Enable for manual Selections' is disabled or PilotsDeck Integration enabled
	- In any Case it will *open* the Menu for Selections (for other Frontends like EFB or PilotsDeck)
- Trying to fix GSX Toolbar/Menu not reacting to Selections when enabled on a Ready-State Menu
  - When enabling the Toolbar, the Menu may briefly disappear before it reappears (Fix is applied)
  - Can be disabled with 'GsxToolbarFixes' in the Configuration File
- Fixing GSX Toolbar/Menu not reacting to Disable in Walkaround Mode
- Added GsxMenuTimeoutFix Parameter to Config File (change at own Risk)
- Added Workaround to reset GSX Stair Vehicle State on GSX Restart
- Updated SimConnect Libraries to latest SDK

### Components
- Build WASM/Community Package with latest SDK

<br/><br/>
## v0.4.1

### App
- Fixed Min Flight Time Constraint
- Fixed edge Cases where the 'Release Brake' Notification did not go away
- Fixed extreme Refuel Rate triggering a Refuel<>Defuel Loop
- Added Alive-Check for PilotsDeck Connection
- Changed dynamic Refuel Rate (Time Target) to have a Minimum Rate (so there is enough Change for GSX to detect)
- Changed Handling of GSX Restarts/Crashes during Departure to catch Cases where Refuel was called but not started
- Refined Conditions for Toolbar/Menu Enablement for certain Menu Checks

<br/><br/>
## v0.4.0

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

<br/><br/>
