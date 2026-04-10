using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Battlebugs;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

	[RequireComponent] PlacementInput _placementInput { get; set; }
	[RequireComponent] AttackingInput _attackingInput { get; set; }
	[RequireComponent] InspectInput _inspectInput { get; set; }

	// Properties
	[Property, Group( "Prefabs" )] public GameObject BoardPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject CellPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject DamageNumberPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject CoinPrefab { get; set; }

	// Networked Variables
	[Sync( SyncFlags.FromHost )] public GameState State { get; set; }
	[Sync( SyncFlags.FromHost )] public Guid CurrentPlayerId { get; set; }
	[Sync( SyncFlags.FromHost )] public bool IsFiring { get; set; }
	[Sync( SyncFlags.FromHost )] public TimeSince TimeSinceTurnStart { get; set; }

	public static bool CpuMode { get; set; }

	// Local Variables
	public List<BoardManager> Boards;
	public BoardManager CurrentPlayer => Boards.FirstOrDefault( x => x.IsValid() && x.Network.OwnerId == CurrentPlayerId );
	Vector3 LastPebblePosition;
	TimeSince TimeSincePebbleToss;

	protected override void OnAwake()
	{
		Instance = this;
		State = GameState.Waiting;
		Boards = new();
	}

	protected override async Task OnLoad()
	{
		if ( Networking.IsActive ) return;

		LoadingScreen.Title = CpuMode ? "Starting Bot Match" : "Creating Lobby";
		await Task.DelayRealtimeSeconds( 0.1f );

		// CPU mode still needs a networking session for NetworkSpawn, IsHost, and RPCs to function.
		// Make it private with max 1 player so nobody else can join.
		var config = new LobbyConfig();
		if ( CpuMode )
		{
			config.MaxPlayers = 1;
			config.Privacy = LobbyPrivacy.Private;
		}
		Networking.CreateLobby( config );
	}

	protected override void OnStart()
	{
		// This is really just for late-joiners
		Boards = Scene.GetAllComponents<BoardManager>().ToList();
	}

	public void OnActive( Connection channel )
	{
		// TODO: Create a spectator pawn or something
		if ( Boards.Count >= 2 ) return;

		CreateBoard( channel );

		// In CPU mode, also create the CPU board now that the lobby is active.
		// Must happen here (not OnStart) because NetworkSpawn requires an active lobby.
		if ( CpuMode )
		{
			CreateBoard( null );
		}
	}

	public void OnDisconnected( Connection channel )
	{
		if ( !Networking.IsHost ) return;
		if ( CpuMode ) return;

		Log.Info( $"Player '{channel.DisplayName}' disconnected" );
		EndGame();
	}

	void CreateBoard( Connection channel )
	{
		var currentBoardCount = Scene.GetAllComponents<BoardManager>().Count();
		var client = BoardPrefab.Clone( new CloneConfig()
		{
			Transform = new Transform( new Vector3( currentBoardCount * 1000f, 0, 2f ), new Angles( 0, currentBoardCount == 0 ? 0 : 180, 0 ) ),
			Name = channel?.DisplayName ?? "CPU"
		} );
		client.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
		client.NetworkSpawn( channel );

		Boards = Scene.GetAllComponents<BoardManager>().ToList();
	}

	[Rpc.Broadcast]
	void StartGame()
	{
		Sound.Play( "player-join" );

		if ( Networking.IsHost )
		{
			State = GameState.Placing;
		}

		Boards = Scene.GetAllComponents<BoardManager>().ToList();
	}

	[Rpc.Broadcast]
	void StartPlaying()
	{
		if ( Networking.IsHost )
		{
			State = GameState.Playing;
			StartTurn();

			foreach ( var board in Boards )
			{
				board.SaveBugReferences();
			}
		}

		foreach ( var segment in Scene.GetAllComponents<BugSegment>() )
		{
			segment.SetAlpha( (segment.Network.OwnerId == Connection.Local.Id) ? 0.5f : 0f );
		}
	}

	void StartTurn()
	{
		TimeSinceTurnStart = 0;
		CurrentPlayerId = Boards.FirstOrDefault( x => x.Network.OwnerId != CurrentPlayerId ).Network.OwnerId;
		IsFiring = true;
	}

	[Rpc.Broadcast]
	void EndGame()
	{
		if ( Networking.IsHost )
		{
			State = GameState.Results;
		}

		if ( !(BoardManager.Local?.IsValid() ?? false) ) return;
		var didWin = BoardManager.Local.GetScorePercent() > 0.5f;
		Sandbox.Services.Stats.Increment( "games_played", 1 );
		if ( didWin ) Sandbox.Services.Stats.Increment( "games_won", 1 );
		else Sandbox.Services.Stats.Increment( "games_lost", 1 );
	}

	protected override void OnUpdate()
	{
		switch ( State )
		{
			case GameState.Waiting: UpdateWaiting(); break;
			case GameState.Placing: UpdatePlacing(); break;
			case GameState.Playing: UpdateGame(); break;
			case GameState.Results: UpdateResults(); break;
		}
		InspectInput.Instance.Enabled = State == GameState.Playing;
	}

	void UpdateWaiting()
	{
		if ( BoardManager.Local is not null )
		{
			UpdateCamera( BoardManager.Local );
		}

		if ( Networking.IsHost )
		{
			if ( Boards.Count > 1 )
			{
				StartGame();
			}
		}
	}

	void UpdatePlacing()
	{
		PlacementInput.Instance.Enabled = true;

		if ( BoardManager.Local is not null )
		{
			UpdateCamera( BoardManager.Local );
		}

		if ( Networking.IsHost )
		{
			if ( !Boards.Any( x => !x.IsReady ) )
			{
				StartPlaying();
			}
		}
	}

	void UpdateGame()
	{
		var currentPlayer = CurrentPlayer;
		PlacementInput.Instance.Enabled = false;
		AttackingInput.Instance.Enabled = IsFiring && (currentPlayer == BoardManager.Local);

		if ( currentPlayer is not null )
		{
			var healthPercent = currentPlayer.GetHealthPercent();
			if ( healthPercent == 0 )
			{
				EndGame();
			}

			var otherPlayer = Boards.FirstOrDefault( x => x.IsValid() && x.Network.OwnerId != CurrentPlayerId );
			if ( otherPlayer is null ) return;
			if ( IsFiring )
			{
				UpdateCamera( otherPlayer );
				LastPebblePosition = Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * 1000f;

				if ( TimeSinceTurnStart >= 15f )
				{
					currentPlayer.AttackRandomly();
				}
			}
			else
			{
				var pebbles = Scene.GetAllComponents<PebbleComponent>();
				var pebble = pebbles.Where( x => x.TimeSinceCreated > 0.6f ).FirstOrDefault();
				if ( pebble is not null )
				{
					LastPebblePosition = pebble.WorldPosition;
				}
				if ( pebbles.Count() > 0 ) TimeSincePebbleToss = 0;
				UpdateCamera( otherPlayer, LastPebblePosition );

				if ( pebbles.Count() == 0 && TimeSincePebbleToss > 3f )
				{
					StartTurn();
				}
			}
		}
	}

	void UpdateResults()
	{

	}

	void UpdateCamera( BoardManager board )
	{
		Scene.Camera.WorldPosition = Scene.Camera.WorldPosition.LerpTo( board.CameraPosition.WorldPosition, Time.Delta * 5f );
		Scene.Camera.WorldRotation = Rotation.Slerp( Scene.Camera.WorldRotation, board.CameraPosition.WorldRotation, Time.Delta * 5f );
	}

	void UpdateCamera( BoardManager board, Vector3 lookAt )
	{
		Scene.Camera.WorldPosition = Scene.Camera.WorldPosition.LerpTo( board.CameraPosition.WorldPosition, Time.Delta * 5f );

		var rotation = Rotation.LookAt( lookAt - Scene.Camera.WorldPosition, Vector3.Up );
		Scene.Camera.WorldRotation = Rotation.Slerp( Scene.Camera.WorldRotation, rotation, Time.Delta * 5f );
	}

	public void CreateBug( BoardManager board, List<PlacementInput.PlacementData> cells, bool isCpu = false )
	{
		// Use the board's own inventory rather than BoardManager.Local, since the
		// local board may not be initialised yet when the CPU places its bugs.
		var bug = board.BugInventory.FirstOrDefault( x => x.Key.SegmentCount == cells.Count );
		if ( bug.Value <= 0 ) return;
		var bugId = Guid.NewGuid();

		for ( int i = 0; i < cells.Count; i++ )
		{
			var segment = cells[i].Prefab.Clone( new Transform(
				cells[i].Cell.WorldPosition,
				cells[i].Rotation
			) );
			segment.Name = bugId.ToString();
			var component = segment.Components.Get<BugSegment>();
			component.Init( bug.Key, i );
			component.Cell = cells[i].Cell;
			segment.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
			segment.NetworkSpawn( isCpu ? null : Connection.Local );

			cells[i].Cell.IsOccupied = true;
		}

		board.BugInventory[bug.Key]--;
	}

	public void SpawnCoins( Vector3 position, int amount = 1 )
	{
		for ( int i = 0; i < amount; i++ )
		{
			CoinPrefab.Clone( position + Vector3.Up * 2f );
		}
	}

	[Rpc.Owner]
	public void BroadcastFire( Guid boardId, string weaponPath, Vector3 position )
	{
		if ( !CpuMode && Rpc.CallerId != CurrentPlayerId ) return;
		if ( IsFiring == false ) return;

		var weapon = ResourceLibrary.Get<WeaponResource>( weaponPath );
		if ( weapon is null ) return;

		var board = Boards.FirstOrDefault( x => x.Id != boardId );
		if ( board is null ) return;

		int count = (int)MathF.Round( weapon.AmountFired.GetValue() );

		for ( int i = 0; i < count; i++ )
		{
			var offset = Vector3.Random.WithZ( 0 ) * weapon.Spray;
			var pos = board.CameraPosition.WorldPosition.WithZ( 32f ) + (board.WorldRotation.Forward * 200f) + offset;
			var target = position + offset;
			var pebbleObj = weapon.Prefab.Clone( pos );
			var pebble = pebbleObj.Components.Get<PebbleComponent>();
			pebble.Damage = weapon.Damage.GetValue();
			pebble.LaunchAt( target );
			pebbleObj.NetworkSpawn( null );
		}
		BroadcastFireSound();
		IsFiring = false;
	}

	[Rpc.Broadcast]
	void BroadcastFireSound()
	{
		Sound.Play( "fling-rocks" );
	}

	[Rpc.Broadcast]
	public void BroadcastDamageNumber( Vector3 position, float damage )
	{
		if ( DamageNumberPrefab is not null )
		{
			var damageNumber = DamageNumberPrefab.Clone( position );
			var text = damageNumber.Components.Get<TextRenderer>();
			text.Text = "-" + damage.ToString();
			text.Color = Color.Red;
		}
	}

	[Rpc.Broadcast]
	public void SendChatMessage( string message )
	{
		var playerHud = PlayerHud.Instances.FirstOrDefault( x => x.Board.Network.OwnerId == Rpc.CallerId );
		if ( playerHud is null ) return;

		playerHud.AddChatMessage( message );
	}

}
