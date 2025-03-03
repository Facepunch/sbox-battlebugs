using Sandbox;

namespace Battlebugs;

public sealed class DamageNumber : Component
{
	[RequireComponent] TextRenderer Renderer { get; set; }

	TimeSince timeSinceCreated = 0;
	float speed = 100f;

	protected override void OnUpdate()
	{
		WorldPosition += Vector3.Up * speed * Time.Delta;
		speed = speed.LerpTo( 0, Time.Delta * 4f );

		if ( timeSinceCreated > 1f )
		{
			Renderer.Color = Renderer.Color.WithAlpha( Renderer.Color.a.LerpTo( 0, Time.Delta * 4f ) );
			if ( Renderer.Color.a <= 0.01f )
			{
				GameObject.Destroy();
			}
		}
	}

	protected override void OnPreRender()
	{
		WorldRotation = Rotation.LookAt( WorldPosition - Scene.Camera.WorldPosition );
	}
}
