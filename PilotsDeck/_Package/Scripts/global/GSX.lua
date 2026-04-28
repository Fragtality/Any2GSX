local SCRIPT_TICK = 10000
RunInterval(SCRIPT_TICK, "MAIN_LOOP")
RunSim("MSFS")
RunEvent("K:EXTERNAL_SYSTEM_TOGGLE", "MENU_EVENT")
RunEvent("L:FSDT_GSX_MENU_CHOICE", "CHOICE_EVENT")
UseLog("gsx.log")
SimVar("X:ANY2GSX_RUNNING")
local GSX_MENU_TITLE = ""
SimVar("X:GSX_MENU_TITLE")
local GSX_MENU_LINES = {}
SimVar("X:GSX_MENU_LINE1")
SimVar("X:GSX_MENU_LINE2")
SimVar("X:GSX_MENU_LINE3")
SimVar("X:GSX_MENU_LINE4")
SimVar("X:GSX_MENU_LINE5")
SimVar("X:GSX_MENU_LINE6")
SimVar("X:GSX_MENU_LINE7")
SimVar("X:GSX_MENU_LINE8")
SimVar("X:GSX_MENU_LINE9")
SimVar("X:GSX_MENU_LINE10")
SimVar("L:FSDT_GSX_MENU_OPEN")
SimVar("L:FSDT_GSX_MENU_CHOICE")

local GSX_CFG_PATH = GetRegistryValue("HKEY_CURRENT_USER\\SOFTWARE\\FSDreamTeam", "root")
if GSX_CFG_PATH == "" or GSX_CFG_PATH == nil then
    GSX_CFG_PATH = "C:\\Program Files (x86)\\Addon Manager"
end
GSX_CFG_PATH = string.gsub(GSX_CFG_PATH, "\\", "/")

local GSX_CFG_MENUFILE = GSX_CFG_PATH .. "/MSFS/fsdreamteam-gsx-pro/html_ui/InGamePanels/FSDT_GSX_Panel/menu"
Log("Using GSX Menu Path: " .. GSX_CFG_MENUFILE)

function ANY2GSX_RUNNING()
	local state = SimRead("X:ANY2GSX_RUNNING")
	return state ~= nil and state ~= 0 and state ~= ""
end


function MAIN_LOOP()
	if ANY2GSX_RUNNING() then
        return
    end
end

local GSX_MENU_ISREADY = false
local GSX_MENU_LASTEVT = 4

function MENU_EVENT(evtData)
	if evtData > 4 then
		return
	end

	GSX_MENU_ISREADY = evtData == 1
    GSX_MENU_LASTEVT = evtData
	
	if ANY2GSX_RUNNING() then
        return
    end
	Log("MENU_READY '" .. evtData .. "' Event received")

	if GSX_MENU_ISREADY then
		GSX_MENU_READ()
	elseif evtData ~= 2 then
		GSX_MENU_CLEAR()
	end
end

function CHOICE_EVENT(evtData)
	if not ANY2GSX_RUNNING() then
		if evtData == -2 then
			GSX_MENU_CLEAR()
		end
		Log("MENU_CHOICE '" .. evtData .. "' Event received")
	end
end

function GSX_MENU_READ()
    local file = io.open(GSX_CFG_MENUFILE, "r")
    GSX_MENU_LINES = {}
    local count = 0
    if file ~= nil then
        for line in file:lines() do
            GSX_MENU_LINES[count] = line
            count = count + 1
        end
        file:close()
		Log("Menu File: Read " .. count .. " Lines")
	else
		Log("Menu File: Could not read File!")
    end

	for i = 0,10,1 do
        if i == 0 then
            SimWrite("X:GSX_MENU_TITLE", GSX_MENU_LINES[i])
            GSX_MENU_TITLE = GSX_MENU_LINES[i]
        else
            if string.len(GSX_MENU_LINES[i]) > 0 then
                SimWrite("X:GSX_MENU_LINE"..tostring(i), GSX_MENU_LINES[i])
            else
                SimWrite("X:GSX_MENU_LINE"..tostring(i), "")
            end
        end
	end
end

function GSX_MENU_CLEAR()
	for i = 0,10,1 do
        if i == 0 then
            SimWrite("X:GSX_MENU_TITLE", "")
            GSX_MENU_TITLE = ""
        else
            SimWrite("X:GSX_MENU_LINE"..tostring(i), "")
        end
	end
end

function SELECT(num)
	if not ANY2GSX_RUNNING() then
		GSX_MENU_CLEAR()
    end
	SimWrite("L:FSDT_GSX_MENU_CHOICE", num - 1)
end

function UNFREEZE()
    Log("Unfreeze - Setting Parking Brake")
    SimCommand("K:PARKING_BRAKE_SET")
    Sleep(300)
    Log("Unfreeze - Set Freeze SimVars to 0")
    SimCalculator("0 (>K:FREEZE_LATITUDE_LONGITUDE_SET) 0 (>K:FREEZE_ALTITUDE_SET) 0 (>K:FREEZE_ATTITUDE_SET)")
    Log("Unfreeze - Set GSX Freeze L-Var to 0")
    SimWrite("FSDT_VAR_Frozen", 0)
end