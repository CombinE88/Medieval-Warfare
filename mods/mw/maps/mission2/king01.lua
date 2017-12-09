Survivors = {Camp1, Camp2, Camp3, Camp4, Camp5, Camp6}
AllKilled = false
TentCount = 0
Tents = false
Barracks = false
LumberCount = 0
Lumber = false
BaronStartsRaiding = false
BaronBuildTimer = 0
BaronAttackGather = 0
BaronArmy = {}
BaronbuildNext = "footmen"

SendMCV = function()
	local foot = Actor.Create("mcv2new", true, { Owner = support, Facing = 0, Location = CPos.New(60,65) })
	foot.MoveIntoWorld(CPos.New(60,60))
	foot.Move(CPos.New(60,57))
	foot.Move(CPos.New(52,57))
	foot.Move(CPos.New(48,53))
	foot.Move(CPos.New(42,53))
	foot.Move(CPos.New(44,40))
	
	Trigger.OnExitedProximityTrigger(Map.CenterOfCell(CPos.New(44,40)), WDist.FromCells(5), function(C, id)
		if C.Owner == player and C.Type == "mcv2new" and not C.IsDead then
			C.Owner = support
			C.Stop()
			C.Move(CPos.New(44,40))
			Trigger.AfterDelay(DateTime.Seconds(2), function() 
				C.Owner = player
			end)
			Media.DisplayMessage("Better we camp here where it is a good spot.","Story")
			
		end
		if C.Owner == player and C.Type == "mcv2new" and C.IsDead then
			Trigger.AfterDelay(DateTime.Seconds(45), function() 		
				Media.DisplayMessage("The sorrounding land seems pretty rich on livestock, perhaps we schould build a hunters lodge.","Story")
				BaronStartsRaiding = true
			end)
		end
	end)
	
	
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(44,40)), WDist.FromCells(3), function(C, id2)
		if C.Owner == support and C.Type == "mcv2new" then
		
			C.Owner = player
			Trigger.RemoveProximityTrigger(id2)
		end
	end)
	
end

Reinforce = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	SendMCV()
end

WorldLoaded = function()
	player = Player.GetPlayer("King")
	support = Player.GetPlayer("Support")
	enemy = Player.GetPlayer("Baron")
	enemy2 = Player.GetPlayer("BaronCamp")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)


	Trigger.AfterDelay(DateTime.Seconds(1), function() 
		secureAreaObjective = player.AddPrimaryObjective("Find the nearby glade.")
		UserInterface.SetMissionText("Find the nearby glade.") 
		Media.DisplayMessage( "Find the nearby glade and free it of all enemy forces, so we can set up a camp before head any further.","Story")
				
		local cam = Actor.Create("CAMERA.small", true, { Owner = player, Location = CPos.New(45,40) })
		
		Trigger.OnAllKilled(Survivors, function()
		
			Reinforce()
			AllKilled = true
			
			basebuilding = player.AddPrimaryObjective("Establisch a little camp.")
			UserInterface.SetMissionText("Establisch a little camp with a atleast 6 tents, 2 lumber shacks and a barracks.") 
		
		end)
	end)
		
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(44,40)), WDist.FromCells(20), function(C, id2)
		if C.Owner == player and C.Type == "nodnuke" then
			TentCount = TentCount +1
		end
		if TentCount > 5 then
			Tents = true
		end
		if C.Owner == player and C.Type == "ljackb" then
			LumberCount = LumberCount +1
		end
		if LumberCount > 1 then
			Lumber = true
		end
		if C.Owner == player and C.Type == "barrnew" then
			Barracks = true
		end
		if Barracks and Tents and Lumber then
			Trigger.RemoveProximityTrigger(id2)
			raidbaron = player.AddPrimaryObjective("Build up a strong force and destroy all of Baron's forces that block our path ahead.")
			UserInterface.SetMissionText("Build up a strong force and destroy all of Baron's forces that block our path ahead.") 
		end
	end)

	
	
	
	
	Camera.Position = Actor122.CenterPosition

	
end

Tick = function()
	if AllKilled then
		player.MarkCompletedObjective(secureAreaObjective)
	end

	if player.HasNoRequiredUnits() and support.HasNoRequiredUnits() then
		player.MarkFailedObjective(secureAreaObjective)
		player.MarkFailedObjective(basebuilding)
		player.MarkFailedObjective(raidbaron)
	end
	
	if Barracks and Tents and Lumber then
		player.MarkCompletedObjective(basebuilding)
	end
	
	if enemy.HasNoRequiredUnits() and enemy2.HasNoRequiredUnits() then
		player.MarkCompletedObjective(raidbaron)
	end
	
	--if BaronStartsRaiding then
	--	BaronBuildTimer = BaronBuildTimer + 1
	--end
	--if BaronBuildTimer > 3000 and not Actor193.IsDead and BaronArmy.GetActors == 0 then
	--	Actor193.Build({"e2new","e4new","e3new2","e2new","e4new","e3new2","e2new","e4new"} function()
	--	table.insert(
	--	end)
	--	
	--end
	

	
end



--BaronStartsRaiding = false
--BaronBuildTimer = 0
--BaronAttackGather = 0
--BaronArmy = {}
--BaronbuildNext = "footmen"