namespace Sandbox.Player;

public sealed class GenericColliderController : Component
{
    [Property] public ActionType CurrentActionType { get; set; } // Add property to specify action type

    private SoundEvent StartSound = new("sounds/tools/sfm/beep.vsnd_c") { UI = true };
    private SoundEvent StopSound = new("sounds/tools/sfm/denyundo.vsnd_c") { UI = true };

    private bool CanStartAction;
    private SceneTraceResult SceneTraceResult;
    private float DelayOnActionChecking;
    [Property] public PlayerActions Player { get; set; }

    public enum ActionType
    {
        Cutting,
        Rocking
    }

    protected override void OnUpdate()
    {
	    if (Player == null)
		    return;

	    PerformSceneTrace();

	    if (SceneTraceResult.Hit)
	    {
		    HandleHit();
	    }
	    else
	    {
		    HandleNoHit();
	    }
    }

    private void PerformSceneTrace()
    {
	    SceneTraceResult = Scene.Trace.FromTo(Player.Head.WorldPosition, Player.Head.WorldPosition + Player.Head.WorldRotation.Forward * 30)
		    .Size(30f)
		    .WithAnyTags("tree", "rock")
		    .Run();
    }

    private void HandleHit()
    {
	    if (Input.Pressed("use"))
	    {
		    if (CanStartAction)
		    {
			    StartAction();
		    }
		    else
		    {
			    StopAction();
		    }
	    }
    }

    private void StartAction()
    {
	    Sound.Play(StartSound);
	    if (SceneTraceResult.Tags.Contains("tree"))
	    {
		    SetPlayerCutting(true);
	    }
	    else if (SceneTraceResult.Tags.Contains("rock"))
	    {
		    SetPlayerRocking(true);
	    }

	    CanStartAction = false;
    }

    private void StopAction()
    {
	    Sound.Play(StopSound);
	    SetPlayerCutting(false);
	    SetPlayerRocking(false);
	    CanStartAction = true;
	    Player.Timer = 0f;
    }

    private void HandleNoHit()
    {
	    DelayOnActionChecking += Time.Delta;

	    if (DelayOnActionChecking < 0.5f)
		    return;

	    if (IsPlayerActionActive())
		    Sound.Play(StopSound);

	    SetPlayerCutting(false);
	    SetPlayerRocking(false);
	    CanStartAction = true;
	    Player.Timer = 0f;
	    DelayOnActionChecking = 0f;
    }

    private void SetPlayerCutting(bool isActive)
    {
        if (CurrentActionType == ActionType.Cutting)
            Player.IsCutting = isActive;
    }
    
    private void SetPlayerRocking(bool isActive)
    {
		if (CurrentActionType == ActionType.Rocking)
		    Player.IsRocking = isActive;
    }

    private bool IsPlayerActionActive()
    {
        return CurrentActionType == ActionType.Cutting ? Player.IsCutting : Player.IsRocking;
    }
}
