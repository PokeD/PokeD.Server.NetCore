Name		= "Test Chat"
Description = "Test Chat System."
Alias		= "test"


local Subscribers = {}


--[[---------------------------------------------------------
    Name: MessageSend
    Desc:
-----------------------------------------------------------]]
function MessageSend(chatMessage)
	for key,value in pairs(Subscribers) do
		Subscribers[key].SendChatMessage(chatMessage)
	end

	return false
end
hook.Add ("MessageSend", "Test_MessageSend", MessageSend)

--[[---------------------------------------------------------
    Name: Subscribe
    Desc:
-----------------------------------------------------------]]
function Subscribe(client)
	table.insert (Subscribers, client)

	return true
end
hook.Add ("Subscribe", "Test_Subscribe", Subscribe)

--[[---------------------------------------------------------
    Name: UnSubscribe
    Desc:
-----------------------------------------------------------]]
function UnSubscribe(client)
	table.remove (Subscribers, client)

	return true
end
hook.Add ("UnSubscribe", "Test_Subscribe", UnSubscribe)