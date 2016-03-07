NPC.PureName = "Announcer"

NPC.LevelFile = "goldenrod.dat"

NPC.Facing = 3
NPC.Position = Vector3(9, 0, 22)
NPC.Skin = "20"

NPC.PokemonVisible = false



local ClientExtraData = {}

local Tournament = {}
Tournament.Name = "Shitarment"


--[[---------------------------------------------------------
    Name: Update
    Desc: Function that is called a lot.
-----------------------------------------------------------]]
function Update ()
end
hook.Add ("Update", "Announcer_Update", Update)


--[[---------------------------------------------------------
    Name: Update2
    Desc: Function that is called every second.
-----------------------------------------------------------]]
function Update2()
    local clients = NPC:GetLocalPlayers ()
    
    for i=1, #clients do
        local client = clients[i]

        if not ClientExtraData[client.ID] then ClientExtraData[client.ID] = { } end
        local extraData = ClientExtraData[client.ID]

        if ClientIsNear (client) then
            if not extraData.ChatInitialized then
                NPC:SayPlayerPM(client, translator.AdvTranslate(client, "Greetings", { name = client.Name }))
                extraData.ChatInitialized = true
            end
        else
            extraData.ChatInitialized = false
        end
    end

end
hook.Add ("Update2", "Announcer_Update2", Update2)


--[[---------------------------------------------------------
    Name: ClientIsNear
    Args: IClient client
    Desc: Checks if the client is near NPC (2 blocks).
-----------------------------------------------------------]]
function ClientIsNear(client)
    if client.Position:DistanceTo(NPC.Position) < 2 then
        return true
    else
        return false
    end
end


--[[---------------------------------------------------------
    Name: Update2
    Desc: Function that is called every second.
-----------------------------------------------------------]]
function PrivateMessage (client, message)

    if message == "/tournament" and (not ClientExtraData[client.ID].TournamentRequest or ClientExtraData[client.ID].TournamentRequest == "no") then
        NPC:SayPlayerPM (client, translator.AdvTranslate (client, "Tournament1", { name = Tournament.Name }))
        ClientExtraData[client.ID].TournamentRequest = "waiting"
    end
    if message == "yes" or message == "yep" and (not ClientExtraData[client.ID].TournamentRequest or ClientExtraData[client.ID].TournamentRequest == "waiting") then
        NPC:SayPlayerPM (client, translator.AdvTranslate (client, "Tournament2", { name = Tournament.Name }))
        ClientExtraData[client.ID].TournamentRequest = "yes"
    end
    if message == "no" or message == "nope" and (not ClientExtraData[client.ID].TournamentRequest or ClientExtraData[client.ID].TournamentRequest == "waiting") then
        NPC:SayPlayerPM (client, translator.AdvTranslate (client, "Tournament3", { name = Tournament.Name }))
        ClientExtraData[client.ID].TournamentRequest = "no"
    end


    if message == "hi" or message == "hai" or message == "hello" then
        NPC:SayPlayerPM (client, translator.AdvTranslate(client, "Greetings", { name = client.Name }))
    end


    if string.find(message, "/move") then
        local tFinal = {}
        message:gsub("%d+", function(i) table.insert(tFinal, i) end)
        local x = tFinal[1]
        local y = tFinal[2]
        local z = tFinal[3]

        NPC.Position = Vector3(tonumber(x), tonumber(y), tonumber(z))
    end
end
hook.Add ("PrivateMessage", "Announcer_PrivateMessage", PrivateMessage)