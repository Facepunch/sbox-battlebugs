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

	public void Init( BoardManager board, Vector2 position )
	{
		Board = board;
		Position = position;

		int index = (int)Position.x + (int)Position.y * (Board.Width + 1);
		IsOdd = index % 2 == 0;
		Renderer.Tint = IsOdd ? Color.White : Color.Gray;
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
		GameInput.Instance.SelectedCells.Add( this );

		foreach ( var cell in GameInput.Instance.SelectedCells )
		{
			cell.UpdateHighlight();
		}
	}

	public void Deselect( bool remove = true )
	{
		IsSelected = false;

		if ( remove )
		{
			GameInput.Instance.SelectedCells.Remove( this );
			UpdateHighlight();
		}
		foreach ( var cell in GameInput.Instance.SelectedCells )
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
			if ( GameInput.Instance.AttemptingToPlace is not null ) color = GameInput.Instance.AttemptingToPlace.Color;
			Renderer.Tint = Color.Lerp( BaseColor, color, 0.8f );
		}
		else if ( IsHovering )
		{
			Renderer.Tint = Color.Lerp( BaseColor, Color.Yellow, 0.5f );
		}
		else
		{
			Renderer.Tint = BaseColor;
		}
	}

}
