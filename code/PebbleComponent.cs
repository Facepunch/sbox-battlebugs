using System;
using Sandbox;

namespace Battlebugs;

public sealed class PebbleComponent : Component
{
	[RequireComponent] Rigidbody Rigidbody { get; set; }

	public TimeSince TimeSinceCreated = 0;

	public void LaunchAt( Vector3 target )
	{
		var time = Random.Shared.Float( 1.5f, 2f );
		var vector = target - Transform.Position.WithZ( target.z );
		var direction = vector.Normal;
		var velocity = vector / time;
		var verticalForce = -Scene.PhysicsWorld.Gravity.z * time / 2f;
		velocity += Vector3.Up * verticalForce;

		Rigidbody.Velocity = velocity;
		Rigidbody.AngularVelocity = Vector3.Random * 10f;
	}
}
