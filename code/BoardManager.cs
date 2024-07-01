using System;
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
	public static BoardManager Opponent => Game.ActiveScene.GetAllComponents<BoardManager>().FirstOrDefault( x => !x.Network.IsOwner );

	// Properties
	[Property] public int GridSize { get; set; } = 64;
	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 10;
	[Property, Group( "References" )] public GameObject CameraPosition { get; set; }

	// Networked Variables
	[Sync] public bool IsReady { get; set; } = false;
	[Sync] public NetList<BugReference> BugReferences { get; set; } = new();

	// Public Variables
	public Weapon SelectedWeapon = null;
	public Dictionary<BugResource, int> BugInventory = new();
	public Dictionary<Weapon, int> WeaponInventory = new();
	public int MaxPlaceableSegments => BugInventory.Where( x => x.Value > 0 ).OrderBy( x => x.Key.SegmentCount ).LastOrDefault().Key?.SegmentCount ?? 0;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		InitBoard();
		ResetBugInventory();
		ResetWeaponInventory();
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
				var index = x + y * (Width + 1);
				cell.Init( this, new Vector2( x, y ), index );
				cellObj.SetParent( GameObject );
				cellObj.NetworkSpawn( Network.OwnerConnection );
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
		Sound.Play( "clear-all-bugs" );
		var segments = Scene.GetAllComponents<BugSegment>();
		foreach ( var segment in segments )
		{
			if ( segment.Network.OwnerId != Network.OwnerId ) continue;
			segment.Clear();
		}
		var cells = Components.GetAll<CellComponent>( FindMode.InChildren );
		foreach ( var cell in cells )
		{
			cell.IsOccupied = false;
		}
		ResetBugInventory();
	}

	public void ToggleReady()
	{
		if ( IsProxy ) return;
		IsReady = !IsReady;
	}

	void ResetBugInventory()
	{
		var allBugs = ResourceLibrary.GetAll<BugResource>();
		BugInventory.Clear();
		foreach ( var bug in allBugs )
		{
			BugInventory[bug] = bug.StartingAmount;
		}
	}

	void ResetWeaponInventory()
	{
		var allWeapons = ResourceLibrary.GetAll<Weapon>();
		WeaponInventory.Clear();
		foreach ( var weapon in allWeapons )
		{
			WeaponInventory[weapon] = weapon.StartingAmount;
			if ( weapon.StartingAmount < 0 ) SelectedWeapon = weapon;
		}
	}

	public void SaveBugReferences()
	{
		BugReferences.Clear();

		// Compose the list of bugs and their individual ids
		var references = new List<BugReference>();
		var segments = Scene.GetAllComponents<BugSegment>();
		foreach ( var segment in segments )
		{
			if ( segment.Network.OwnerId != Network.OwnerId ) continue;
			var existingRef = references.FirstOrDefault( x => x.BugId == segment.GameObject.Name );
			if ( existingRef.ResourceId != 0 ) existingRef.Add( segment.GameObject.Id );
			else
			{
				var reference = new BugReference( segment.BugId, segment.GameObject.Name );
				reference.Add( segment.GameObject.Id );
				references.Add( reference );
			}
		}

		// Add entries to the NetList
		foreach ( var reference in references )
		{
			BugReferences.Add( reference );
		}
	}

	public struct BugReference
	{
		public int ResourceId { get; set; }
		public string BugId { get; set; }
		public List<string> ObjectIds { get; set; }

		private BugResource _bug;

		public BugReference( int resourceId, string bugId )
		{
			ResourceId = resourceId;
			BugId = bugId;
			ObjectIds = new List<string>();
		}

		public void Add( Guid objectId )
		{
			ObjectIds.Add( objectId.ToString() );
		}

		public BugResource GetBug()
		{
			if ( _bug is null ) _bug = ResourceLibrary.Get<BugResource>( ResourceId );
			return _bug;
		}
	}

}
