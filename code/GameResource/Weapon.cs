namespace Battlebugs;

[GameResource( "Battlebugs/Weapon", "weapon", "Describes a weapon definition", Icon = "whatshot" )]
public class WeaponResource : GameResource
{
    public string Name { get; set; } = "Rock";
    [TextArea] public string Description { get; set; } = "";
    [ImageAssetPath] public string Icon { get; set; }
    public int StartingAmount { get; set; } = 1;
    [Group( "Attack" )] public RangedFloat AmountFired { get; set; } = new RangedFloat( 1 );
    [Group( "Attack" )] public RangedFloat Damage { get; set; } = new RangedFloat( 8 );
    [Group( "Attack" )] public float Spray { get; set; } = 1f;
    [Group( "Attack" )] public GameObject Prefab { get; set; }
}