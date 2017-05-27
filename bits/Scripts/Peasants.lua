PeasentTypes = {"mwwand1","mwwand2","mwwand3","mwwand6","mwwand4","mwwand5","mwwand10","mwwand11","mwwand12"}
Noble = {"noble3","noble4","noble1","noble2"}
PropsList = {"mwconst1","mwconst2","mwconst3","mwconst4","mwconst5","mwconst6","mwconst7"}


WorldLoaded = function()
	-- set up Player
	People = {}
	LumberShaks = {}
	Lumberer = {}
	
	CamelShacks = {}
	CamelTrader = {}
	
	--Timer = 50
	Timer = Utils.RandomInteger(625,1250)
	CamelTimer = Utils.RandomInteger(325,650)
	
	Trigger.AfterDelay(1, function()
		People = Player.GetPlayers(function(P)
			return P.InternalName ~= "Neutral" and P.InternalName ~= "Creeps" and P.InternalName ~= "Everyone"
		end)
	end)
	
	Trigger.AfterDelay(2,function()
		--print(tostring(#People) .. " player found")
		for i = 1,#People do
			
			local who = People[i]
			--print(who.Name .. " Triggeredd!")
						
			Trigger.AfterDelay(DateTime.Seconds(10),function()
				SpawnSettlers(who)
			end)
		end
		
		LumberShaks = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
			return C.Type == "split5"
		end)
		if LumberShaks and #LumberShaks > 0 then
			for i = 1, #LumberShaks do
				table.insert(Lumberer,"empty")
			end
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,45)),CheckForLumberjacks)
		end
		
		CamelShacks = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
			return C.Type == "split6"
		end)
		if CamelShacks and #CamelShacks > 0 then
			for i = 1, #CamelShacks do
				table.insert(CamelTrader,"empty")
				Trigger.AfterDelay(DateTime.Seconds(1),function()
					SpawnACamel(CamelShacks[i])
				end)
			end
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,45)),CheckForCamelTrader)
		end
		
	
	end)

end

CheckForCamelTrader = function()

	if CamelShacks and #CamelShacks > 0 then
		for i = 1, #CamelShacks do
			if CamelTrader[i] == "empty" then
				--print("Line 107, Camel: " .. tostring(CamelShacks[i].Location))
				table.insert(CamelTrader,i,Actor.Create("cameltrader",true,{Owner = CamelShacks[i].Owner,Location = CamelShacks[i].Location}))
				CamelTrader[i].Move(CamelTrader[i].Location+CVec.New(0,1),3)
			elseif CamelTrader[i].IsDead then
				table.remove(CamelTrader,i)
				--print("Line 111, Camel: " .. tostring(CamelShacks[i].Location))
				table.insert(CamelTrader,i,Actor.Create("cameltrader",true,{Owner = CamelShacks[i].Owner,Location = CamelShacks[i].Location}))
				CamelTrader[i].Move(CamelTrader[i].Location+CVec.New(0,1),3)
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,45)),CheckForCamelTrader)
	end
end

CheckForLumberjacks = function()

	if LumberShaks and #LumberShaks > 0 then
		for i = 1, #LumberShaks do
			if Lumberer[i] == "empty" then
				table.insert(Lumberer,i,Actor.Create("mwlumberer",true,{Owner = LumberShaks[i].Owner,Location = LumberShaks[i].Location}))
				--print("Line 107, Lumberer: " .. tostring(LumberShaks[i].Location)); Lumberer[i].Move(Lumberer[i].Location+CVec.New(0,1),3)
			elseif Lumberer[i].IsDead then
				table.remove(Lumberer,i)
				table.insert(Lumberer,i,Actor.Create("mwlumberer",true,{Owner = LumberShaks[i].Owner,Location = LumberShaks[i].Location}))
				--print("Line 107, Lumberer: " .. tostring(LumberShaks[i].Location)); table.insert(Lumberer,i,Actor.Create("mwlumberer",true,{Owner = LumberShaks[i].Owner,Location = LumberShaks[i].Location}))
				Lumberer[i].Move(Lumberer[i].Location+CVec.New(0,1),3)
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,45)),CheckForLumberjacks)
	end
end

SpawnACamel = function(Lactor)

	local actors = Map.ActorsInCircle(Map.CenterOfCell(Lactor.Location),WDist.FromCells(8),function(C)
		return (C.Type == "mwcamel" or C.Type == "grownmwcamel") and (not C.IsDead)
	end)
	
	if #actors < 9 and Lactor and (not Lactor.IsDead) then
		local camel = Actor.Create("mwcamel",true,{Owner = Lactor.Owner,Location = Lactor.Location+CVec.New(1,0)})
		camel.Move(camel.Location+CVec.New(0,1),3)
	end
	
end




Talkagain = function(Actor)
	if Actor and (not Actor.IsDead) then
		Actor.GrantCondition("Talking",Utils.RandomInteger(150,500))
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(15,90)),function()
			if Actor and (not Actor.IsDead) then
				Talkagain(Actor)
			end
		end)
	end
end

