namespace Sandbox.Player;

public sealed class GenericColliderController : Component, Component.ITriggerListener
{
    [Property] public ActionType CurrentActionType { get; set; } // Add property to specify action type

    private SoundEvent StartSound = new("sounds/tools/sfm/beep.vsnd_c") { UI = true };
    private SoundEvent StopSound = new("sounds/tools/sfm/denyundo.vsnd_c") { UI = true };

    private bool CanStartAction;
    private bool IsWithinRange;
    private PlayerController Player { get; set; }

    public enum ActionType
    {
        Cutting,
        Rocking
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other is CapsuleCollider && other.Tags.Has("player"))
        {
            Player = other.Components.Get<PlayerController>();
            Log.Info($"{CurrentActionType} on trigger");
            CanStartAction = true;
            IsWithinRange = true;
        }
    }

    protected override void OnUpdate()
    {
        if (Player != null && IsWithinRange)
        {
            if (Input.Pressed("use") && CanStartAction)
            {
                Sound.Play(StartSound);
                SetPlayerAction(true);
                CanStartAction = false;
            }
            else if (Input.Pressed("use") && !CanStartAction)
            {
                Sound.Play(StopSound);
                SetPlayerAction(false);
                CanStartAction = true;
                Player.Timer = 0f;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (Player != null && IsWithinRange)
        {
            if (IsPlayerActionActive())
                Sound.Play(StopSound);

            Log.Info($"outside {CurrentActionType}");
            CanStartAction = false;
            SetPlayerAction(false);
            Player.Timer = 0f;
            IsWithinRange = false;
        }
    }

    private void SetPlayerAction(bool isActive)
    {
        if (CurrentActionType == ActionType.Cutting)
            Player.IsCutting = isActive;
        else if (CurrentActionType == ActionType.Rocking)
            Player.IsRocking = isActive;
    }

    private bool IsPlayerActionActive()
    {
        return CurrentActionType == ActionType.Cutting ? Player.IsCutting : Player.IsRocking;
    }
}
