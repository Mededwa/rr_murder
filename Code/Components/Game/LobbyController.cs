using Sandbox;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;

public sealed class LobbyController : Component, Component.INetworkListener
{
	public static LobbyController Instance { get; private set; }
	[Property] public List<Player> PlayerList { get; set; } = new List<Player>();

	[Property] public int PlayerCount => PlayerList.Count;

	[Property] public int MinPlayerForTinyLobby { get; set; } = 1;
	[Property] public int MinPlayerForBigLobby { get; set; } = 2;

	[Property] public float MinLobbyTimer { get; set; } = 30f;

	[Property] public float BigLobbyTimer { get; set; } = 5f;

	[Property] public float RoundDuration { get; set; }

	public double LobbyTimer { get; set; } = 30f;

	public TimeUntil RoundTimer { get; set; }

	public bool gameHasStarted { get; set; } = false;
	private bool LobbytimerHasStarted { get; set; } = false;

	private bool RoundTimerHasStarted { get; set; } = false;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		PlayerList = Game.ActiveScene?.GetAll<Player>().ToList();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( gameHasStarted ) return;

		if ( !LobbytimerHasStarted && PlayerCount >= MinPlayerForTinyLobby )
		{
			LobbytimerHasStarted = true;
			LobbyTimer = MinLobbyTimer;
			Log.Info( $"Lobby countdown started: {LobbyTimer} seconds" );
		}

		if ( LobbytimerHasStarted )
		{
			if ( PlayerCount < MinPlayerForTinyLobby )
			{
				LobbytimerHasStarted = false;
				Log.Info( "Not enough players, lobby countdown stopped." );
				return;
			}

			if ( PlayerCount >= MinPlayerForBigLobby && LobbyTimer > BigLobbyTimer )
			{
				LobbyTimer = Math.Min( LobbyTimer, BigLobbyTimer );
				Log.Info( $"Player count reached Big Lobby threshold. Timer adjusted to {LobbyTimer} seconds." );
			}

			if ( PlayerCount >= MinPlayerForBigLobby )
			{
				LobbyTimer = Math.Max( 0, LobbyTimer - Time.Delta ); 
			}
			else if ( PlayerCount >= MinPlayerForTinyLobby )
			{
				LobbyTimer = Math.Max( 0, LobbyTimer - Time.Delta );
			}

			Log.Info( $"Time remaining: {LobbyTimer:F2} seconds" );

			if ( LobbyTimer <= 0 )
			{
				LobbytimerHasStarted = false;
				gameHasStarted = true;
				Log.Info( "Game has started!" );
				StartGame();
			}
		}
	}

	public void Restart()
	{
		PlayerList.Clear();
		gameHasStarted = false;
		Log.Info( "Reset de LobbyController" );
	}

	public void StartGame() 
	{
		Log.Info( "Started game" );
		GameObject.AddComponent<RoundController>();
	}
}