SpawnSettlers = function(Player)


	local who = Player
	local hut = nil
	local largehuts = {}
	local huts = {}
	local peasantss = {}
	local peasent = nil
	local PeasType = nil
	local castles = {}
	local nobles = {}
	local Facts = {}
	local Chicken = {}
	
	peasantss = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "mwwand1" or C.Type == "mwwand2" or C.Type == "mwwand3" or C.Type == "mwwand4" or C.Type == "mwwand5" or C.Type == "mwwand6" or C.Type == "mwwand10" or C.Type == "mwwand11" or C.Type == "mwwand12") and C.Owner == who
	end)
	
	Chicken = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "mwchick1") and C.Owner == who
	end)
	
	huts = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "nukenew" or C.Type == "sulnuke" or C.Type == "nodnuke" ) and C.Owner == who
	end)
	
	largehuts = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "nuk2new" or C.Type == "sulnuk2" or C.Type == "nodnuk2") and C.Owner == who
	end)
	
	Facts = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return C.Type == "factnew" and C.Owner == who
	end)
	
	if #peasantss < (#huts*2 + #largehuts*3) then
	
		if #largehuts ~= 0 and #huts ~= 0 then
		
			local rnd = Utils.RandomInteger(1,3)
			if rnd == 1 and #huts > 0 then
				hut = Utils.Random(huts)
			elseif rnd == 2 and #largehuts > 0 then
				hut = Utils.Random(largehuts)
			end
			
		elseif #largehuts > 0 then
			hut = Utils.Random(largehuts)
		elseif #huts > 0 then
			hut = Utils.Random(huts)
		elseif #Facts > 0 then
			hut = Utils.Random(Facts)
		end
		
		PeasType = Utils.Random(PeasentTypes)
		
		if hut and (not hut.IsDead) then
			--print("Line 424, Peasant: " .. tostring(hut.Location))
			peasent = Actor.Create(PeasType,true,{Owner = who,Location = hut.Location+CVec.New(1,1)})
			peasent.Move(peasent.Location+CVec.New(0,1),3)
			table.insert(peasantss,peasent)
			
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(30,150)),function()
				if peasent and (not peasent.IsDead) then
					Talkagain(peasent)
				end
			end)
		
		
			if (#peasantss + #nobles) > #Chicken*5 and hut and (not hut.IsDead) then
				Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,100)),function()
					if hut and (not hut.IsDead) then
						--print("Line 439, Chicken: " .. tostring(hut.Location))
						local chicko = Actor.Create("mwchick1",true,{Owner = who,Location = hut.Location+CVec.New(1,2)})
						chicko.Move(chicko.Location+CVec.New(0,1),3)
					end
				end)
			end
		end
		
	end
	
	nobles = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "noble1" or C.Type == "noble2" or C.Type == "noble3" or C.Type == "noble4") and C.Owner == who
	end)
	
	castles = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "castle" or C.Type == "castle2" or C.Type == "sulcastle") and C.Owner == who
	end)
	
	
	
	landlord = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return C.Type == "noble5" and C.Owner == who
	end)
	
	if #landlord < (#castles) and #castles > 0 then
	
		hut = Utils.Random(castles)
		peasent = nil
		
		if hut and (not hut.IsDead) then
			--print("Line 469, Noble: " .. tostring(hut.Location))
			peasent = Actor.Create("noble5",true,{Owner = who,Location = hut.Location+CVec.New(1,2)})
			peasent.Move(peasent.Location+CVec.New(0,1),3)
		
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(15,90)),function()
				if (not peasent.IsDead) and peasent then
					Talkagain(peasent)
				end
			end)
		end
	elseif #nobles < (#castles+4) and #castles > 0 then
		
		peasent = nil
		
		
		hut = Utils.Random(castles)
		PeasType = Utils.Random(Noble)
		
		if hut and (not hut.IsDead) then
			--print("Line 488, Noble: " .. tostring(hut.Location))
			peasent = Actor.Create(PeasType,true,{Owner = who,Location = hut.Location+CVec.New(1,2)})
			peasent.Move(peasent.Location+CVec.New(0,1),3)
			table.insert(peasantss,peasent)
		
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(15,90)),function()
				if peasent and (not peasent.IsDead) then
					Talkagain(peasent)
				end
			end)
		end
	end
	
	if (#largehuts + #huts) == 0 then
		Trigger.AfterDelay(DateTime.Seconds(10),function()
			SpawnSettlers(who)
		end)
	else
		Trigger.AfterDelay(DateTime.Seconds(10+math.floor(60/((#castles*3 +#largehuts*2 + #huts +2)/2))),function()
			SpawnSettlers(who)
		end)	
	end
	
	local Stuff = {}
	local Spawn = nil
	local dep = nil
	local here = {}
	local Factor = nil
	
	here = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "nukenew" or C.Type == "sulnuke" or C.Type == "sulnuk2" or C.Type == "nuk2new" or C.Type == "nodnuke" or C.Type == "nodnuk2") and C.Owner == who
	end)
	
	Stuff = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
		return (C.Type == "mwconst1" or C.Type == "mwconstback" or C.Type == "prop1" or C.Type == "prop2" or C.Type == "prop3" or C.Type == "prop4" or C.Type == "prop5" or C.Type == "prop6" or C.Type == "prop7" ) and C.Owner == who
	end)
	
	if #peasantss > ((#Stuff+1)*4) and #here > 0 and #Facts > 0  then
		
		Spawn = Utils.Random(here)
		Factor = Utils.Random(Facts)

		
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(3,8)),function()
			if Spawn and Factor and (not Factor.IsDead) and (not Spawn.IsDead) then
				--print("Line 533, Props: " .. tostring(hut.Location))
				dep = Actor.Create(Utils.Random(PropsList),true,{Owner = who,Location = Factor.Location+CVec.New(1,1)})
				
				dep.Move(dep.Location+CVec.New(0,1),3)
				dep.Move(Spawn.Location+CVec.New(Utils.RandomInteger(-4,5),Utils.RandomInteger(-4,5)),5)
				dep.Deploy()
			end
		end)
		
	end
end

Tick = function()

	CamelTimer = CamelTimer-1
	if CamelTimer < 1 then
		if CamelShacks and #CamelShacks > 0 then
			for i = 1, #CamelShacks do
				SpawnACamel(CamelShacks[i])
			end
		end
		CamelTimer = Utils.RandomInteger(325,650)
	end

end