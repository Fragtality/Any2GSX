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
