using Sandbox;

namespace Battlebugs;

public sealed class BoardManager : Component
{
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

	[Property] public int GridSize { get; set; } = 64;
	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 10;

	[Property] public List<BugItem> BugInventory = new();
	public int MaxPlaceableSegments => BugInventory.Where( x => x.Count > 0 ).OrderBy( x => x.Bug.SegmentCount ).LastOrDefault().Bug?.SegmentCount ?? 0;

	[Property, Group( "References" )] public GameObject CameraPosition { get; set; }

	protected override void OnStart()
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

	protected override void OnUpdate()
	{

	}

	public struct BugItem
	{
		public Bug Bug { get; set; }
		public int Count { get; set; }

		public BugItem( Bug bug, int count )
		{
			Bug = bug;
			Count = count;
		}
	}

}
