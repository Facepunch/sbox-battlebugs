namespace Battlebugs;

public class BugSegment : Component
{
    [Property] public GameObject Body { get; set; }
    [Property] public ModelRenderer BodyRenderer { get; set; }

    public int ParentSegments { get; set; } = 1;

    bool _initialized = false;

    public async void Init( Color color, int segments, float delay )
    {
        BodyRenderer.Tint = color;
        ParentSegments = segments;
        Body.Transform.LocalPosition = Vector3.Down * 250f;
        await GameTask.DelaySeconds( delay );
        _initialized = true;
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy ) return;
        if ( !_initialized ) return;

        Body.Transform.LocalPosition = Body.Transform.LocalPosition.LerpTo( Vector3.Zero, Time.Delta * 15f );
    }

    public void Clear()
    {
        if ( IsProxy ) return;
        // TODO: Funny destroy particles
        GameObject.Destroy();
    }
}