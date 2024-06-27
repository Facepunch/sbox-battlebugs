using System;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Network;

namespace Battlebugs;

public enum GameState
{
	Waiting,
	Placing,
	Playing,
	Results
}

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

	[RequireComponent] PlacementInput _placementInput { get; set; }
	[RequireComponent] AttackingInput _attackingInput { get; set; }

	// Properties
	[Property] public bool IsTestMode { get; set; }
	[Property, Group( "Prefabs" )] public GameObject BoardPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject CellPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject BugSegmentPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject PebblePrefab { get; set; }

	// Networked Variables
	[HostSync] public GameState State { get; set; }
	[HostSync] public Guid CurrentPlayerId { get; set; }
	[HostSync] public bool IsFiring { get; set; }

	// Local Variables
	public List<BoardManager> Boards;
	public BoardManager CurrentPlayer => Boards.FirstOrDefault( x => x.Network.OwnerId == CurrentPlayerId );
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
		if ( IsTestMode ) return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
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

		var currentBoardCount = Scene.GetAllComponents<BoardManager>().Count();
		var client = BoardPrefab.Clone( new CloneConfig()
		{
			Transform = new Transform( new Vector3( currentBoardCount * 1000f, 0, 0 ), new Angles( 0, currentBoardCount == 0 ? 0 : 180, 0 ) ),
			Name = channel.DisplayName
		} );
		client.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
		client.NetworkSpawn( channel );

		Boards = Scene.GetAllComponents<BoardManager>().ToList();
	}

	[Broadcast]
	void StartGame()
	{
		if ( Networking.IsHost )
		{
			State = GameState.Placing;
		}

		Boards = Scene.GetAllComponents<BoardManager>().ToList();
	}

	[Broadcast]
	void StartPlaying()
	{
		if ( Networking.IsHost )
		{
			State = GameState.Playing;
			StartTurn();
		}

		foreach ( var segment in Scene.GetAllComponents<BugSegment>() )
		{
			segment.SetAlpha( segment.IsProxy ? 0f : 0.5f );
		}
	}

	void StartTurn()
	{
		CurrentPlayerId = Boards.FirstOrDefault( x => x.Network.OwnerId != CurrentPlayerId ).Network.OwnerId;
		IsFiring = true;
	}

	void EndGame()
	{
		State = GameState.Results;
	}

	protected override void OnUpdate()
	{
		if ( IsTestMode ) return;

		switch ( State )
		{
			case GameState.Waiting: UpdateWaiting(); break;
			case GameState.Placing: UpdatePlacing(); break;
			case GameState.Playing: UpdateGame(); break;
			case GameState.Results: UpdateResults(); break;
		}
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
			if ( Boards.Any( x => x.Network.OwnerId == Guid.Empty ) )
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
		PlacementInput.Instance.Enabled = false;
		AttackingInput.Instance.Enabled = IsFiring && (CurrentPlayer == BoardManager.Local);

		if ( CurrentPlayer is not null )
		{
			var otherPlayer = Boards.FirstOrDefault( x => x.Network.OwnerId != CurrentPlayerId );
			if ( IsFiring )
			{
				UpdateCamera( otherPlayer );
				LastPebblePosition = Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * 1000f;
			}
			else
			{
				var pebbles = Scene.GetAllComponents<PebbleComponent>();
				var pebble = pebbles.Where( x => x.TimeSinceCreated > 0.6f ).FirstOrDefault();
				if ( pebble is not null )
				{
					LastPebblePosition = pebble.Transform.Position;
				}
				if ( pebbles.Count() > 0 ) TimeSincePebbleToss = 0;
				UpdateCamera( otherPlayer, LastPebblePosition );

				if ( pebbles.Count() == 0 && TimeSincePebbleToss > 2f )
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
		var camTarget = board.CameraPosition.Transform;
		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camTarget.Position, Time.Delta * 5f );
		Scene.Camera.Transform.Rotation = Rotation.Slerp( Scene.Camera.Transform.Rotation, camTarget.Rotation, Time.Delta * 5f );
	}

	void UpdateCamera( BoardManager board, Vector3 lookAt )
	{
		var camTarget = board.CameraPosition.Transform;
		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camTarget.Position, Time.Delta * 5f );

		var rotation = Rotation.LookAt( lookAt - Scene.Camera.Transform.Position, Vector3.Up );
		Scene.Camera.Transform.Rotation = Rotation.Slerp( Scene.Camera.Transform.Rotation, rotation, Time.Delta * 5f );
	}

	public void CreateBug( List<CellComponent> cells )
	{
		var bug = BoardManager.Local.BugInventory.FirstOrDefault( x => x.Key.SegmentCount == cells.Count );
		if ( bug.Value <= 0 ) return;

		var rotation = new Angles( 0, 0, 0 );
		if ( cells.Count > 1 ) rotation = Rotation.LookAt( cells[1].Transform.Position - cells[0].Transform.Position, Vector3.Up );

		var bugId = Guid.NewGuid();

		for ( int i = 0; i < cells.Count; i++ )
		{
			if ( i > 0 ) rotation = Rotation.LookAt( cells[i].Transform.Position - cells[i - 1].Transform.Position, Vector3.Up );
			var segment = BugSegmentPrefab.Clone();
			segment.Name = bugId.ToString();
			segment.Transform.Position = cells[i].Transform.Position;
			segment.Transform.Rotation = rotation;
			var component = segment.Components.Get<BugSegment>();
			component.Init( bug.Key.Color, cells.Count, i * 0.05f );
			component.Cell = cells[i];
			segment.NetworkSpawn();

			cells[i].IsOccupied = true;
		}

		BoardManager.Local.BugInventory[bug.Key] = bug.Value - 1;
	}

	[Authority]
	public void BroadcastFire( Vector3 position )
	{
		if ( Rpc.CallerId != CurrentPlayerId ) return;
		if ( IsFiring == false ) return;

		var board = Boards.FirstOrDefault( x => x.Network.OwnerId != Rpc.CallerId );
		if ( board is null ) return;

		// TODO: Implement firing logic
		var pebbleObj = PebblePrefab.Clone( board.CameraPosition.Transform.Position.WithZ( 32f ) );
		var pebble = pebbleObj.Components.Get<PebbleComponent>();
		pebble.LaunchAt( position );
		pebbleObj.NetworkSpawn( null );

		IsFiring = false;
	}

}
