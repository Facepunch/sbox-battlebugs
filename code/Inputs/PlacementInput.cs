using Sandbox;

namespace Battlebugs;

public sealed class PlacementInput : Component
{
	public static PlacementInput Instance { get; private set; }

	public CellComponent HighlightedCell { get; set; } = null;
	public List<CellComponent> SelectedCells { get; set; } = new();
	public bool IsSelecting { get; private set; } = false;
	public Bug AttemptingToPlace { get; private set; } = null;

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
					bug.Clear();
				}
			}
		}

		if ( Input.Released( "Attack1" ) )
		{
			HighlightedCell?.MouseReleased();

			IsSelecting = false;

			if ( SelectedCells.Count > 1 )
			{
				Sound.Play( "ui-select-complete" );
				GameManager.Instance.CreateBug( SelectedCells );
			}
			else if ( SelectedCells.Count == 1 )
			{
				Sound.Play( "ui-select-bug" );
			}
			DeselectAll();
		}
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
}
