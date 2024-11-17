using System;
using System.Runtime.CompilerServices;
using Sandbox.Citizen;

namespace Sandbox.Player;

public sealed class PlayerActions : Component
{
	[Property] public PlayerController Player { get; set; }
    [Property] public float Health { get; set; } = 100f;
    [Property] public float MaxHealth { get; set; } = 100f;
    [Property] public long Logs { get; set; }
    [Property] public long Rocks { get; set; }
    [Property] public GameObject Head { get; set; }
    [Property] public List<string> Inventory { get; set; } = new() { "Fist" };
    
    public int WoodcuttingLevel { get; set; } = 1;
    public long WoodcuttingExperience { get; set; } = 1;
    public int MiningLevel { get; set; } = 1;
    public long MiningExperience { get; set; } = 0;
    public int ActiveSlot = 0;
    public int Slots => 9;
    public float PunchCooldown = 2f;
    public ClothingContainer ClothingContainer;
    public bool IsCutting = false;
    public bool IsRocking = false;
    private CitizenAnimationHelper _citizenAnimationHelper;
    public float CuttingSpeed = 1f;
    public float MiningSpeed = 1f;
    public long CuttingAmount = 1;
    public long MiningAmount = 1;
    public float Timer;
    private float _soundTimer;
    private float _saveTimer;
    private TimeSince _lastPunch;
    private SoundEvent ResourceGained = new("sounds/kenney/ui/drop_002.vsnd_c") { UI = true, Volume = 2};

	protected override void OnStart()
    {
        InitializeComponents();
        ApplyClothing();
        LoadPlayerData();
    }

    protected override void OnUpdate()
    {
        //UpdateCrouch();
        HandleInput();
        //RotateBody();
        //UpdateAnimations();
       // PlayFootStepSound();
        StartAction(IsRocking, MiningSpeed, MiningAmount, GenericColliderController.ActionType.Rocking);
        StartAction(IsCutting, CuttingSpeed, CuttingAmount, GenericColliderController.ActionType.Cutting);
        SavePlayerData();
        //HandleLandingSound();
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

    private void InitializeComponents()
    {
	    _citizenAnimationHelper = Player.Components.Get<CitizenAnimationHelper>();
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
    
    /*private void Jump()
    {
        if (!_characterController.IsOnGround) return;

        Sound.Play( new SoundEvent("sounds/footsteps/footstep-grass-jump-start-004.vsnd_c") { UI = true, Volume = 2} );
        _characterController.Punch(Vector3.Up * JumpForce);
        _citizenAnimationHelper?.TriggerJump();
    }*/

    private void StartAction(bool isActive, float actionSpeed, long actionAmount, GenericColliderController.ActionType actionType)
    {
        if (!isActive) return;

        Timer += Time.Delta;
        _soundTimer += Time.Delta;

        if (_soundTimer >= 1f)
        {
            PlaySound(actionType);
            Punch();
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
            PlayerProgression.AddExperience( this, 25, actionType );
            
        }
        else if (actionType == GenericColliderController.ActionType.Rocking)
        {
            Rocks += amount;
            PlayerProgression.AddExperience( this, 25, actionType );
        }
    }

    private void PlaySound(GenericColliderController.ActionType action)
    {
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
    /*private void PlayFootStepSound()
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
    }*/
    
    /*private void HandleLandingSound()
    {
	    if (_wasInAir && _characterController.IsOnGround)
	    {
		    Sound.Play(new SoundEvent("sounds/footsteps/footstep-gravel-jump-land-004.vsnd_c") { UI = true, Volume = 2});
	    }
	    _wasInAir = !_characterController.IsOnGround;
    }*/
    
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
