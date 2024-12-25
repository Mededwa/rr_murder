using Sandbox;
using Sandbox.Citizen;
using Scenebox;
using System;
using System.Linq;
using System.Net.Http;
public sealed class Player : Component
{
	public static Player Local => Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.IsOwner );
	[RequireComponent] public Inventory Inventory { get; set; }
	[RequireComponent] public PlayerController PlayerController{ get; set; }

	[Property, Group("Stats")] public string Name { get; set; } = "XD";
	[Property, Group( "Stats" )] public float Health { get; set; } = 100f;
	[Property, Group( "Stats" )] public float MaxHealth { get; set; } = 100f;
	[Property, Group( "Stats" )] public float Stamina { get; set; } = 100f;
	[Property, Group( "Stats" )] public int Clues { get; set; } = 0;
	public bool DisplayHotbar { get; set; } = false;

	public int ActiveSlot = 0;
	public int Slots => 2;

	[Property] public string Role = null;

	[Property, Group("References")] public GameObject FlashlightObject { get; set; }
	[Property, Group( "References" )] public GameObject Head { get; set; }
	[Property, Group( "References" )] public ModelPhysics ModelPhysics { get; set; }
	[Property, Group( "References" )] public Collider PlayerBoxCollider { get; set; }
	[Property, Group( "References" )] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property, Group( "References" )] public GameObject Body { get; set; }







	[Sync] public bool IsFlashlightOn { get; set; } = false;
	[Sync] public Angles Direction { get; set; } = Angles.Zero;
	[Sync] public CitizenAnimationHelper.HoldTypes CurrentHoldType { get; set; } = CitizenAnimationHelper.HoldTypes.None;



	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Network.IsProxy ) return;

		if ( Input.Pressed( "Flashlight" ) )
		{
			Log.Info( "Press f ^^" );
			IsFlashlightOn = !IsFlashlightOn;
			BroadcastFlashlightSound();
		}

		if ( Input.MouseWheel.y >= 0 )
		{
			ActiveSlot = (ActiveSlot + Math.Sign( Input.MouseWheel.y )) % Slots;
		}
		else if ( Input.MouseWheel.y < 0 )
		{
			ActiveSlot = ((ActiveSlot + Math.Sign( Input.MouseWheel.y )) % Slots) + Slots;
		}

		if ( Input.MouseWheel.y != 0 )
		{
			DisplayHotbar = true;
		}

		if ( Input.Released( "attack1" ) && DisplayHotbar == true )
		{
			DisplayHotbar = false;
		}
		FlashlightObject.Enabled = IsFlashlightOn;

		UpdateCamera();
	}


	[Rpc.Broadcast]
	void BroadcastFlashlightSound()
	{
		var sound = Sound.Play( "flashlight.toggle" );
		if ( !IsProxy )
		{
			sound.Volume = 0.4f;
			sound.ListenLocal = true;
		}
	}


	public void Damage(float amount)
	{
		if (Health <= 0) return;
		if (IsProxy ) return;
		Health -= (int)amount;
	}

	void UpdateCamera()
	{
		var eyeAngles = Head.WorldRotation.Angles();
		var sens = Preferences.Sensitivity;
		eyeAngles.pitch += Input.MouseDelta.y * sens / 100f;
		eyeAngles.yaw -= Input.MouseDelta.x * sens / 100f;

		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.WorldRotation = Scene.Camera.WorldRotation;

		var camPos = Head.WorldPosition;
		var camForward = eyeAngles.Forward;
		var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * 150) )
			.WithoutTags( "player", "trigger" )
			.Run();

		if ( camTrace.Hit )
		{
			camPos = camTrace.HitPosition + camTrace.Normal;
		}
		else
		{
			camPos = camTrace.EndPosition;
		}

		Scene.Camera.WorldPosition = camPos;
		Scene.Camera.WorldRotation = eyeAngles;
		Scene.Camera.FieldOfView = 90f;
		Direction = eyeAngles;
	}

	[Rpc.Broadcast]
	public void Damage( float amount, int damageType = 0 )
	{
		if ( Health <= 0 ) return;
		if ( IsProxy ) return;

		var sound = Sound.Play( "impact-melee-flesh" );
		sound.ListenLocal = true;

		Health -= (int)amount;
		if ( Health <= 0 )
		{
			Kill( damageType, Rpc.Caller?.DisplayName ?? "" );
		}
	}

	void SetRagdoll( bool enabled )
	{
		ModelPhysics.Enabled = enabled;
		AnimationHelper.Target.UseAnimGraph = !enabled;

		RoundController.Instance.BroadcastSetTag( GameObject.Id, "ragdoll", enabled );

		if ( !enabled )
		{
			GameObject.LocalPosition = Vector3.Zero;
			GameObject.LocalRotation = Rotation.Identity;
		}

		ShowBodyParts( enabled );

		Transform.ClearInterpolation();
	}

	[Rpc.Broadcast]
	public void Kill( int damageType = 0, string killer = "", bool enableRagdoll = true )
	{
		GameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		GameObject.Network.SetOrphanedMode( NetworkOrphaned.Host );
		if ( enableRagdoll )
		{
			SetRagdoll( true );
			PlayerBoxCollider.Enabled = false;
			var fadeAfter = Components.GetOrCreate<FadeAfter>();
			fadeAfter.Time = 10f;
			fadeAfter.FadeTime = 4f;
		}
		else
		{
			GameObject.Tags.Set( "invisible", true );
			SetRagdoll( false );
		}
		if ( IsProxy ) return;
		Health = 0;

		Inventory.HolsterWeapon();
		BroadcastDestroy( GameObject.Id );
	}

	[Rpc.Broadcast]
	void BroadcastDestroy( Guid id )
	{
		var gameObject = Scene.Directory.FindByGuid( id );
		if ( gameObject.IsValid() )
		{
			AnimationHelper.Components.GetOrCreate<PropHelper>();
			Components.Get<Inventory>()?.Destroy();
			Components.Get<CharacterController>()?.Destroy();
			Components.Get<Voice>()?.Destroy();
			Components.Get<Player>()?.Destroy();
		}
	}

	void ShowBodyParts( bool show )
	{
		var renderers = AnimationHelper.GameObject.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var renderer in renderers )
		{
			renderer.RenderType = show ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.ShadowsOnly;
		}
	}

}
