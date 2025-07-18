using Sandbox;
using Sandbox.NPCs;

public sealed class WanderersNpc : Component, IBaseNpc
{
	private GameObject WandererPrefab = GameObject.GetPrefab("prefabs/wandererprefab.prefab");
	private Vector3 SpawnPosition;
	public int Health { get; set; } = 150;
	private float TimeSinceDeath;
	private bool _isDead;

	protected override void OnStart()
	{
		SpawnPosition = WorldPosition;
	}

	protected override void OnUpdate()
	{
		if (Health <= 0 && !_isDead)
		{
			OnDeath();
		}

		if (_isDead)
		{
			TimeSinceDeath += Time.Delta;
			if (TimeSinceDeath > 3f)
			{
				// Clone a new NPC at the original position
				WandererPrefab.Clone(SpawnPosition);
				// Destroy this GameObject
				GameObject.Destroy();
			}
		}
	}

	public void OnDeath()
	{
		// Find all skeletal physics bodies and make them dynamic
		foreach (var body in GameObject.Components.GetAll<PhysicsBody>(FindMode.EverythingInSelf))
		{
			body.Enabled = true;
			body.BodyType = PhysicsBodyType.Dynamic;
			// Remove any constraints
			body.ClearForces();
		}
    
		// Disable any animation by stopping the model components from updating
		foreach (var modelComponent in GameObject.Components.GetAll<ModelRenderer>(FindMode.EverythingInSelf))
		{
			// If the component has a way to disable animations, do it
			if (modelComponent is SkinnedModelRenderer skinnedModel)
			{
				// We still want the model visible, but not animated
				//skinnedModel.AnimationEnabled = false;
			}
		}

		// Apply random impulse to make it fall naturally
		var mainBody = GameObject.Components.Get<PhysicsBody>();
		if (mainBody != null)
		{
			mainBody.ApplyImpulse(Vector3.Random.Normal * 500f);
		}

		_isDead = true;
		TimeSinceDeath = 0f;
	}
}
