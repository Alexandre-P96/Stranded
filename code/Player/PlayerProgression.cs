using System;
using Sandbox.Player;

public static class PlayerProgression
{
	private static int _baseXp = 500; 
	private static readonly double _xpModifier = 1.2;
	private static readonly double _levelModifier = 0.10;
	
	public static void AddExperience(PlayerActions player, int experience, GenericColliderController.ActionType actionType)
	{
		var experienceWithLevel = experience;

		switch (actionType)
		{
			case GenericColliderController.ActionType.Cutting:
				if (player.WoodcuttingLevel > 1)
				{
					experienceWithLevel = experience + (int)(experience * player.WoodcuttingLevel * _levelModifier);
				}
				player.WoodcuttingExperience += experienceWithLevel;
				CheckForLevelUp(player, actionType);
				break;

			case GenericColliderController.ActionType.Rocking:
				if (player.MiningLevel > 1)
				{
					experienceWithLevel = experience + (int)(experience * player.MiningLevel * _levelModifier);
				}
				player.MiningExperience += experienceWithLevel;
				CheckForLevelUp(player, actionType);
				break;
		}

		Log.Info("exp gained: " + experienceWithLevel);
	}

	public static int CalculateXPRequired(int level)
	{
		return (int)(_baseXp * Math.Pow(level, _xpModifier));
	}

	private static void CheckForLevelUp(PlayerActions player, GenericColliderController.ActionType actionType)
	{
		var currentLevel = 0;
		long currentXp = 0;

		switch (actionType)
		{
			case GenericColliderController.ActionType.Cutting:
				currentLevel = player.WoodcuttingLevel;
				currentXp = player.WoodcuttingExperience;
				break;
			case GenericColliderController.ActionType.Rocking:
				currentLevel = player.MiningLevel;
				currentXp = player.MiningExperience;
				break;
		}

		var xpRequired = CalculateXPRequired(currentLevel);
		Log.Info("xp required: " + xpRequired);

		while (currentXp >= xpRequired)
		{
			currentXp -= xpRequired;
			currentLevel++;
			xpRequired = CalculateXPRequired(currentLevel);
		}

		switch (actionType)
		{
			case GenericColliderController.ActionType.Cutting:
				player.WoodcuttingLevel = currentLevel;
				player.WoodcuttingExperience = currentXp;
				break;
			case GenericColliderController.ActionType.Rocking:
				player.MiningLevel = currentLevel;
				player.MiningExperience = currentXp;
				break;
		}
	}
}
