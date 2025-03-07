using System;
using Sandbox;

public sealed class CoinComponent : Component
{
	Vector3 Velocity = Vector3.Zero;
	TimeSince timeSinceStart = 0;

	protected override void OnStart()
	{
		Velocity = Vector3.Random.Normal;
		Velocity = Velocity.WithZ( MathF.Abs( Velocity.z ) );
		Velocity = Velocity.Normal * Random.Shared.Float( 20, 100 );
		timeSinceStart = Random.Shared.Float( 0.2f );
	}

	protected override void OnFixedUpdate()
	{
		Velocity += Vector3.Down * 100 * Time.Delta;

		var tr = Scene.Trace.Ray( WorldPosition, WorldPosition + Velocity * Time.Delta )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
		{
			if ( Velocity.z < -10 ) Sound.Play( "coin-bounce", WorldPosition );
			Velocity = Vector3.Reflect( Velocity, tr.Normal );
			Velocity /= 1.5f;
		}

		WorldPosition += Velocity * Time.Delta;

		if ( timeSinceStart > 1f )
		{
			Sound.Play( "coin-collect", WorldPosition );
			GameObject.Destroy();
		}
	}
}
