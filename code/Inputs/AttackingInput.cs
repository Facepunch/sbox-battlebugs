using Sandbox;

namespace Battlebugs;

public sealed class AttackingInput : Component
{
	public static AttackingInput Instance { get; private set; }

	[Property] GameObject ReticlePrefab { get; set; }

	GameObject Reticle = null;
	Vector3 ReticlePosition = Vector3.Zero;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		var tr = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 8000f )
			.WithoutTags( "bug" )
			.Run();

		if ( tr.Hit && tr.GameObject.Tags.Has( "cell" ) )
		{
			if ( !Reticle.IsValid() )
			{
				if ( ReticlePrefab is not null )
				{
					Reticle = ReticlePrefab.Clone( tr.HitPosition );
				}
			}
			else
			{
				Reticle.Transform.Position = tr.HitPosition + Vector3.Up;
			}
		}
		else if ( Reticle.IsValid() )
		{
			Reticle?.Destroy();
			Reticle = null;
		}

		if ( Reticle.IsValid() && Input.Pressed( "Attack1" ) )
		{

		}
	}

	protected override void OnDisabled()
	{

	}
}
