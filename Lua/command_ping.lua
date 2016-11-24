Name		= "ping"
Description = "Ping pong."
Aliases		= {"pingP", "pingpong"}
Permission = "Default"


--[[---------------------------------------------------------
    Name: Handle
    Desc:
-----------------------------------------------------------]]
function Handle(client)
	client:SendServerMessage("Pong!")
end
hook.Add ("Handle", "Ping_Handle", Handle)

--[[---------------------------------------------------------
    Name: Help
    Desc:
-----------------------------------------------------------]]
function Help(client, alias)
	client:SendServerMessage("Correct usage is /"..alias)
end
hook.Add ("Help", "Ping_Help", Help)