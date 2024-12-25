using Sandbox;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
	[Property] public float Damage { get; set; } = 10f;
	public void OnTriggerEnter(Collider other) 
	{
		other.GameObject.Root.Components.TryGet<Player>(out var player);
		if (player != null )
		{
			player.Health -= Damage;
		}
	}

	public void OnTriggerExit(Collider other) { }
}
