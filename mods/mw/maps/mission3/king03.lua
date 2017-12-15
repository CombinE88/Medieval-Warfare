
WorldLoaded = function()
	Timer = 0
	Locked = false
	player = Player.GetPlayer("King")
	enemy = Player.GetPlayer("Baron")
	neutral = Player.GetPlayer("Neutral")
	_gate = nil
	
	got1 = false
	got2 = false
	got3 = false
	got4 = false
	
	OpenLDoor(Actor1,Actor0)
	OpenDoor(Actor50,Actor49)
	OpenDoor(Actor51,Actor48)
	OpenLDoor(Actor79,Actor166)
	OpenLDoor(Actor274,Actor100)
	OpenDoor(Actor197,Actor192)
	OpenDoor(Actor198,Actor194)
	OpenDoor(Actor199,Actor195)
	OpenDoor(Actor386,Actor382)
	OpenLDoor(Actor446,Actor424)
	OpenLDoor(Actor445,Actor99)
	
	Trigger.AfterDelay(DateTime.Seconds(4), function() 
	
		findawayintocastle = player.AddPrimaryObjective("Find a way into the castle.")
		UserInterface.SetMissionText("Find a way into the castle.") 
		Media.DisplayMessage( "Find yourself a way into the castle ahead.","Story")
		got1 = true
		
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
			got2 = true
			
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,97)), WDist.FromCells(3), function(C, id)
		if C.Owner == player then
		
			
			reachcells = player.AddPrimaryObjective("Find the prison cells.")
			UserInterface.SetMissionText("Find the prison cells.") 
			Media.DisplayMessage( "Theres a very important person in here somewhere, we need to find him!","Story")
			
			player.MarkCompletedObjective(findwayinner)
			
			Trigger.RemoveProximityTrigger(id)
			
			got3 = true

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
			
			Media.DisplayMessage( "Sewer key picked up!","Info")
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	Camera.Position = Actor350.CenterPosition
	
	
		Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,70)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and Timer == 0 and not Actor207.IsDead then
			Timer = 200
			Media.DisplayMessage( "This door is locked, we need the key to get throu.","Story")
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(69,70)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor383.IsDead and Actor207.IsDead then
			Actor383.Kill()
			Media.DisplayMessage( "Door Unlocked.","Info")
		end
	end)	
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(75,68)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor362.IsDead then
		
			Actor362.Kill()
			
			Media.DisplayMessage( "You found a Letter!","Info")
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(89,63)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor520.IsDead then
		
			Actor520.Kill()
			
			Media.DisplayMessage( "Prison cell key picked up!","Info")
			
			Trigger.RemoveProximityTrigger(id)

		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(87,75)), WDist.FromCells(2), function(C, id)
	
		if C.Owner == player and not Actor434.IsDead and Actor520.IsDead then
		
			Actor434.Kill()
			Media.DisplayMessage( "Prison door unlocked.","Info")
			
			if Actor362.IsDead then
				
				Trigger.AfterDelay(DateTime.Seconds(1), function() 
				
					Media.DisplayMessage( "You showed them the letter you found and they decided wanna join your fight.","Story")
					
					Actor97.Kill()
					Actor543.Move(CPos.New(89,78),2)
					Actor542.Move(CPos.New(89,78),2)
					Actor544.Move(CPos.New(89,78),2)
					Actor540.Move(CPos.New(89,78),2)
					Actor541.Move(CPos.New(89,78),2)
					Actor543.Owner = player
					Actor542.Owner = player
					Actor544.Owner = player
					Actor540.Owner = player
					Actor541.Owner = player
			
				end)
			elseif not Actor362.IsDead then
				
				Trigger.AfterDelay(DateTime.Seconds(1), function() 
				
					Media.DisplayMessage( "It wasn't a good idea to open that door.","Story")
					Actor97.Kill()
					Actor543.AttackMove(CPos.New(89,78),2)
					Actor542.AttackMove(CPos.New(89,78),2)
					Actor544.AttackMove(CPos.New(89,78),2)
					Actor540.AttackMove(CPos.New(89,78),2)
					Actor541.AttackMove(CPos.New(89,78),2)
					Actor543.Hunt()
					Actor542.Hunt()
					Actor544.Hunt()
					Actor540.Hunt()
					Actor541.Hunt()
			
				end)
			end
			
		Trigger.RemoveProximityTrigger(id)
		end
		
		
		
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(87,81)), WDist.FromCells(2), function(C, id)
		if C.Owner == player and not Actor96.IsDead and Actor520.IsDead then
			Actor96.Kill()
			Media.DisplayMessage( "Prison door unlocked.","Info")
			
			Actor.Create("idoorwest",true,{ Owner = neutral, Facing = 0, Location = CPos.New(80,61) })
			
			Actor547.Owner = player
				
			Trigger.AfterDelay(DateTime.Seconds(2), function() 
				
				Media.DisplayMessage( "We secured the master engineer, now lets get him out of here.","Story")
				
			end)
			Trigger.AfterDelay(DateTime.Seconds(4), function() 
				
				Actor98.Kill()
				Media.DisplayMessage( "A heavy gate has been lifted","Info")
				
			end)
			Trigger.AfterDelay(DateTime.Seconds(6), function() 
			
				surviveeng = player.AddPrimaryObjective("Escape with the engineer.")
				UserInterface.SetMissionText("Escape with the engineer alive.") 
				
				if got3 then
					player.MarkCompletedObjective(reachcells)
				end
				
				Media.DisplayMessage( "I think the only way to get out is where we came in!","Story")
				
				
				
				_test = Actor.Create("idooriron",true,{ Owner = neutral, Facing = 0, Location = CPos.New(89,71) })
				Media.DisplayMessage( "A heavy gate has been lifted","Info")
				
				got4 = true
			end)
			
			--Trigger.AfterDelay(DateTime.Seconds(10), function() 
			--	Media.DisplayMessage( "Unfortunaly the Mission ends here for now!","Story")
			--	
			--end)
			--Trigger.AfterDelay(DateTime.Seconds(14), function() 
			--	
			--	player.MarkCompletedObjective(surviveeng)
			--end)
			Trigger.RemoveProximityTrigger(id)
		end
		
	end)

	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(90,92)), WDist.FromCells(2), function(C, id)
	
		if C.Owner == player then	
			_gate = Actor.Create("idooriron",true,{ Owner = neutral, Facing = 0, Location = CPos.New(89,86) })
			
			Media.DisplayMessage( "A Trap, Kill the Commander to get out!","Story")
			
			Trigger.OnKilled(Actor566, function()
				if not _gate == nil then
					_gate.Kill()
				end
				Actor104.Kill()
				
				Media.DisplayMessage( "You got it! now get out.","Story")
				
				Actor.Create("idoorwest",true,{ Owner = neutral, Facing = 0, Location = CPos.New(59,97) })
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(70,117)), WDist.FromCells(5), function(C, id)
		if C.Owner == player and C == Actor547 then
		
			player.MarkCompletedObjective(surviveeng)
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
		if got1 then
			player.MarkFailedObjective(findawayintocastle)
		end
		if got2 then
			player.MarkFailedObjective(findwayinner)
		end
		if got3 then
			player.MarkFailedObjective(reachcells)
		end
		if got4 then
			player.MarkFailedObjective(surviveeng)
		end
	end
	if got4 and Actor547.IsDead then
		player.MarkFailedObjective(surviveeng)
	end

end