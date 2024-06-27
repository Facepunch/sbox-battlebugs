namespace Battlebugs;

[GameResource( "Battlebugs/Weapon", "weapon", "Describes a weapon definition", Icon = "whatshot" )]
public class Weapon : GameResource
{
    public string Name { get; set; } = "Rock";
    [ImageAssetPath] public string Icon { get; set; }
    public int StartingAmount { get; set; } = 1;
    [Group( "Attack" )]
    public RangedFloat AmountFired { get; set; } = new RangedFloat( 1 );
    [Group( "Attack" )] public GameObject Prefab { get; set; }
}