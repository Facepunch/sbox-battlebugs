namespace Battlebugs;

public class BugSegment : Component
{
    [Property] public GameObject Body { get; set; }
    [Property] public ModelRenderer BodyRenderer { get; set; }

    [Sync] public int BugId { get; set; }
    [Sync] public float Health { get; set; } = 10f;
    public Bug Bug => ResourceLibrary.Get<Bug>( BugId );

    public CellComponent Cell { get; set; }

    bool _initialized { get; set; } = false;
    float _targetAlpha { get; set; } = 1f;

    public async void Init( Bug bug, float delay )
    {
        BugId = bug.ResourceId;
        BodyRenderer.Tint = bug.Color;
        Health = bug.StartingHealth;
        Body.Transform.LocalPosition = Vector3.Down * 250f;
        await GameTask.DelaySeconds( delay );
        _initialized = true;
        Sound.Play( "segment-drop" );
    }

    protected override void OnStart()
    {
        SetAlpha( IsProxy ? 0f : 1f, true );
    }

    protected override void OnUpdate()
    {
        foreach ( var renderer in Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ) )
        {
            renderer.Tint = renderer.Tint.WithAlpha( renderer.Tint.a.LerpTo( _targetAlpha, Time.Delta * 5f ) );
        }
    }

    protected override void OnFixedUpdate()
    {
        if ( _initialized || IsProxy )
        {
            Body.Transform.LocalPosition = Body.Transform.LocalPosition.LerpTo( Vector3.Zero, Time.Delta * 15f );
        }
    }

    public void SetAlpha( float alpha, bool instant = false )
    {
        if ( instant )
        {
            foreach ( var renderer in Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ) )
            {
                renderer.Tint = renderer.Tint.WithAlpha( alpha );
            }
        }
        _targetAlpha = alpha;
    }

    public void Clear()
    {
        if ( IsProxy ) return;
        // TODO: Funny destroy particles
        GameObject.Destroy();
    }


    public void AddHighlight( Color color = default )
    {
        var outline = Components.GetOrCreate<HighlightOutline>();
        outline.Color = color;
        outline.InsideColor = Color.Transparent;
        outline.ObscuredColor = Color.Transparent;
        outline.InsideObscuredColor = Color.Transparent;
    }

    public void RemoveHighlight()
    {
        Components.Get<HighlightOutline>()?.Destroy();
    }

    CellComponent GetCell()
    {
        return Scene.GetAllComponents<CellComponent>().OrderBy( x => x.Transform.Position.DistanceSquared( Transform.Position ) ).FirstOrDefault();
    }

    [Broadcast]
    public void Damage( float damage )
    {
        _targetAlpha = 1f;
        GetCell()?.BroadcastHit();

        if ( IsProxy ) return;

        Health -= damage;

        if ( Health <= 0f )
        {
            Clear();
        }
    }
}