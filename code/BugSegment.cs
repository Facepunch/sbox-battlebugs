namespace Battlebugs;

public class BugSegment : Component
{
    [Property] public GameObject Body { get; set; }
    [Property] public ModelRenderer BodyRenderer { get; set; }

    public int ParentSegments { get; set; } = 1;

    bool _initialized { get; set; } = false;
    float _targetAlpha { get; set; } = 1f;

    public async void Init( Color color, int segments, float delay )
    {
        BodyRenderer.Tint = color;
        ParentSegments = segments;
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
            renderer.Tint = renderer.Tint.WithAlpha( renderer.Tint.a.LerpTo( _targetAlpha, Time.Delta * 15f ) );
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
}