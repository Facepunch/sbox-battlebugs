using Sandbox;

namespace Battlebugs;

public sealed class BoardManager : Component
{
	// Static Variables
	public static BoardManager Local
	{
		get
		{
			if ( !_local.IsValid() )
			{
				_local = Game.ActiveScene.GetAllComponents<BoardManager>().FirstOrDefault( x => x.Network.IsOwner );
			}
			return _local;
		}
	}
	private static BoardManager _local;

	// Properties
	[Property] public int GridSize { get; set; } = 64;
	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 10;
	[Property, Group( "References" )] public GameObject CameraPosition { get; set; }

	// Networked Variables
	[Sync] public bool IsReady { get; set; } = false;

	// Public Variables
	public Dictionary<Bug, int> BugInventory = new();
	public int MaxPlaceableSegments => BugInventory.Where( x => x.Value > 0 ).OrderBy( x => x.Key.SegmentCount ).LastOrDefault().Key?.SegmentCount ?? 0;

	protected override void OnStart()
	{
		InitBoard();
		ResetInventory();
	}

	void InitBoard()
	{
		Vector3 startingPosition = Transform.Position + new Vector3( -(Width * GridSize) / 2f + GridSize / 2f, -(Height * GridSize) / 2f + GridSize / 2f, 0 );
		for ( int x = 0; x < Width; x++ )
		{
			for ( int y = 0; y < Height; y++ )
			{
				var cellObj = GameManager.Instance.CellPrefab.Clone( startingPosition + new Vector3( x * GridSize, y * GridSize, 0 ) );
				var cell = cellObj.Components.Get<CellComponent>();
				cell.Init( this, new Vector2( x, y ) );
				cellObj.SetParent( GameObject );
			}
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var size = new Vector3( Width * GridSize, Height * GridSize, 0 );
		var bounds = new BBox( size / 2f, -size / 2f );
		Gizmo.Draw.LineBBox( bounds );
	}

	public void ClearAllBugs()
	{
		var segments = Scene.GetAllComponents<BugSegment>();
		foreach ( var segment in segments )
		{
			if ( segment.Network.OwnerId != Network.OwnerId ) continue;
			segment.Clear();
		}
		ResetInventory();
	}

	public void ToggleReady()
	{
		if ( IsProxy ) return;
		IsReady = !IsReady;
	}

	void ResetInventory()
	{
		var allBugs = ResourceLibrary.GetAll<Bug>();
		BugInventory.Clear();
		foreach ( var bug in allBugs )
		{
			BugInventory[bug] = bug.StartingAmount;
		}
	}

}
