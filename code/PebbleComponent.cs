using System;
using Sandbox;

namespace Battlebugs;

public sealed class PebbleComponent : Component, Component.ICollisionListener
{
	[RequireComponent] Rigidbody Rigidbody { get; set; }

	[Property] GameObject ParticlePrefab { get; set; }

	[Sync] public float Damage { get; set; } = 4f;

	public TimeSince TimeSinceCreated = 0;

	public void LaunchAt( Vector3 target )
	{
		var time = Random.Shared.Float( 1.8f, 2f );
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
		if ( IsProxy ) return;

		if ( TimeSinceCreated > 10f )
		{
			GameObject.Destroy();
		}
	}

	public void OnCollisionStart( Collision collision )
	{
		if ( IsProxy ) return;
		if ( collision.Other.GameObject.Tags.Has( "pebble" ) ) return;

		if ( collision.Other.GameObject.Components.TryGet<BugSegment>( out var segment ) )
		{
			segment.Damage( Damage );
		}
		else if ( collision.Other.GameObject.Components.TryGet<CellComponent>( out var cell ) )
		{
			cell.BroadcastHit();
		}

		BroadcastDestroyEffect();
		GameObject.Destroy();
	}

	[Broadcast]
	void BroadcastDestroyEffect()
	{
		if ( ParticlePrefab is not null )
		{
			ParticlePrefab.Clone( Transform.Position );
		}
	}
}
