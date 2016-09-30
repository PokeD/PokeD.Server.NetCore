Name		= "time"
Description = "Shows the current world time."
Aliases		= {}

function Handle(client)
	client:SendMessage(World.CurrentTime:ToString())
end

function Help(client)
	client:SendMessage("/"..alias..": "..Description)
end