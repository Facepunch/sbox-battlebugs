namespace Battlebugs;

[GameResource( "Battlebugs/Bug", "bug", "Describes a bug definition", Icon = "bug_report" )]
public class Bug : GameResource
{
    public string Name { get; set; } = "Bug";
    public Color Color { get; set; } = Color.White;
    public int SegmentCount { get; set; } = 3;
    public GameObject SegmentPrefab { get; set; }
    public int StartingAmount { get; set; } = 1;
}