using Sandbox;
using Sandbox.NPCs;

public sealed class WanderersNpc : Component, IBaseNpc
{
	private GameObject WandererPrefab = GameObject.GetPrefab( "prefabs/wandererprefab.prefab" );
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
				WandererPrefab.Clone(SpawnPosition);
				TimeSinceDeath = 0f;
				_isDead = false;
			}
		}
	}

	public void OnDeath()
	{
		GameObject
		_isDead = true;
		TimeSinceDeath = 0f;
	}
}
