namespace Battlebugs;

[GameResource( "Battlebugs/Weapon", "weapon", "Describes a weapon definition", Icon = "whatshot" )]
public class Weapon : GameResource
{
    public string Name { get; set; } = "Rock";

    public RangedFloat Amount { get; set; }
    public GameObject Prefab { get; set; }
}