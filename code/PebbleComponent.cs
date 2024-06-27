using System;
using Sandbox;

namespace Battlebugs;

public sealed class PebbleComponent : Component, Component.ICollisionListener
{
	[RequireComponent] Rigidbody Rigidbody { get; set; }

	[Property] GameObject ParticlePrefab { get; set; }
	[Property] int HitCount { get; set; } = 1;

	[Sync] public float Damage { get; set; } = 4f;
	List<BugSegment> HitSegments = new();
	List<CellComponent> HitCells = new();

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
			if ( HitSegments.Contains( segment ) ) return;
			HitSegments.Add( segment );
			GameManager.Instance.BroadcastDamageNumber( Transform.Position, Damage );
			segment.Damage( Damage );
			Hit();
		}
		else if ( collision.Other.GameObject.Components.TryGet<CellComponent>( out var cell ) )
		{
			if ( HitCells.Contains( cell ) ) return;
			HitCells.Add( cell );
			cell.BroadcastHit();
			HitCount--;
			if ( HitCount == 0 ) Hit();
		}
	}

	void Hit()
	{
		HitCount--;
		if ( HitCount <= 0 )
		{
			BroadcastDestroyEffect();
			GameObject.Destroy();
		}
	}

	[Broadcast]
	void BroadcastDestroyEffect()
	{
		Sound.Play( "break-rocks", Transform.Position );
		if ( ParticlePrefab is not null )
		{
			ParticlePrefab.Clone( Transform.Position );
		}
	}
}
