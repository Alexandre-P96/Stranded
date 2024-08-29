using System;
using Sandbox.Citizen;

namespace Sandbox.Player;

public sealed class PlayerController : Component
{
    [Property] public float Health { get; set; } = 100f;
    [Property] public float MaxHealth { get; set; } = 100f;
    [Property] public long Logs { get; set; } = 100;
    [Property] public long Rocks { get; set; } = 100;
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
    TimeSince _lastPunch;
    private SoundEvent ResourceGained = new("sounds/kenney/ui/drop_002.vsnd_c") { UI = true };
    private List<SoundEvent> _rockHittingSounds = new()
    {
        new SoundEvent("sounds/impacts/melee/bluntweapon/concrete-1.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/concrete-2.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/concrete-3.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/concrete-4.vsnd") { UI = true }
    };
    private int _currentSoundIndex = 0;

    private List<SoundEvent> _woodHittingSounds = new()
    {
        new SoundEvent("sounds/impacts/melee/bluntweapon/wood-1.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/wood-2.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/wood-3.vsnd") { UI = true },
        new SoundEvent("sounds/impacts/melee/bluntweapon/wood-4.vsnd") { UI = true }
    };

    private SoundEvent WoodHittingSound = new("sounds/impacts/melee/impact-melee-wood.sound") { UI = true };

    protected override void OnStart()
    {
        InitializeComponents();
        ApplyClothing();
    }

    protected override void OnUpdate()
    {
        UpdateCrouch();
        HandleInput();
        RotateBody();
        UpdateAnimations();
        StartRocking();
        StartCutting();
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

        if (Input.Pressed("Crouch") && !IsCrouching)
        {
            IsCrouching = true;
            _characterController.Height /= 2f;
        }

        if (Input.Released("Crouch") && IsCrouching)
        {
            IsCrouching = false;
            _characterController.Height *= 2f;
        }
    }

    private void BuildWishVelocity()
    {
        WishVelocity = 0;
        var rot = Head.Transform.Rotation;

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

        var targetAngle = new Angles(0, Head.Transform.Rotation.Yaw(), 0).ToRotation();
        var rotateDifference = Body.Transform.Rotation.Distance(targetAngle);

        if (rotateDifference > 50f || _characterController.Velocity.Length > 10f)
        {
            Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 3f);
        }
    }

    private void Jump()
    {
        if (!_characterController.IsOnGround) return;

        _characterController.Punch(Vector3.Up * JumpForce);
        _citizenAnimationHelper?.TriggerJump();
    }

    private void UpdateAnimations()
    {
        if (_citizenAnimationHelper is null) return;

        _citizenAnimationHelper.WithWishVelocity(WishVelocity);
        _citizenAnimationHelper.WithVelocity(WishVelocity);
        _citizenAnimationHelper.AimAngle = Head.Transform.Rotation;
        _citizenAnimationHelper.IsGrounded = _characterController.IsOnGround;
        _citizenAnimationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
        _citizenAnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
        _citizenAnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
    }

    private void StartCutting()
    {
        if (!IsCutting) return;

        Timer += Time.Delta;
        _soundTimer += Time.Delta;

        if (_soundTimer >= 1f)
        {
            PlaySound(_woodHittingSounds);
        }

        if (Timer >= CuttingSpeed)
        {
            Timer = 0f;
            Logs += CuttingAmount;
            Sound.Play(ResourceGained);
        }
    }

    private void StartRocking()
    {
        if (!IsRocking) return;

        Timer += Time.Delta;
        _soundTimer += Time.Delta;

        if (_soundTimer >= 1f)
        {
            PlaySound(_rockHittingSounds);
        }

        if (Timer >= MiningSpeed)
        {
            Timer = 0f;
            Rocks += MiningAmount;
            Sound.Play(ResourceGained);
        }
    }

    private void PlaySound(List<SoundEvent> soundEvents)
    {
        Punch();
        Sound.Play(soundEvents[_currentSoundIndex]);
        _soundTimer = 0f;
        _currentSoundIndex = (_currentSoundIndex + 1) % soundEvents.Count;
    }

    private void Punch()
    {
        if (_citizenAnimationHelper != null)
        {
            _citizenAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
            _citizenAnimationHelper.Target.Set("b_attack", true);
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
}
