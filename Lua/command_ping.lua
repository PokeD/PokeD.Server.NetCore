Name		= "ping"
Description = "Ping pong."
Aliases		= {"pingP", "pingpong"}


--[[---------------------------------------------------------
    Name: Handle
    Desc:
-----------------------------------------------------------]]
function Handle(client)
	client:SendMessage("Pong!")
end
hook.Add ("Handle", "Ping_Handle", Handle)

--[[---------------------------------------------------------
    Name: Help
    Desc:
-----------------------------------------------------------]]
function Help(client)
	client:SendMessage("Correct usage is /"..alias)
end
hook.Add ("Help", "Ping_Help", Help)