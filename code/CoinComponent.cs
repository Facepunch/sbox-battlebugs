using System;
using Sandbox;

public sealed class CoinComponent : Component
{
	Vector3 Velocity = Vector3.Zero;

	protected override void OnStart()
	{
		Velocity = Vector3.Random.Normal;
		Velocity = Velocity.WithZ( MathF.Abs( Velocity.z ) );
	}

	protected override void OnFixedUpdate()
	{
		Velocity += Vector3.Down * 100 * Time.Delta;

		var tr = Scene.Trace.Ray( Transform.Position, Transform.Position + Velocity * Time.Delta )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
		{
			Velocity = Vector3.Reflect( Velocity, tr.Normal );
		}

		Transform.Position += Velocity * Time.Delta;
	}
}
