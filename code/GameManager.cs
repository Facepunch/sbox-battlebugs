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

	// Properties
	[Property, Group( "Prefabs" )] public GameObject BoardPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject CellPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject BugSegmentPrefab { get; set; }

	// Networked Variables
	[HostSync] public GameState State { get; set; }
	[HostSync] public Guid CurrentPlayerId { get; set; }

	// Local Variables
	public List<BoardManager> Boards;
	public BoardManager CurrentPlayer => Boards.FirstOrDefault( x => x.GameObject.Id == CurrentPlayerId );

	protected override void OnAwake()
	{
		Instance = this;
		State = GameState.Waiting;
		Boards = new();
	}

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor ) return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
			StartGame();
		}
	}

	public void OnActive( Connection channel )
	{
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

	void StartGame()
	{
		State = GameState.Waiting;
	}

	void EndGame()
	{
		State = GameState.Results;
	}

	protected override void OnUpdate()
	{
		switch ( State )
		{
			case GameState.Waiting: UpdateWaiting(); break;
			case GameState.Placing: UpdatePlacing(); break;
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
				State = GameState.Placing;
			}
		}
	}

	void UpdatePlacing()
	{
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

	public void CreateBug( List<CellComponent> cells )
	{
		var bug = BoardManager.Local.BugInventory.FirstOrDefault( x => x.Bug.SegmentCount == cells.Count );
		if ( bug.Count <= 0 ) return;

		var rotation = new Angles( 0, 0, 0 );
		if ( cells.Count > 1 ) rotation = Rotation.LookAt( cells[1].Transform.Position - cells[0].Transform.Position, Vector3.Up );

		for ( int i = 0; i < cells.Count; i++ )
		{
			if ( i > 0 ) rotation = Rotation.LookAt( cells[i].Transform.Position - cells[i - 1].Transform.Position, Vector3.Up );
			var segment = BugSegmentPrefab.Clone();
			segment.Transform.Position = cells[i].Transform.Position;
			segment.Transform.Rotation = rotation;
			segment.SetParent( cells[i].GameObject );
			var component = segment.Components.Get<BugSegment>();
			component.Init( bug.Bug.Color, cells.Count, i * 0.05f );

			cells[i].IsOccupied = true;
		}

		int bugIndex = BoardManager.Local.BugInventory.IndexOf( bug );
		bug.Count--;
		BoardManager.Local.BugInventory[bugIndex] = bug;
	}

}
