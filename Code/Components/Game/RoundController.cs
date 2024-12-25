using Sandbox;
using System;
using System.Runtime.CompilerServices;

public sealed class RoundController : Component, Component.INetworkListener
{
	public static RoundController Instance { get; private set; }

	[Property] public Dictionary<Player, string> RoleList { get; set; } = new Dictionary<Player, string>();
	[Property] public List<Player> PlayerList { get; set; } = new List<Player>();
	[Property, Group( "Prefabs" )] public GameObject RemoverDestroyParticle { get; set; }
	[Property, Group( "Prefabs" )] public GameObject DecalObject { get; set; }

	[Property] public List<Player> SpectatorList { get; set; } = new List<Player>();

	[Rpc.Broadcast]
	public void BroadcastAddTag( Guid objectId, string tag )
	{
		Scene.Directory.FindByGuid( objectId )?.Tags?.Add( tag );
	}

	[Rpc.Broadcast]
	public void BroadcastRemoveTag( Guid objectId, string tag )
	{
		Scene.Directory.FindByGuid( objectId )?.Tags?.Remove( tag );
	}

	[Rpc.Broadcast]
	public void BroadcastSetTag( Guid objectId, string tag, bool state )
	{
		var obj = Scene.Directory.FindByGuid( objectId );
		if ( obj.IsValid() )
		{
			if ( state )
			{
				obj.Tags.Add( tag );
			}
			else
			{
				obj.Tags.Remove( tag );
			}
		}
	}

	[Rpc.Broadcast]
	public void SpawnDecal( string decalPath, Vector3 position, Vector3 normal, Guid parentId = default )
	{
		if ( string.IsNullOrWhiteSpace( decalPath ) ) decalPath = "decals/bullethole.decal";
		position += normal;
		var decalObject = DecalObject.Clone( position, Rotation.LookAt( -normal ) );
		var parent = Scene.Directory.FindByGuid( parentId );
		if ( parent.IsValid() ) decalObject.SetParent( parent );
		decalObject.Name = decalPath;
		if ( !string.IsNullOrWhiteSpace( decalPath ) )
		{
			var renderer = decalObject.Components.Get<DecalRenderer>();
			var decal = ResourceLibrary.Get<DecalDefinition>( decalPath );
			if ( decal is not null )
			{
				var entry = decal.Decals.OrderBy( x => Random.Shared.Float() ).FirstOrDefault();
				renderer.Material = entry.Material;
				var width = entry.Width.GetValue();
				var height = entry.Height.GetValue();
				renderer.Size = new Vector3(
					width,
					entry.KeepAspect ? width : height,
					entry.Depth.GetValue()
				);
				var fadeAfter = decalObject.Components.GetOrCreate<FadeAfter>();
				fadeAfter.Time = entry.FadeTime.GetValue();
				fadeAfter.FadeTime = entry.FadeDuration.GetValue();
			}
		}
	}

	[Rpc.Broadcast]
	public void BroadcastDestroyObject( Guid objectId )
	{
		var obj = Scene.Directory.FindByGuid( objectId );
		if ( obj.IsValid() )
		{
			obj.Destroy();
		}
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		var lobbyController = GameObject.Components.Get<LobbyController>();
		PlayerList = new List<Player>( lobbyController.PlayerList );

		//DecalObject = ResourceLibrary.Get<GameObject>( "Assets/prefabs/Decal.prefab" );

		Random random = new Random();

		int murderIndex = random.Next( PlayerList.Count );

		RoleList.Add( PlayerList[murderIndex], "Murder" );

		int copIndex = random.Next( PlayerList.Count );

		while ( copIndex == murderIndex )
		{
			copIndex = random.Next( PlayerList.Count );
		}
		string role;
		RoleList.Add( PlayerList[copIndex], "Cop" );

		foreach ( Player player in PlayerList )
		{
			player.Inventory.Enabled = true;
			if ( !RoleList.ContainsKey( player ) )
			{
				RoleList[player] = "Bystander";
			}
			if ( RoleList.TryGetValue( player, out role ) && role == "Murder" )
			{
			}

			else if ( RoleList.TryGetValue( player, out role ) && role == "Cop" )
			{
			}
			player.Role = role;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var invalidKeys = RoleList.Keys.Where( player => player == null || !player.IsValid() ).ToList();
		foreach ( var invalidKey in invalidKeys )
		{
			RoleList.Remove( invalidKey );
		}

		foreach ( Player player in Game.ActiveScene?.GetAll<Player>() )
		{
			if ( !PlayerList.Contains( player ) && !SpectatorList.Contains( player ))
			{
				SpectatorList.Add( player );
			}
		}

		foreach ( Player player in RoleList.Keys )
		{
			if ( !PlayerList.Contains( player ) && RoleList.ContainsKey(player)) 
			{
				//RoleList.Remove( player );
			}
		}



		if ( !RoleList.ContainsValue( "Murder" ) )
		{
			Log.Info( "Fin de partie, les innocents ont gagnés" );
			Components.Get<LobbyController>().Restart();
			Components.Get<RoundController>().Destroy();
		}

		if (RoleList.Count == 1 && RoleList.ContainsValue( "Murder" ) )
		{
			Log.Info( "Fin de partie, le Murder a gagné" );
			Components.Get<LobbyController>().Restart();
			Components.Get<RoundController>().Destroy();
		}
	}
}
