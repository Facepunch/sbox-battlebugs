using System;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Network;

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
	[Sync( SyncFlags.FromHost )] public bool CpuMode { get; set; }

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
		if ( Scene.IsEditor ) return;
		if ( Networking.IsActive ) return;
		CpuMode = MainMenu.IsCpuGame;
		if ( CpuMode ) return;

		LoadingScreen.Title = "Creating Lobby";
		await Task.DelayRealtimeSeconds( 0.1f );
		Networking.CreateLobby( new LobbyConfig() );
	}

	protected override void OnStart()
	{
		// This is really just for late-joiners
		Boards = Scene.GetAllComponents<BoardManager>().ToList();

		// Initialize both boards right away if in CPU mode
		if ( CpuMode && !IsProxy )
		{
			CreateBoard( Connection.Local );
			CreateBoard( null );
		}
	}

	public void OnActive( Connection channel )
	{
		// TODO: Create a spectator pawn or something
		if ( Boards.Count >= 2 ) return;

		CreateBoard( channel );
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
			if ( Boards.Any( x => x.Network.OwnerId == Guid.Empty ) && !CpuMode )
			{
				EndGame();
			}
			else if ( !Boards.Any( x => !x.IsReady ) )
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
		var bug = BoardManager.Local.BugInventory.FirstOrDefault( x => x.Key.SegmentCount == cells.Count );
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
	public void BroadcastFire( Guid boardId, int weaponId, Vector3 position )
	{
		if ( !CpuMode && Rpc.CallerId != CurrentPlayerId ) return;
		if ( IsFiring == false ) return;

		var weapon = ResourceLibrary.Get<WeaponResource>( weaponId );
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