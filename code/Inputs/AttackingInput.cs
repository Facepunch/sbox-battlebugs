using System;
using Sandbox;

namespace Battlebugs;

public sealed class AttackingInput : Component
{
	public static AttackingInput Instance { get; private set; }

	[Property] GameObject ReticlePrefab { get; set; }

	GameObject Reticle = null;
	Vector3 ReticlePosition = Vector3.Zero;
	Vector3 ReticleOffset = Vector3.Zero;
	public int ReticleState = 0;
	SoundHandle AimingSound = null;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		if ( !BoardManager.Local.IsValid() ) return;

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
							CreateReticle( tr.HitPosition );
						}
					}
					else
					{
						Reticle.Transform.Position = tr.HitPosition.WithZ( 0 ) + Vector3.Up;
					}
				}
			}
			else if ( Reticle.IsValid() )
			{
				DestroyReticle();
			}
		}
		else if ( Reticle.IsValid() )
		{
			if ( ReticleState == 1 )
			{
				Reticle.Transform.Position = ReticlePosition + Vector3.Forward * MathF.Sin( Time.Now * 5f ) * 72f;
			}
			else if ( ReticleState == 2 )
			{
				Reticle.Transform.Position = ReticlePosition + ReticleOffset + Vector3.Right * MathF.Sin( Time.Now * 5f ) * 72f;
			}
		}

		if ( Reticle.IsValid() && Input.Pressed( "Attack1" ) && BoardManager.Local.WeaponInventory[BoardManager.Local.SelectedWeapon] != 0 )
		{
			if ( ReticleState == 0 )
			{
				AimingSound?.Stop();
				AimingSound = Sound.Play( "aiming-loop" );
			}

			ReticleState++;
			ReticleOffset = Reticle.Transform.Position - ReticlePosition;
			if ( ReticleState < 2 ) ReticlePosition = Reticle.Transform.Position;
			if ( ReticleState == 3 )
			{
				BoardManager.Local.WeaponInventory[BoardManager.Local.SelectedWeapon]--;
				GameManager.Instance.BroadcastFire( BoardManager.Local.SelectedWeapon.ResourceId, Reticle.Transform.Position );
				DestroyReticle();
			}
		}

		if ( Input.Pressed( "Attack2" ) && ReticleState == 1 )
		{
			ReticleState--;
			if ( ReticleState == 0 )
			{
				AimingSound?.Stop();
				AimingSound = null;
			}
		}
	}

	protected override void OnDisabled()
	{
		DestroyReticle();
	}

	void CreateReticle( Vector3 position )
	{
		Reticle = ReticlePrefab.Clone( position );
	}

	void DestroyReticle()
	{
		AimingSound?.Stop();
		AimingSound = null;
		Reticle?.Destroy();
		Reticle = null;
		ReticleState = 0;
	}
}
