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
        Rocking,
        Fishing
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
		    .WithAnyTags("tree", "rock", "water")
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
	    else if ( SceneTraceResult.Tags.Contains( "rock" ) )
	    {
		    SetPlayerRocking( true );
	    }
	    else if ( SceneTraceResult.Tags.Contains( "water" ) )
	    {
		    Log.Info("water");
		    SetPlayerFishing( true );
	    }

	    CanStartAction = false;
    }

    private void StopAction()
    {
	    Sound.Play(StopSound);
	    SetPlayerCutting(false);
	    SetPlayerRocking(false);
	    SetPlayerFishing( false );
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
	    
	    if ( !SceneTraceResult.Tags.Contains( "water" ) )
	    {
		    Player.Player.WalkSpeed = 110;
	    }
    }

    private void SetPlayerCutting(bool isActive)
    {
        Player.IsCutting = isActive;
    }
    
    private void SetPlayerRocking(bool isActive)
    {
		Player.IsRocking = isActive;
    }

    private void SetPlayerFishing( bool isActive )
    {
	    Player.Player.WalkSpeed = 0;
	    Player.IsFishing = isActive;
	    if ( !isActive )
	    {
		    Player.Player.WalkSpeed = 110;
	    }
    }

    private bool IsPlayerActionActive()
    {
	    return CurrentActionType switch
	    {
		    ActionType.Cutting => Player.IsCutting,
		    ActionType.Rocking => Player.IsRocking,
		    ActionType.Fishing => Player.IsFishing,
		    _ => false
	    };
    }
}
