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
	public static BoardManager Opponent => Game.ActiveScene.GetAllComponents<BoardManager>().FirstOrDefault( x => x.Network.Owner != Connection.Local );

	// Properties
	[Property] public int GridSize { get; set; } = 64;
	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 10;
	[Property, Group( "References" )] public GameObject CameraPosition { get; set; }

	// Networked Variables
	[Sync] public int Coins { get; set; } = 100;
	[Sync] public bool IsReady { get; set; } = false;
	[Sync] public NetList<BugReference> BugReferences { get; set; } = new();
	[Sync] public int CoinsSpent { get; set; } = 0;
	[Sync] public int BugsKilled { get; set; } = 0;

	// Public Variables
	public WeaponResource SelectedWeapon = null;
	public Dictionary<BugResource, int> BugInventory = new();
	public Dictionary<WeaponResource, int> WeaponInventory = new();
	public int MaxPlaceableSegments => BugInventory.Where( x => x.Value > 0 ).OrderBy( x => x.Key.SegmentCount ).LastOrDefault().Key?.SegmentCount ?? 0;
	TimeSince timeSinceTurnStart = 0;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		InitBoard();
		ResetBugInventory();
		ResetWeaponInventory();

		if ( Network.Owner is null )
		{
			SetupBoardRandomly();
			IsReady = true;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( GameManager.Instance.CurrentPlayer != this ) timeSinceTurnStart = 0;
		if ( Network.Owner is null && timeSinceTurnStart > 2.5f && GameManager.Instance.CurrentPlayer == this && GameManager.Instance.State == GameState.Playing && GameManager.Instance.IsFiring )
		{
			AttackRandomly();
		}
	}

	void InitBoard()
	{
		Vector3 startingPosition = WorldPosition + new Vector3( -(Width * GridSize) / 2f + GridSize / 2f, -(Height * GridSize) / 2f + GridSize / 2f, 0 );
		for ( int x = 0; x < Width; x++ )
		{
			for ( int y = 0; y < Height; y++ )
			{
				var cellObj = GameManager.Instance.CellPrefab.Clone( startingPosition + new Vector3( x * GridSize, y * GridSize, 0 ) );
				var cell = cellObj.Components.Get<CellComponent>();
				var index = x + y * (Width + 1);
				cell.Init( this, new Vector2( x, y ), index );
				cellObj.SetParent( GameObject );
				cellObj.NetworkSpawn( Network.Owner );
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

	public void ClearAllBugs( bool playSound = true )
	{
		if ( playSound ) Sound.Play( "clear-all-bugs" );
		var segments = Scene.GetAllComponents<BugSegment>();
		foreach ( var segment in segments )
		{
			if ( segment.Network.OwnerId != Network.OwnerId ) continue;
			segment.Cell.IsOccupied = false;
			segment.GameObject.Destroy();
		}
		var cells = Components.GetAll<CellComponent>( FindMode.InChildren );
		foreach ( var cell in cells )
		{
			cell.IsOccupied = false;
			cell.BroadcastUpdateHighlight();
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

	[Rpc.Owner]
	public void AttackRandomly()
	{
		var targetSegment = Scene.GetAllComponents<BugSegment>().OrderBy( x => Random.Shared.Float() ).FirstOrDefault( x => x.Network.OwnerId != Network.OwnerId );
		var targetPosition = targetSegment.WorldPosition + (Vector3.Random.WithZ( 0 ) * (targetSegment.IsVisible ? 48 : 250));
		var opponentPos = targetSegment.GameObject.Root.WorldPosition;
		targetPosition = new Vector3(
			Math.Clamp( targetPosition.x, opponentPos.x - (Width * GridSize) / 2f, opponentPos.x + (Width * GridSize) / 2f ),
			Math.Clamp( targetPosition.y, opponentPos.y - (Height * GridSize) / 2f, opponentPos.y + (Height * GridSize) / 2f ),
			0
		);

		var weapon = WeaponInventory.OrderBy( x => Random.Shared.Float() ).FirstOrDefault( x => x.Value != 0 ).Key;
		SelectedWeapon = weapon;
		WeaponInventory[SelectedWeapon]--;
		GameManager.Instance.BroadcastFire( Id, SelectedWeapon.ResourceId, targetPosition );
	}

	void ResetWeaponInventory()
	{
		var allWeapons = ResourceLibrary.GetAll<WeaponResource>();
		WeaponInventory.Clear();
		foreach ( var weapon in allWeapons )
		{
			WeaponInventory[weapon] = weapon.StartingAmount;
			if ( weapon.StartingAmount < 0 ) SelectedWeapon = weapon;
		}
	}

	public float GetHealthPercent()
	{
		var segments = Scene.GetAllComponents<BugSegment>().Where( x => x.Network.OwnerId == Network.OwnerId );
		var totalSegments = segments.Count();
		var totalHealth = segments.Sum( x => x.Health );
		var totalMaxHealth = BugReferences.Sum( x => ResourceLibrary.Get<BugResource>( x.ResourceId ).StartingHealth * x.ObjectIds.Count );
		return (float)totalHealth / totalMaxHealth;
	}

	public float GetScorePercent()
	{
		if ( GameManager.Instance.State < GameState.Playing ) return 0.5f;
		var myScore = GetHealthPercent();
		var opponentScore = Scene.GetAllComponents<BoardManager>().FirstOrDefault( x => x.Network.OwnerId != Network.OwnerId ).GetHealthPercent();
		return myScore / (myScore + opponentScore);

	}

	[Rpc.Owner]
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

	public void PurchaseWeapon( WeaponResource weapon )
	{
		Coins -= weapon.Cost;
		WeaponInventory[weapon]++;
		Sandbox.Services.Stats.Increment( "coins_spent", weapon.Cost );
	}

	[Rpc.Owner]
	public void GiveCoins( int amount )
	{
		Coins += amount;
		CoinsSpent += amount;
		Sandbox.Services.Stats.Increment( "coins_earned", amount );
	}

	[Rpc.Owner]
	public void IncrementBugsKilled()
	{
		BugsKilled++;
		Sandbox.Services.Stats.Increment( "bugs_killed", 1 );
	}

	public void IncrementDamageDealt( float damage )
	{
		Sandbox.Services.Stats.Increment( "damage_dealt", (int)damage );
	}

	[Rpc.Owner]
	public void GiveCellCoins( Vector3 position )
	{
		GiveCoins( 5 );
		GameManager.Instance.SpawnCoins( position, 1 );
		HintPanel.Instance.CellCoinNotification();
	}

	public void SetupBoardRandomly()
	{
		ClearAllBugs( false );
		int attempts = 0;
		foreach ( var entry in BugInventory )
		{
			while ( BugInventory[entry.Key] > 0 )
			{
				while ( !TryPlaceRandomBug( entry.Key ) )
				{
					attempts++;
					if ( attempts > 100 )
					{
						SetupBoardRandomly();
						return;
					}
				}
			}
		}
	}

	bool TryPlaceRandomBug( BugResource bug )
	{
		int attempts = 0;

		var startingCell = Components.GetAll<CellComponent>().Where( x => !x.IsOccupied && x.GameObject.Root == GameObject ).OrderBy( x => Guid.NewGuid() ).FirstOrDefault();
		if ( startingCell is null ) return false;

		var cells = new List<CellComponent> { startingCell };

		while ( cells.Count < bug.SegmentCount )
		{
			var cell = cells.Last();
			var neighbors = cell.GetNeighbors().Where( x => !x.IsOccupied && !cells.Contains( x ) && x.GameObject.Root == GameObject ).ToList();
			if ( neighbors.Count == 0 )
			{
				attempts++;
				if ( attempts > 100 ) return false;
				cells = new List<CellComponent> { startingCell };
				continue;
			}
			var nextCell = neighbors.OrderBy( x => Random.Shared.Float() ).First();
			cells.Add( nextCell );
		}

		GameManager.Instance.CreateBug( this, PlacementInput.Instance.GetPlacementData( cells, bug ), Network.Owner is null );
		return true;
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
