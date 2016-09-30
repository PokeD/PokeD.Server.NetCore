Name		= "ping"
Description = "Ping pong."
Aliases		= {"pingP", "pingpong"}

function Handle(client)
	client:SendMessage("Pong!")
end

function Help(client)
	client:SendMessage("Correct usage is /"..alias)
end