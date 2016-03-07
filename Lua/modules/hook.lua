local Hooks  = {}


--[[---------------------------------------------------------
    Name: GetTable
    Desc: Returns a table of all Hooks.
-----------------------------------------------------------]]
function GetTable() return Hooks end


--[[---------------------------------------------------------
    Name: Add
    Args: string hookName, any identifier, function func
    Desc: Add a hook to listen to the specified event.
-----------------------------------------------------------]]
function Add(hookName, name, func)
	if (not type(hookName)	== "string") then return end
	if (not type(func)		== "function") then return end

	if Hooks[hookName] == nil then Hooks[hookName] = {} end

	Hooks[hookName][name] = func
end


--[[---------------------------------------------------------
    Name: Remove
    Args: string hookName, identifier
    Desc: Removes the hook with the given indentifier.
-----------------------------------------------------------]]
function Remove(hookName, name)
	if (not type(hookName) == "string") then return end
	if (not Hooks[hookName]) then return end

	Hooks[hookName][name] = nil
end


--[[---------------------------------------------------------
    Name: Call
    Args: string hookName, vararg args
    Desc: Calls hooks associated with the hook name.
-----------------------------------------------------------]]
function Call(hookName, ...)
	if (not type(hookName) == "string") then return end

	local HookTable = Hooks[hookName]
	if HookTable == nil then return end
	
	local a, b, c, d, e, f
	for k, v in pairs(HookTable) do
		if (type(k) == "string") then
			--
			-- If it's a string, it's cool
			--
			a, b, c, d, e, f = v(...)
		else
			--
			-- See https://github.com/garrynewman/garrysmod/blob/master/garrysmod/lua/includes/modules/hook.lua#L92
			-- If the object has become invalid - remove it
			--
			HookTable[k] = nil
		end

		--
		-- Hook returned a value - it overrides the function
		--
		if (a ~= nil) then
			return a, b, c, d, e, f
		end
	end    
end