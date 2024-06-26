using Sandbox;

namespace Battlebugs;

public sealed class GameInput : Component
{
	public static GameInput Instance { get; private set; }

	public CellComponent HighlightedCell { get; set; } = null;
	public List<CellComponent> SelectedCells { get; set; } = new();

	public bool CanSelect { get; set; } = true;
	public bool IsSelecting { get; private set; } = false;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnUpdate()
	{
		if ( !CanSelect )
		{
			if ( SelectedCells.Count > 0 )
			{
				DeselectAll();
			}
			if ( HighlightedCell.IsValid() )
			{
				HighlightedCell.MouseExit();
			}
			return;
		}
		var tr = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 8000f )
			.WithoutTags( "bug" )
			.Run();

		if ( tr.Hit )
		{
			var newCell = tr.GameObject.Components.Get<CellComponent>();
			if ( newCell != HighlightedCell )
			{
				HighlightedCell?.MouseExit();
				HighlightedCell = newCell;
				HighlightedCell?.MouseEnter();

				if ( IsSelecting )
				{
					if ( SelectedCells.Count > 1 && HighlightedCell == SelectedCells.ElementAt( SelectedCells.Count - 2 ) )
					{
						SelectedCells.LastOrDefault()?.Deselect();
						SelectedCells.RemoveAt( SelectedCells.Count - 1 );
					}

					if ( HighlightedCell is not null && !HighlightedCell.IsOccupied && !SelectedCells.Contains( HighlightedCell ) && (SelectedCells.Count == 0 || SelectedCells.LastOrDefault().IsAdjacent( HighlightedCell )) && SelectedCells.Count < (BoardManager.Local?.MaxPlaceableSegments ?? 0) )
					{
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
			HighlightedCell?.Select();
		}

		if ( Input.Released( "Attack1" ) )
		{
			HighlightedCell?.MouseReleased();

			IsSelecting = false;

			if ( SelectedCells.Count > 1 )
			{
				GameManager.Instance.CreateBug( SelectedCells );
			}
			DeselectAll();
		}
	}

	void DeselectAll()
	{
		foreach ( var cell in SelectedCells )
		{
			cell.Deselect();
		}
		SelectedCells.Clear();
	}
}
