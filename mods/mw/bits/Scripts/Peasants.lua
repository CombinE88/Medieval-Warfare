PeasentTypes = {"mwwand1","mwwand2","mwwand3","mwwand6","mwwand4","mwwand5","mwwand10","mwwand11","mwwand12"}
StartPeasentTypes = {"mwwand1","mwwand2","mwwand3","mwwand6","mwwand4","mwwand5"}
Noble = {"noble3","noble4","noble1","noble2"}
PropsList = {"mwconst1","mwconst2","mwconst3","mwconst4","mwconst5","mwconst6","mwconst7"}
PopulationModifierX1 = {"engeneer", "e1new", "e2new", "e2newv2", "enew", "e3newv2", "e9new", "e4new", "e4newv2", "e5new", "rmbonew", "newcomm", "assassin", "e8farmer", "bummler", "orni", "warkite", "drgnrdr", "wrbln", "ross1", "ross2", "ross5", "ross3", "ross4", "ross4b", "ross4c", "egarvxl", "transporter", "sule1", "sule2", "sule3", "sule4", "sule5", "sule6", "sulm1", "sulm2", "sulm4", "sulm3"}
PopulationModifierX2= {"siege1", "siege2", "sulb0", "sulb1"}
PopulationModifierX3= {"sulb2", "davinci"}
PopulationModifierX4= {"siege3", "siege4"}
PropCratesBarrels = {}
Transporter1 = {}
Transporter2 = {}
Transporter3 = {}
Transporter4 = {}
TransportProvider = {}
FreeEngineer = {}
ProxyTriger = {}

WorldLoaded = function()
	-- set up Player
	People = {}
	LumberShaks = {}
	Lumberer = {}
	respawntime = 30
	StartSpawnedCivs = {}
	
	
	--Timer = 50
	Timer = Utils.RandomInteger(625,1250)
	TimerCheck = 100
	
	Trigger.AfterDelay(1, function()
		People = Player.GetPlayers(function(P)
			return P.InternalName ~= "Neutral" and P.InternalName ~= "Creeps" and P.InternalName ~= "Everyone"
		end)
	end)
	
	Trigger.AfterDelay(2,function()
		--print(tostring(#People) .. " player found")
	
		LumberShaks = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(C)
			return C.Type == "split5"
		end)
		if LumberShaks and #LumberShaks > 0 then
			for i = 1, #LumberShaks do
				table.insert(Lumberer,"empty")
			end
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(25,45)),CheckForLumberjacks)
		end
	
	end)
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

