using System;
using Sandbox;

namespace Battlebugs;

public sealed class PebbleComponent : Component, Component.ICollisionListener
{
	[RequireComponent] Rigidbody Rigidbody { get; set; }

	[Property] GameObject ParticlePrefab { get; set; }

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

	protected override void OnFixedUpdate()
	{
		if ( TimeSinceCreated > 10f )
		{
			GameObject.Destroy();
		}
	}

	public void OnCollisionStart( Collision collision )
	{
		Log.Info( collision );
		if ( ParticlePrefab is not null )
		{
			ParticlePrefab.Clone( Transform.Position );
		}
		GameObject.Destroy();
	}
}
