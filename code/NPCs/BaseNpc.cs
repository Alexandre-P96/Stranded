namespace Sandbox.NPCs;

public class BaseNPC : IBaseNpc
{
	public virtual int Health { get; set; } = 100;

	public virtual void OnDeath()
	{
		// Base implementation can be empty or have common death behavior
	}
}

public interface IBaseNpc
{
	public int Health { get; set; }
	public void OnDeath();
}
