### App
- Fixed Min Flight Time Constraint
- Fixed edge Cases where the 'Release Brake' Notification did not go away
- Fixed extreme Refuel Rate triggering a Refuel<>Defuel Loop
- Added Alive-Check for PilotsDeck Connection
- Changed dynamic Refuel Rate (Time Target) to have a Minimum Rate (so there is enough Change for GSX to detect)
- Changed Handling of GSX Restarts/Crashes during Departure to catch Cases where Refuel was called but not started
- Refined Conditions for Toolbar/Menu Enablement for certain Menu Checks