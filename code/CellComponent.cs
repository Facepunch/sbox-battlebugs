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
	public bool IsOccupied { get; set; } = false;

	bool IsOdd = false;
	Color BaseColor => IsOdd ? Color.White : Color.Gray;

	protected override void OnStart()
	{
		int index = GameObject.Parent.Children.IndexOf( GameObject );
		index += (index - 1) / 10;
		IsOdd = index % 2 == 0;
		UpdateHighlight();
	}

	public void Init( BoardManager board, Vector2 position )
	{
		Board = board;
		Position = position;
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

		if ( IsSelected ) return;
		UpdateHighlight();
	}

	public void MouseExit()
	{
		IsHovering = false;

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

	void UpdateHighlight()
	{
		if ( IsSelected )
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

}
