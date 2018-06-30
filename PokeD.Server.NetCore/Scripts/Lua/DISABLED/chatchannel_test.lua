Name		= "Lua Test Chat"
Description = "Lua Test Chat System."
Alias		= "lglobal"


local Subscribers = {}


--[[---------------------------------------------------------
    Name: MessageSend
    Desc:
-----------------------------------------------------------]]
function MessageSend(chatMessage)
	for key,value in pairs(Subscribers) do
		Subscribers[key].SendChatMessage(this, chatMessage)
	end

	return true
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
	table.remove (Subscribers, tablefind(Subscribers, client))

	return true
end
hook.Add ("UnSubscribe", "Test_Subscribe", UnSubscribe)

function tablefind(tab,el)
    for index, value in pairs(tab) do
        if value == el then
            return index
        end
    end
end
