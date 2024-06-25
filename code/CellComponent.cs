using System;
using Sandbox;

namespace Battlebugs;

public sealed class CellComponent : Component
{
	[RequireComponent] ModelRenderer Renderer { get; set; }

	public BoardManager Board { get; set; }
	public Vector2 Position { get; set; }
	public bool IsHovering { get; private set; } = false;
	public bool IsSelected { get; set; } = false;
	public bool IsOccupied { get; set; } = false;

	bool IsOdd = false;

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
		Renderer.Tint = IsOccupied ? Color.Red : Color.Yellow;
	}

	public void MouseExit()
	{
		IsHovering = false;

		if ( IsSelected ) return;
		Renderer.Tint = IsOdd ? Color.White : Color.Gray;
	}

	public void Select()
	{
		IsSelected = true;
		GameInput.Instance.SelectedCells.Add( this );

		Renderer.Tint = Color.Blue;
	}

	public void Deselect()
	{
		IsSelected = false;

		Renderer.Tint = IsOdd ? Color.White : Color.Gray;
	}

	public bool IsAdjacent(CellComponent other)
	{
		// Above/below
		if ( Position.x == other.Position.x && MathF.Abs( Position.y - other.Position.y ) == 1 )
			return true;

		// Left/right
		if ( Position.y == other.Position.y && MathF.Abs( Position.x - other.Position.x ) == 1 )
			return true;

		return false;
	}

}
