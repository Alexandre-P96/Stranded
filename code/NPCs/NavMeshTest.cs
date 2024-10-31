using Sandbox;
using Sandbox.Citizen;
using Sandbox.Player;

public sealed class NavMeshTest : Component
{
	[Property] public bool AttractAgents { get; set; }
	[Property] public PlayerActions Player { get; set; }

	RealTimeSince timeSinceUpdate = 0;
	private CitizenAnimationHelper _citizenAnimationHelper;

	protected override void OnStart()
	{
		_citizenAnimationHelper = Components.Get<CitizenAnimationHelper>();
	}
	
	protected override void OnUpdate()
	{
		if ( AttractAgents && timeSinceUpdate > 1 )
		{
			timeSinceUpdate = 0;
			foreach ( var agent in Scene.GetAllComponents<NavMeshAgent>() )
			{
				agent.MoveTo( Player.Transform.World.Position );
				//_citizenAnimationHelper.WithWishVelocity(500);
				_citizenAnimationHelper.WithVelocity(500);
				_citizenAnimationHelper.AimAngle = Player.Head.WorldRotation;
				_citizenAnimationHelper.WithLook(Player.Head.WorldRotation.Forward, 1f, 0.75f, 0.5f);
				_citizenAnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;

				/*
				agent.WishVelocity.WithFriction( 50 );*/
			}
		}
	}
}
