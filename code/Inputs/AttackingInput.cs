using Sandbox;

namespace Battlebugs;

public sealed class AttackingInput : Component
{
	public static AttackingInput Instance { get; private set; }

	[Property] GameObject ReticlePrefab { get; set; }

	GameObject Reticle = null;
	Vector3 ReticlePosition = Vector3.Zero;
	Vector3 ReticleOffset = Vector3.Zero;
	int ReticleState = 0;

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

		if ( ReticleState == 0 )
		{
			if ( tr.Hit && tr.GameObject.Components.TryGet<CellComponent>( out var cell ) )
			{
				if ( cell.Board != BoardManager.Local )
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
			}
			else if ( Reticle.IsValid() )
			{
				Reticle?.Destroy();
				Reticle = null;
			}
		}

		if ( Reticle.IsValid() && Input.Pressed( "Attack1" ) )
		{
			ReticleState++;
			ReticleOffset = Reticle.Transform.Position - ReticlePosition;
			ReticlePosition = Reticle.Transform.Position;
		}

		if ( Input.Pressed( "Attack2" ) && ReticleState > 0 )
		{
			ReticleState--;
		}
	}

	protected override void OnDisabled()
	{

	}
}
