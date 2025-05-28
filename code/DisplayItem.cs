using System;
using Sandbox.Player;

public sealed class DisplayItem : Component
{
	// Configuration
	public float HoverHeight { get; set; } = 10f; // 50 units = ~half a meter
	public float RotationSpeed { get; set; } = 18f; // 360 degrees / 5 seconds = 72 degrees per second
	public string ItemName { get; set; } = "Test Item";

	private Vector3 _basePosition;

	protected override void OnUpdate()
	{
		// Update rotation
		var currentRotation = WorldRotation;
		var newRotation = currentRotation.RotateAroundAxis(Vector3.Up, RotationSpeed * Time.Delta);
		WorldRotation = newRotation;

		// Maintain hover height with oscillation
		var oscillation = MathF.Sin(Time.Now * 0.5f) * 3f; // Adjust the speed and amplitude as needed
		var targetPos = _basePosition + Vector3.Up * (HoverHeight + oscillation);
		WorldPosition = targetPos;

		// Perform ray trace to check if the player is looking at the item
		PerformRayTrace();
	}

	protected override void OnAwake()
	{
		_basePosition = WorldPosition;
	}

	private void PerformRayTrace()
	{
		var player = Components.Get<PlayerActions>();
		
		if (player == null) return;
		
		var traceResult = Scene.Trace.FromTo(player.Head.WorldPosition, player.Head.WorldPosition + player.Head.WorldRotation.Forward * 30)
			.Size(30f)
			.WithAnyTags("tree", "rock", "water")
			.Run();

		if (traceResult.Tags.Contains("loot"))
		{
			DisplayItemInfo();
		}
	}

	private void DisplayItemInfo()
	{
		// Display the item's information in the world
		Log.Info($"Item Name: {ItemName}");
		// You can add more code here to display the information in the game world
	}
}
