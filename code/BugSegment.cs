using System;

namespace Battlebugs;

public class BugSegment : Component
{
    [Property] public GameObject Body { get; set; }
    [Property] public ModelRenderer BodyRenderer { get; set; }
    [Property] BugHealthbar Healthbar { get; set; }
    [Property] bool Floating { get; set; }

    [Property, Group( "Prefabs" )] public GameObject BugSplatParticle { get; set; }

    [Sync] public int Index { get; set; } = 0;
    [Sync] public int BugId { get; set; }
    [Sync] public float Health { get; set; } = 10f;
    public BugResource Bug => ResourceLibrary.Get<BugResource>( BugId );

    public CellComponent Cell { get; set; }

    bool _initialized { get; set; } = false;
    float _targetAlpha { get; set; } = 1f;
    TimeSince _timeSinceLastDamage { get; set; } = 0f;

    public bool IsVisible => _targetAlpha > 0f;

    public async void Init( BugResource bug, int index )
    {
        Index = index;
        BugId = bug.ResourceId;
        Health = bug.StartingHealth;
        Body.Transform.LocalPosition = Vector3.Down * 250f;
        Body.Transform.LocalRotation = new Angles( 0, Random.Shared.Float( -180f, 180f ), 0 );
        await GameTask.DelaySeconds( index * 0.05f );
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
        Healthbar.Alpha = Healthbar.Alpha.LerpTo( (_targetAlpha == 1 && GameManager.Instance.State != GameState.Placing) ? 1 : 0, Time.Delta * 5f );
    }

    protected override void OnFixedUpdate()
    {
        if ( _initialized || IsProxy )
        {
            var targetPos = Vector3.Up * 2.5f;
            if ( Floating ) targetPos += Vector3.Up * (MathF.Sin( (Time.Now + (Transform.Position.x + (Transform.Position.y * 10f)) / 32f) * 2f ) * 0.25f) * 8f;
            Body.Transform.LocalPosition = Body.Transform.LocalPosition.LerpTo( targetPos, Time.Delta * 15f );
            Body.Transform.LocalRotation = Rotation.Slerp( Body.Transform.LocalRotation, Rotation.Identity, Time.Delta * 15f );
        }

        if ( !IsProxy )
        {
            if ( _timeSinceLastDamage > 1f && Health <= 0 )
            {
                Clear();
            }
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
        if ( GameManager.Instance.State == GameState.Playing ) Cell.BroadcastClear();
        BroadcastDestroyFX();
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
        if ( !IsValid ) return;
        Components.Get<HighlightOutline>()?.Destroy();
    }

    CellComponent GetCell()
    {
        return Scene.GetAllComponents<CellComponent>().OrderBy( x => x.Transform.Position.DistanceSquared( Transform.Position ) ).FirstOrDefault();
    }

    [Broadcast]
    public void Damage( float damage )
    {
        if ( Health <= 0 ) return;

        _targetAlpha = 1f;
        GetCell()?.BroadcastHit();

        if ( IsProxy ) return;

        Health -= damage;
        _timeSinceLastDamage = 0f;
    }

    [Broadcast]
    void BroadcastDestroyFX()
    {
        Sound.Play( "impact-bullet-flesh", Transform.Position );
        GameManager.Instance.SpawnCoins( Transform.Position, 3 );
        if ( BugSplatParticle is not null )
        {
            var obj = BugSplatParticle.Clone( Transform.Position + Vector3.Up * 16f );
            var part = obj.Components.Get<ParticleEffect>();
            part.Tint = Bug.Color;
        }
    }
}