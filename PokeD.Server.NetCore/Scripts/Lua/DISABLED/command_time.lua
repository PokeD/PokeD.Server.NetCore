Name		= "time"
Description = "Shows the current world time."
Aliases		= {}
Permission = "UserOrHigher"


--[[---------------------------------------------------------
    Name: Handle
    Desc:
-----------------------------------------------------------]]
function Handle(client)
	client:SendServerMessage(World.CurrentTime:ToString())
end
hook.Add ("Handle", "Time_Handle", Handle)

--[[---------------------------------------------------------
    Name: Help
    Desc:
-----------------------------------------------------------]]
function Help(client, alias)
	client:SendServerMessage("/"..alias..": "..Description)
end
hook.Add ("Help", "Time_Help", Help)