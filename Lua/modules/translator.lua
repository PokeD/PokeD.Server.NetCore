languages = {}
language = "en"

local emptyTable = {}

function LoadLanguage(name, overridePath)
    local script = CompileFile(overridePath or ("lang/" .. name .. ".lua"))
    if not script then return end

    languages[name] = script.lang
end

function GetLanguage() return language or "en" end


function GetLanguageTable()
    local lang = GetLanguage()
    if languages[lang] then return languages[lang] end
    if languages["en"] then return languages["en"] end
    return emptyTable
end

function GetEnglishTable()
    if languages["en"] then return languages["en"] end
    return emptyTable
end

function ChangeLanguage(lang) language = lang end

function VarTranslate(s, reptable)
    for k, v in pairs(reptable) do
        s = s:gsub("{" .. k .. "}", v)
    end
    return s
end

function GetTranstation (languageTable, names)
    for k, name in pairs(names) do
        local a = rawget(languageTable, name)
        if a then
            --if type(a) == "function" then
            --    local ret = a(name)
            --    if ret then
            --        return ret
            --    end
            --end
            return a
        end
    end

    --local a = rawget(languageTable, "default")
    --if a then
    --    if type(a) == "function" then
    --       local ret = a(names[1])
    --        if ret then
    --           return ret
    --       end
    --   end
    --   return a
    --end
end

function Translate(client, ...)
    if (not type(client) == "IClient") then return "TRANSTALE_ERROR_CLIENT" end

    local lang = languages[client.Language.TwoLetterISOLanguageName]
    if not lang then lang = GetEnglishTable() end

    local args = {...}

    local a = GetTranstation(lang, args)
    if a then return a end

    return "TRANSTALE_ERROR"
end

function AdvTranslate(client, text, reptable)
    local s = Translate(client, text)

    for k, v in pairs(reptable) do
        s = s:gsub("{" .. k .. "}", v)
    end
    return s
end


--
-- Stuff after file loaded
--
local files = GetFiles("lang/")
for k, v in pairs(files) do
    local name = v:sub(1, -5)
    LoadLanguage(name)
end