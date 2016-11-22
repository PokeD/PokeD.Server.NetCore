Name		= "time"
Description = "Shows the current world time."
Aliases		= {}


--[[---------------------------------------------------------
    Name: Handle
    Desc:
-----------------------------------------------------------]]
function Handle(client)
	client:SendMessage(World.CurrentTime:ToString())
end
hook.Add ("Handle", "Time_Handle", Handle)

--[[---------------------------------------------------------
    Name: Help
    Desc:
-----------------------------------------------------------]]
function Help(client)
	client:SendMessage("/"..alias..": "..Description)
end
hook.Add ("Help", "Time_Help", Help)