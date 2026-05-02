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
