-- for CCLuaEngine traceback
function __G__TRACKBACK__(msg)
    CCLuaLog("----------------------------------------")
    CCLuaLog("LUA ERROR: " .. tostring(msg) .. "\n")
    CCLuaLog(debug.traceback())
    CCLuaLog("----------------------------------------")
end

local function main()
	-- Initialize Game Settings
	local function init()
		-- avoid memory leak
		collectgarbage("setpause", 100)
		collectgarbage("setstepmul", 5000)

		--[[
			Set Frame Size
			For Simulating in Windows
			Remove or Annotate the below line when export the game
		--]]
		-- CCEGLView:sharedOpenGLView():setFrameSize(480, 800)

		-- Set Design Resolution Size
		CCEGLView:sharedOpenGLView():setDesignResolutionSize(480, 800, kResolutionShowAll)

		-- Set FPS
		CCDirector:sharedDirector():setAnimationInterval(1.0 / 60.0)

		-- Display FPS
		CCDirector:sharedDirector():setDisplayStats(true)
	end

	-- Initialize Game and Run Hello Scene
	init()
	require "hello"
	CCDirector:sharedDirector():runWithScene(helloScene())

end

xpcall(main, __G__TRACKBACK__)
