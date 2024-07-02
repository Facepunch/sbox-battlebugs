using System;
using Sandbox;

namespace Battlebugs;

public sealed class CellComponent : Component
{
	[Property] ModelRenderer Renderer { get; set; }

	public BoardManager Board { get; set; }
	public Vector2 Position { get; set; }
	public bool IsHovering { get; private set; } = false;
	public bool IsSelected { get; set; } = false;

	[Sync] public bool WasOccupied { get; set; } = false;
	[Sync] public bool IsOccupied { get; set; } = false;
	[Sync] bool IsHit { get; set; } = false;
	[Sync] bool IsOdd { get; set; } = false;
	Color BaseColor => IsOdd ? Color.White : Color.Gray;

	protected override void OnStart()
	{
		Transform.Rotation = new Angles( 0, Random.Shared.Int( 0, 3 ) * 90f + Random.Shared.Float( -3f, 3f ), 0 );
		UpdateHighlight();
	}

	public void Init( BoardManager board, Vector2 position, int index )
	{
		Board = board;
		Position = position;
		IsOdd = index % 2 == 0;
	}

	public void MousePressed()
	{

	}

	public void MouseReleased()
	{

	}

	public void MouseEnter()
	{
		IsHovering = true;

		var bugs = Scene.GetAllComponents<BugSegment>();
		var cellBug = bugs.FirstOrDefault( x => x.Cell == this );
		if ( cellBug is not null )
		{
			bugs = bugs.Where( x => x.GameObject.Name == cellBug.GameObject.Name ).ToList();
			foreach ( var bug in bugs )
			{
				bug.AddHighlight( Color.Red );
			}
		}

		if ( IsSelected ) return;
		UpdateHighlight();
	}

	public void MouseExit()
	{
		IsHovering = false;

		var bugs = Scene.GetAllComponents<BugSegment>();
		var cellBug = bugs.FirstOrDefault( x => x.Cell == this );
		if ( cellBug is not null )
		{
			bugs = bugs.Where( x => x.GameObject.Name == cellBug.GameObject.Name ).ToList();
			foreach ( var bug in bugs )
			{
				bug.RemoveHighlight();
			}
		}

		if ( IsSelected ) return;
		UpdateHighlight();
	}

	public void Select()
	{
		IsSelected = true;
		PlacementInput.Instance.SelectedCells.Add( this );

		Sound.Play( "ui-select" );

		foreach ( var cell in PlacementInput.Instance.SelectedCells )
		{
			cell.UpdateHighlight();
		}
	}

	public void Deselect( bool remove = true )
	{
		IsSelected = false;

		if ( remove )
		{
			PlacementInput.Instance.SelectedCells.Remove( this );
			Sound.Play( "ui-select-bug" );
			UpdateHighlight();
		}
		foreach ( var cell in PlacementInput.Instance.SelectedCells )
		{
			cell.UpdateHighlight();
		}
	}

	public bool IsAdjacent( CellComponent other )
	{
		// Above/below
		if ( Position.x == other.Position.x && MathF.Abs( Position.y - other.Position.y ) == 1 )
			return true;

		// Left/right
		if ( Position.y == other.Position.y && MathF.Abs( Position.x - other.Position.x ) == 1 )
			return true;

		return false;
	}

	public List<CellComponent> GetNeighbors()
	{
		var neighbors = new List<CellComponent>();

		var cells = Scene.GetAllComponents<CellComponent>();
		foreach ( var cell in cells )
		{
			if ( cell == this ) continue;
			if ( IsAdjacent( cell ) )
			{
				neighbors.Add( cell );
			}
		}

		return neighbors;
	}

	void UpdateHighlight()
	{
		if ( IsHit )
		{
			Renderer.Tint = Color.Lerp( BaseColor, IsOccupied ? Color.Orange : (WasOccupied ? Color.Green : Color.Red), 0.5f );
		}
		else if ( IsSelected )
		{
			var color = Color.Yellow;
			var placing = PlacementInput.Instance.AttemptingToPlace;
			if ( placing is not null )
			{
				if ( BoardManager.Local.BugInventory.FirstOrDefault( x => x.Key.ResourceId == placing.ResourceId ).Value <= 0 ) color = Color.Yellow;
				else color = placing.Color;
			}
			Renderer.Tint = Color.Lerp( BaseColor, color, 0.8f );
		}
		else if ( IsHovering )
		{
			Renderer.Tint = IsOccupied ? Color.Lerp( BaseColor, Color.Red, 0.5f ) : Color.Lerp( BaseColor, Color.Yellow, 0.5f );
		}
		else
		{
			Renderer.Tint = BaseColor;
		}
	}

	[Broadcast]
	public void BroadcastHit()
	{
		if ( IsProxy ) return;

		IsHit = true;
		BroadcastUpdateHighlight();
	}

	[Broadcast]
	public void BroadcastClear()
	{
		if ( IsProxy ) return;

		IsHit = true;
		WasOccupied = IsOccupied;
		IsOccupied = false;
		BroadcastUpdateHighlight();
	}

	[Broadcast]
	public void BroadcastUpdateHighlight()
	{
		UpdateHighlight();
	}

}
