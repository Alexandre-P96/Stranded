using System;
using System.Runtime.CompilerServices;
using Sandbox.Citizen;

namespace Sandbox.Player;

public sealed class PlayerActions : Component
{
    [Property] public float Health { get; set; } = 100f;
    [Property] public float MaxHealth { get; set; } = 100f;
    [Property] public long Logs { get; set; }
    [Property] public long Rocks { get; set; }
    [Property] public List<string> Inventory { get; set; } = new() { "Fist" };

    public int ActiveSlot = 0;
    public int Slots => 9;

    [Property, Group("Movement")] public float GroundControl { get; set; } = 4.0f;
    [Property, Group("Movement")] public float AirControl { get; set; } = 0.1f;
    [Property, Group("Movement")] public float MaxForce { get; set; } = 50f;
    [Property, Group("Movement")] public float Speed { get; set; } = 160f;
    [Property, Group("Movement")] public float RunSpeed { get; set; } = 290f;
    [Property, Group("Movement")] public float CrouchSpeed { get; set; } = 90f;
    [Property, Group("Movement")] public float JumpForce { get; set; } = 350f;
    private float _footStepTimer = 0f;
    private float _footStepInterval = 0.40f; // Adjust this value to set the interval for footstep sounds
    private bool _wasInAir = false;

    public float PunchStrength = 1f;
    public float PunchCooldown = 2f;
    public float PunchRange = 50f;
    public ClothingContainer ClothingContainer;

    [Property] public GameObject Head { get; set; }
    [Property] public GameObject Body { get; set; }

    public Vector3 WishVelocity = Vector3.Zero;
    public bool IsCrouching;
    public bool IsSprinting;
    public bool IsCutting = false;
    public bool IsRocking = false;

    private CharacterController _characterController;
    private CitizenAnimationHelper _citizenAnimationHelper;
    public float CuttingSpeed = 5f;
    public float MiningSpeed = 5f;
    public long CuttingAmount = 1;
    public long MiningAmount = 1;
    public float Timer;
    private float _soundTimer = 0f;
    private float _saveTimer = 0f;
    TimeSince _lastPunch;
    private SoundEvent ResourceGained = new("sounds/kenney/ui/drop_002.vsnd_c") { UI = true, Volume = 2};

	protected override void OnStart()
    {
        InitializeComponents();
        ApplyClothing();
        LoadPlayerData();
    }

    protected override void OnUpdate()
    {
        UpdateCrouch();
        HandleInput();
        RotateBody();
        UpdateAnimations();
        PlayFootStepSound();
        StartAction(IsRocking, MiningSpeed, MiningAmount, GenericColliderController.ActionType.Rocking);
        StartAction(IsCutting, CuttingSpeed, CuttingAmount, GenericColliderController.ActionType.Cutting);
        SavePlayerData();
        HandleLandingSound();
    }

    private void SavePlayerData()
    {
	    _saveTimer += Time.Delta;
	    
	    if(_saveTimer < 10f) return;
	    
	    var playerData = new PlayerData
	    {
		    Wood = Logs,
		    Rocks = Rocks
	    };
	    PlayerData.Save(playerData);
	    
	    _saveTimer = 0f;
    }

    private void LoadPlayerData()
    {
	    var playerData = PlayerData.Load();
	    if ( playerData == null )
	    {
		    var data = new PlayerData
		    {
			    MiningLevel = 1,
			    WoodcuttingLevel = 1,
			    Rocks = 0,
			    Wood = 0,
		    };
		    PlayerData.Save(data);
	    }
	    else
	    {
			Logs = playerData.Wood;
			Rocks = playerData.Rocks;
	    }
    }

    protected override void OnFixedUpdate()
    {
        BuildWishVelocity();
        Move();
    }

    private void InitializeComponents()
    {
        _characterController = Components.Get<CharacterController>();
        _citizenAnimationHelper = Components.Get<CitizenAnimationHelper>();
    }

    private void ApplyClothing()
    {
        var model = Components.GetInChildren<SkinnedModelRenderer>();
        if (model != null)
        {
            ClothingContainer = ClothingContainer.CreateFromLocalUser();
            ClothingContainer.Apply(model);
        }
    }

    private void HandleInput()
    {
        IsSprinting = Input.Down("Run");

        if (Input.Pressed("Jump"))
            Jump();

        if (Input.Pressed("attack1") && _lastPunch >= PunchCooldown)
            Punch();

        if (_lastPunch >= PunchCooldown + 1)
            IdleAnimation();

        HandleInventoryInput();
    }

    private void HandleInventoryInput()
    {
        if (Input.MouseWheel.y >= 0)
        {
            ActiveSlot = (ActiveSlot + Math.Sign(Input.MouseWheel.y)) % Slots;
        }
        else if (Input.MouseWheel.y < 0)
        {
            ActiveSlot = ((ActiveSlot + Math.Sign(Input.MouseWheel.y)) % Slots) + Slots;
        }
    }

    private void UpdateCrouch()
    {
        if (_characterController is null) return;

        if (Input.Pressed("Duck") && !IsCrouching)
        {
            IsCrouching = true;
            _characterController.Height /= 2f;
        }

        if (Input.Released("Duck") && IsCrouching)
        {
            IsCrouching = false;
            _characterController.Height *= 2f;
        }
    }

