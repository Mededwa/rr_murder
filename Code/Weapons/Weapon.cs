using Sandbox;
using Sandbox.Citizen;
using Scenebox;
using System;
public class Weapon : Component
{
	[Property] public WeaponResource Resource { get; set; }

	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] protected CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;

	[Property] public float Damage { get; set; } = 20f;
	[Property] protected float Force { get; set; } = 20f;


	[Property, Group( "Sounds" )] public SoundEvent EquipSound { get; set; }

	[Property, Group( "References" )] public GameObject Muzzle { get; set; }

	public Player Player => Components.Get<Player>( FindMode.EverythingInAncestors );

	[Sync]
	public bool IsEquipped
	{
		get => _isEquipped;
		set
		{
			_isEquipped = value;

			if ( ModelRenderer.IsValid() )
				ModelRenderer.Enabled = _isEquipped;
		}
	}
	bool _isEquipped;

	public int Ammo { get; set; } = 0;
	public int AmmoReserve { get; set; } = 0;

	public ViewModel ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value;

			if ( _viewModel.IsValid() )
			{
				_viewModel.Weapon = this;
			}
		}
	}
	private ViewModel _viewModel;

	protected override void OnStart()
	{
		if ( IsEquipped )
			OnEquip();
		else
			OnUnequip();
	}

	[Rpc.Owner]
	public void Equip()
	{
		if ( IsEquipped ) return;

		if ( Player.IsValid() )
		{
			var weapons = Player.Inventory.Weapons.ToList();

			foreach ( var weapon in weapons )
			{
				weapon.Unequip();
			}
		}

		IsEquipped = true;
		Player.CurrentHoldType = HoldType;
		GameObject.Enabled = true;

		OnEquip();
	}

	[Rpc.Owner]
	public void Unequip()
	{
		if ( !IsEquipped ) return;

		IsEquipped = false;
		GameObject.Enabled = false;

		OnUnequip();
	}

	public void ClearViewModel()
	{
		if ( ViewModel.IsValid() )
		{
			ViewModel.GameObject.Destroy();
		}
	}

	public void CreateViewModel( bool playEquipEffects = true )
	{
		if ( !Player.IsValid() ) return;

		ClearViewModel();
		UpdateRenderMode();

		if ( Resource.ViewModelPrefab.IsValid() )
		{
			var viewModelGameObject = Resource.ViewModelPrefab.Clone( new CloneConfig()
			{
				Transform = new(),
				StartEnabled = true
			} );

			var viewModelComponent = viewModelGameObject.Components.Get<ViewModel>();
			ViewModel = viewModelComponent;
			viewModelGameObject.BreakFromPrefab();
		}

		if ( !playEquipEffects ) return;
		if ( EquipSound is null ) return;

		var sound = Sound.Play( EquipSound, WorldPosition );
		if ( !sound.IsValid() ) return;

		sound.ListenLocal = !IsProxy;
	}

	protected void UpdateRenderMode()
	{
		foreach ( var renderer in Components.GetAll<ModelRenderer>() )
		{
			renderer.RenderType = IsProxy ? Sandbox.ModelRenderer.ShadowRenderType.On : Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
		}
	}

	protected virtual void OnEquip()
	{
		CreateViewModel();

		BroadcastSetVisible( true );
	}

	protected virtual void OnUnequip()
	{
		BroadcastSetVisible( false );

		ClearViewModel();
	}

	protected override void OnDestroy()
	{
		ClearViewModel();
	}

	protected virtual void Attack( SceneTraceResult tr )
	{
		if ( tr.Hit )
		{
			Sound.Play( tr.Surface.Sounds.ImpactHard, tr.HitPosition );
			string decal = "";
			var decals = tr.Surface.ImpactEffects.BulletDecal;
			if ( (decals?.Count() ?? 0) > 0 )
				decal = decals.OrderBy( x => Random.Shared.Float() ).FirstOrDefault();

			if ( tr.GameObject?.Components?.TryGet<PropHelper>( out var propHelper ) ?? false )
			{
				propHelper.BroadcastAddForce( tr.Body.GroupIndex, tr.Direction * 80000f * (Force / 10f) );
				propHelper.Damage( Damage );
			}

			if ( tr.GameObject?.Root?.Components?.TryGet<Player>( out var player ) ?? false )
			{
				player.Damage( Damage, Resource.ResourceId );
			}

			RoundController.Instance.SpawnDecal( decal, tr.HitPosition, tr.Normal, tr.GameObject?.Id ?? Guid.Empty );
		}
	}

	[Rpc.Broadcast]
	void BroadcastSetVisible( bool visible )
	{
		if ( ModelRenderer.IsValid() ) ModelRenderer.Enabled = visible;
	}


	private bool IsNearby( Vector3 position )
	{
		return position.DistanceSquared( Scene.Camera.WorldPosition ) < 4194304f;
	}
}
