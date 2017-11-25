FootReinforcements = { "e3new", "e3new", "e2newv2", "e4newv2" }

SimpleReinForcments = function(units, waypointStart, waypointEnd)
	for i = 1, #units do
		local foot = Actor.Create(units[i], true, { Owner = player, Facing = 0, Location = waypointStart })
		foot.MoveIntoWorld(CPos.New(83,6))
		foot.Move(waypointEnd)
	end
end

Reinforce = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	SimpleReinForcments(FootReinforcements, CPos.New(85,6), CPos.New(79,6))
end

WorldLoaded = function()
	player = Player.GetPlayer("King")
	enemy = Player.GetPlayer("Baron")

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

	secureAreaObjective = player.AddPrimaryObjective("Eliminate all Baron forces in the area.")
	UserInterface.SetMissionText("Eliminate all Baron forces in the area.")

	Trigger.AfterDelay(DateTime.Seconds(30), function() Reinforce() end)
	Trigger.AfterDelay(DateTime.Seconds(90), function() Reinforce() end)
	Trigger.AfterDelay(DateTime.Seconds(180), function() Reinforce() end)
	Trigger.AfterDelay(DateTime.Seconds(195), function() Reinforce() end)
	
	Camera.Position = Actor230.CenterPosition

	
end

Tick = function()
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(secureAreaObjective)
	end

	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(secureAreaObjective)
	end
end
