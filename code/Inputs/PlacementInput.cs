using Sandbox;

namespace Battlebugs;

public sealed class PlacementInput : Component
{
	public static PlacementInput Instance { get; private set; }

	public CellComponent HighlightedCell { get; set; } = null;
	public List<CellComponent> SelectedCells { get; set; } = new();
	public bool IsSelecting { get; private set; } = false;
	public BugResource AttemptingToPlace { get; private set; } = null;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		if ( !BoardManager.Local.IsValid() ) return;

		var tr = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 8000f )
			.WithoutTags( "bug" )
			.Run();

		if ( tr.Hit )
		{
			var newCell = tr.GameObject.Components.Get<CellComponent>();
			if ( newCell?.Board != BoardManager.Local ) return;
			if ( newCell != HighlightedCell )
			{
				HighlightedCell?.MouseExit();
				HighlightedCell = newCell;
				HighlightedCell?.MouseEnter();

				if ( IsSelecting )
				{
					if ( SelectedCells.Count > 1 && HighlightedCell == SelectedCells.ElementAt( SelectedCells.Count - 2 ) )
					{
						AttemptingToPlace = SelectedCells.Count > 0 ? SelectedCells.FirstOrDefault().Board.BugInventory.FirstOrDefault( x => x.Key.SegmentCount == SelectedCells.Count - 1 ).Key : null;
						SelectedCells.LastOrDefault()?.Deselect();
					}

					if ( HighlightedCell is not null && !HighlightedCell.IsOccupied && !SelectedCells.Contains( HighlightedCell ) && (SelectedCells.Count == 0 || SelectedCells.LastOrDefault().IsAdjacent( HighlightedCell )) && SelectedCells.Count < (BoardManager.Local?.MaxPlaceableSegments ?? 0) )
					{
						AttemptingToPlace = SelectedCells.Count > 0 ? SelectedCells.FirstOrDefault().Board.BugInventory.FirstOrDefault( x => x.Key.SegmentCount == SelectedCells.Count + 1 ).Key : null;
						HighlightedCell?.Select();
					}
				}
			}
		}
		else
		{
			HighlightedCell?.MouseExit();
			HighlightedCell = null;
		}

		if ( Input.Pressed( "Attack1" ) )
		{
			HighlightedCell?.MousePressed();

			IsSelecting = true;
			DeselectAll();
			if ( HighlightedCell is not null && !HighlightedCell.IsOccupied )
			{
				HighlightedCell?.Select();
			}
		}

		if ( HighlightedCell is not null && Input.Pressed( "Attack2" ) )
		{
			IsSelecting = false;
			DeselectAll();

			var bugs = Scene.GetAllComponents<BugSegment>();
			var cellBug = bugs.FirstOrDefault( x => x.Cell == HighlightedCell );
			if ( cellBug is not null )
			{
				BoardManager.Local.BugInventory[cellBug.Bug]++;
				bugs = bugs.Where( x => x.GameObject.Name == cellBug.GameObject.Name ).ToList();
				foreach ( var bug in bugs )
				{
					bug.Cell.IsOccupied = false;
					bug.Clear();
				}
			}
			BoardManager.Local.IsReady = false;
		}

		if ( Input.Released( "Attack1" ) )
		{
			HighlightedCell?.MouseReleased();

			IsSelecting = false;

			if ( SelectedCells.Count > 1 )
			{
				Sound.Play( "ui-select-complete" );
				GameManager.Instance.CreateBug( BoardManager.Local, GetPlacementData() );
			}
			else if ( SelectedCells.Count == 1 )
			{
				Sound.Play( "ui-select-bug" );
			}
			DeselectAll();
		}
	}

	public List<PlacementData> GetPlacementData( List<CellComponent> cells = null, BugResource bug = null )
	{
		if ( cells is null ) cells = SelectedCells.ToList();
		if ( bug is null ) bug = AttemptingToPlace;
		var data = new List<PlacementData>();

		var rotation = new Angles( 0, 0, 0 );
		if ( cells.Count > 1 ) rotation = Rotation.LookAt( cells[1].Transform.Position - cells[0].Transform.Position, Vector3.Up );

		for ( int i = 0; i < cells.Count; i++ )
		{
			if ( i < cells.Count - 1 ) rotation = Rotation.LookAt( cells[i + 1].Transform.Position - cells[i].Transform.Position, Vector3.Up );
			else if ( i > 0 ) rotation = Rotation.LookAt( cells[i].Transform.Position - cells[i - 1].Transform.Position, Vector3.Up );

			// Get prefab based on rotation/position
			var prefab = bug.BodyPrefab;
			if ( i == 0 ) prefab = bug.TailPrefab;
			else if ( i == cells.Count - 1 ) prefab = bug.HeadPrefab;
			else
			{
				var last = cells[i - 1];
				var next = cells[i + 1];
				var current = cells[i];
				if ( !(last.Position.x == next.Position.x || last.Position.y == next.Position.y) )
				{
					prefab = bug.CornerPrefab;
					rotation = Rotation.LookAt( last.Transform.Position - next.Transform.Position, Vector3.Up );
					var previousRotation = Rotation.LookAt( last.Transform.Position - current.Transform.Position, Vector3.Up );

					// If previous piece is left and next piece is down
					if ( last.Transform.Position.x < current.Transform.Position.x && next.Transform.Position.y > current.Transform.Position.y )
					{
						rotation -= new Angles( 0, 90 + 45, 0 );
					}
					// If rotating left
					else if ( rotation.yaw - previousRotation.Yaw() > 0 )
					{
						rotation -= new Angles( 0, 45 + 90, 0 );
					}
					else
					{
						rotation += new Angles( 0, 45, 0 );
					}
				}
			}

			data.Add( new PlacementData
			{
				Cell = cells[i],
				Rotation = rotation,
				Prefab = prefab
			} );
		}

		return data;
	}

	protected override void OnDisabled()
	{
		if ( SelectedCells.Count > 0 )
		{
			DeselectAll();
		}
		if ( HighlightedCell.IsValid() )
		{
			HighlightedCell.MouseExit();
		}
	}

	void DeselectAll()
	{
		foreach ( var cell in SelectedCells )
		{
			cell.Deselect( false );
		}
		SelectedCells.Clear();
	}

	public class PlacementData
	{
		public CellComponent Cell { get; set; }
		public Rotation Rotation { get; set; }
		public GameObject Prefab { get; set; }
	}
}
