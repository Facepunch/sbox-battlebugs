namespace Battlebugs;

[AssetType( Category = "Battlebugs", Extension = "bug", Name = "Bug" )]
public class BugResource : GameResource
{
	[Group( "Information" )] public string Name { get; set; } = "Bug";
	[Group( "Information" )] public Color Color { get; set; } = Color.White;
	[Group( "Information" )] public int SegmentCount { get; set; } = 3;
	[Group( "Stats" )] public int StartingAmount { get; set; } = 1;
	[Group( "Stats" )] public float StartingHealth { get; set; } = 8f;
	[Group( "Prefabs" )] public GameObject HeadPrefab { get; set; }
	[Group( "Prefabs" )] public Model HeadModel { get; set; }
	[Group( "Prefabs" )] public GameObject BodyPrefab { get; set; }
	[Group( "Prefabs" )] public Model BodyModel { get; set; }
	[Group( "Prefabs" )] public GameObject CornerPrefab { get; set; }
	[Group( "Prefabs" )] public Model CornerModel { get; set; }
	[Group( "Prefabs" )] public GameObject TailPrefab { get; set; }
	[Group( "Prefabs" )] public Model TailModel { get; set; }

	public string GetHeadIcon() => "ui/thumbnails/" + ResourceName + "_head.png";
	public string GetBodyIcon() => "ui/thumbnails/" + ResourceName + "_body.png";
	public string GetCornerIcon() => "ui/thumbnails/" + ResourceName + "_corner.png";
	public string GetTailIcon() => "ui/thumbnails/" + ResourceName + "_tail.png";

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "bug_report", width, height );
	}

}