    private void BuildWishVelocity()
    {
        WishVelocity = 0;
        var rot = Head.WorldRotation;

        if (Input.Down("Forward")) WishVelocity += rot.Forward;
        if (Input.Down("Backward")) WishVelocity += rot.Backward;
        if (Input.Down("Left")) WishVelocity += rot.Left;
        if (Input.Down("Right")) WishVelocity += rot.Right;

        WishVelocity = WishVelocity.WithZ(0);

        if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

        if (IsCrouching) WishVelocity *= CrouchSpeed;
        else if (IsSprinting) WishVelocity *= RunSpeed;
        else WishVelocity *= Speed;
    }

    private void Move()
    {
        var gravity = Scene.PhysicsWorld.Gravity;

        if (_characterController.IsOnGround)
        {
            _characterController.Velocity = _characterController.Velocity.WithZ(0);
            _characterController.Accelerate(WishVelocity);
            _characterController.ApplyFriction(GroundControl);
        }
        else
        {
            _characterController.Velocity += gravity * Time.Delta * 0.5f;
            _characterController.Accelerate(WishVelocity.ClampLength(MaxForce));
            _characterController.ApplyFriction(AirControl);
        }

        _characterController.Move();

        if (!_characterController.IsOnGround)
        {
            _characterController.Velocity += gravity * Time.Delta * 0.5f;
        }
        else
        {
            _characterController.Velocity = _characterController.Velocity.WithZ(0);
        }
    }

    private void RotateBody()
    {
        if (Body is null) return;

        var targetAngle = new Angles(0, Head.WorldRotation.Yaw(), 0).ToRotation();
        var rotateDifference = Body.WorldRotation.Distance(targetAngle);

        if (rotateDifference > 50f || _characterController.Velocity.Length > 10f)
        {
            Body.WorldRotation = Rotation.Lerp(Body.WorldRotation, targetAngle, Time.Delta * 3f);
        }
    }

    private void Jump()
    {
        if (!_characterController.IsOnGround) return;

        Sound.Play( new SoundEvent("sounds/footsteps/footstep-grass-jump-start-004.vsnd_c") { UI = true, Volume = 2} );
        _characterController.Punch(Vector3.Up * JumpForce);
        _citizenAnimationHelper?.TriggerJump();
    }

    private void UpdateAnimations()
    {
        if (_citizenAnimationHelper is null) return;

        _citizenAnimationHelper.WithWishVelocity(WishVelocity);
        _citizenAnimationHelper.WithVelocity(WishVelocity);
        _citizenAnimationHelper.AimAngle = Head.WorldRotation;
        _citizenAnimationHelper.IsGrounded = _characterController.IsOnGround;
        _citizenAnimationHelper.WithLook(Head.WorldRotation.Forward, 1f, 0.75f, 0.5f);
        _citizenAnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
        _citizenAnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
    }

    private void StartAction(bool isActive, float actionSpeed, long actionAmount, GenericColliderController.ActionType actionType)
    {
        if (!isActive) return;

        Timer += Time.Delta;
        _soundTimer += Time.Delta;

        if (_soundTimer >= 1f)
        {
            PlaySound(actionType);
        }

        if (Timer >= actionSpeed)
        {
            Timer = 0f;
            UpdateResource(actionAmount, actionType);
            Sound.Play(ResourceGained);
        }
    }

    private void UpdateResource(long amount, GenericColliderController.ActionType actionType)
    {
        if (actionType == GenericColliderController.ActionType.Cutting)
        {
            Logs += amount;
        }
        else if (actionType == GenericColliderController.ActionType.Rocking)
        {
            Rocks += amount;
        }
    }

    private void PlaySound(GenericColliderController.ActionType action)
    {
        Punch();
        switch (action)
        {
	        case GenericColliderController.ActionType.Cutting:
		        Sound.Play("impact-melee-wood");
		        break;
	        case GenericColliderController.ActionType.Rocking:
		        Sound.Play("impact-melee-concrete");
		        break;
        }
        _soundTimer = 0f;
    }

    private void Punch()
    {
        if (_citizenAnimationHelper != null)
        {
            _citizenAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
            _citizenAnimationHelper.Target.Set("b_attack", true);
            Sound.Play(new SoundEvent("sounds/physics/phys-impact-meat-2.vsnd_c") { UI = true, Volume = 2});
        }

        _lastPunch = 0f;
    }

    private void IdleAnimation()
    {
        if (_citizenAnimationHelper != null)
        {
            _citizenAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
        }
    }
    private void PlayFootStepSound()
    {
	    if (_characterController.IsOnGround && _characterController.Velocity.Length > 0)
	    {
		    _footStepTimer += Time.Delta;
		    if (_footStepTimer >= _footStepInterval)
		    {
			    Sound.Play("footstep-grass");
			    _footStepTimer = 0f;
		    } 
		    else if( IsSprinting )
		    {
			    if (_footStepTimer >= _footStepInterval / 1.40)
			    {
				    Sound.Play("footstep-grass");
				    _footStepTimer = 0f;
			    } 
		    }
	    }
	    else
	    {
		    _footStepTimer = 0f;
	    }
    }
    
    private void HandleLandingSound()
    {
	    if (_wasInAir && _characterController.IsOnGround)
	    {
		    Sound.Play(new SoundEvent("sounds/footsteps/footstep-gravel-jump-land-004.vsnd_c") { UI = true, Volume = 2});
	    }
	    _wasInAir = !_characterController.IsOnGround;
    }
    
    public void BuyUpgrade(string upgrade)
	{
	    if (upgrade == "Mining")
	    {
		    Log.Info( "Trying to upgrade mining" );
		    Rocks -= 1;
	    }
	    else if (upgrade == "Woodcutting")
	    {
			Log.Info( "Trying to upgrade woodcutting" );
			Logs -= 1;
	    }
	}
}
