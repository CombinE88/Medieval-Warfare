
WorldLoaded = function()
	Timer = 0
	Locked = false
	player = Player.GetPlayer("King")
	enemy = Player.GetPlayer("Baron")
	
	OpenLDoor(Actor1,Actor0)
	OpenDoor(Actor50,Actor49)
	OpenDoor(Actor51,Actor48)
	OpenLDoor(Actor79,Actor166)
	OpenLDoor(Actor274,Actor100)
	OpenDoor(Actor197,Actor192)
	OpenDoor(Actor198,Actor194)
	OpenDoor(Actor199,Actor195)
	
	Trigger.AfterDelay(DateTime.Seconds(4), function() 
	
		findawayintocastle = player.AddPrimaryObjective("Find a way into the castle.")
		UserInterface.SetMissionText("Find a way into the castle.") 
		Media.DisplayMessage( "Find yourself a way into the castle ahead.","Story")
		
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,112)), WDist.FromCells(3), function(C, id)
		if C.Owner == player and Timer == 0 and not Actor1.IsDead then
			Timer = 125
			Media.DisplayMessage( "There must be a way to lift that door. We need to keep loking around.","Story")
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,109)), WDist.FromCells(3), function(C, id)
		if C.Owner == player then
		
			
			
			
			findwayinner = player.AddPrimaryObjective("Find a way into the inner.")
			UserInterface.SetMissionText("Find a way into the inner.") 
			Media.DisplayMessage( "We need to get behind the second gate now. I noticed a loose brick in the wall to the right. Maybe we can break throu!","Story")
			
			player.MarkCompletedObjective(findawayintocastle)
			
			Trigger.RemoveProximityTrigger(id)
			
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,97)), WDist.FromCells(3), function(C, id)
		if C.Owner == player then
		
			
			reachcells = player.AddPrimaryObjective("Find the prison cells.")
			UserInterface.SetMissionText("Find the prison cells.") 
			Media.DisplayMessage( "Theres a very important person in here somewhere, we need to find him!","Story")
			
			player.MarkCompletedObjective(findwayinner)
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(47,101)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor124.IsDead then
		
			Media.DisplayMessage( "Key picked up!","Info")
			Actor124.Kill()
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(50,93)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and Timer == 0 and not Actor106.IsDead then
			Timer = 200
			Media.DisplayMessage( "This door is locked, we need the key to get throu.","Story")
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(50,93)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor106.IsDead and Actor124.IsDead then
			Actor106.Kill()
			Media.DisplayMessage( "Door Unlocked.","Info")
		end
	end)	
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(49,72)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor101.IsDead then
			Actor101.Kill()
			Media.DisplayMessage( "We must defeat the commander.","Story")
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(49,59)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor207.IsDead then
		
			Actor207.Kill()
			
			Media.DisplayMessage( "Cell Key picked up!","Info")
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	Camera.Position = Actor350.CenterPosition
	
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(58,61)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and then
		
			
			Media.DisplayMessage( "Mission end! Thanks for testing!","Info")
			
			player.MarkCompletedObjective(reachcells)
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
end


OpenDoor = function(lever,door)
		Trigger.OnKilled(lever, function()
			door.Kill()
			Media.DisplayMessage( "A door has been opened","Info")
		end)
end
OpenLDoor = function(lever,door)
		Trigger.OnKilled(lever, function()
			door.Kill()
			Media.DisplayMessage( "A heavy gate has been lifted","Info")
		end)
end


Tick = function()
	
	if Timer > 0 then
		Timer = Timer -1
	end

	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(findawayintocastle)
		player.MarkFailedObjective(findwayinner)
		player.MarkFailedObjective(reachcells)
	end

end