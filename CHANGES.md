### App
- Fixed Walkaround being skipped after switchting to it in Preparation Phase although Walkaround is not configured in the Sim but Skip is activated
- Changed the GSX API Client to disconnect when an invalid hello Message is received (to force a reconnect)
- Fixed Issues where Dependent Services where already called when there was no called Service yet (i.e. because of a Manual Service in the Queue)
- Removed checking and setting the GSX Notification Options (App Settings) - i.e. the Options are not checked automatically anymore when the current GSX Settings are on the target Value
- Changed how the External Control State is displayed in the Monitor View (uncolored, On/Off)
- Fixed "Stairs operating" Notification to stay (maybe finally)

### Installer
- Added Check if the number of installed Simulators matches the number of installed Modules - else the Module Update Options will be shown, defaulting to Update on all Simulators
