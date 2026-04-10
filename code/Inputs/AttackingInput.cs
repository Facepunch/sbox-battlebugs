using System;

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

	const float BaseReticleScale = 0.8f;
	const float MinSprayRadius = 48f;

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
			if ( !ShopPanel.Instance.IsOpen && !PauseMenu.Instance.IsOpen && tr.Hit && tr.GameObject.Components.TryGet<CellComponent>( out var cell ) )
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
						Reticle.WorldPosition = tr.HitPosition.WithZ( BoardManager.Local.WorldPosition.z ) + Vector3.Up * 4f;
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
				Reticle.WorldPosition = ReticlePosition + Vector3.Forward * MathF.Sin( Time.Now * 5f ) * 72f;
			}
			else if ( ReticleState == 2 )
			{
				Reticle.WorldPosition = ReticlePosition + ReticleOffset + Vector3.Right * MathF.Sin( Time.Now * 5f ) * 72f;
			}
		}

		// Scale the reticle to match the selected weapon's spray radius.
		if ( Reticle.IsValid() )
		{
			UpdateReticleScale();
		}

		if ( Reticle.IsValid() && Input.Pressed( "Attack1" ) && BoardManager.Local.WeaponInventory[BoardManager.Local.SelectedWeapon] != 0 )
		{
			if ( ReticleState == 0 )
			{
				AimingSound?.Stop();
				AimingSound = Sound.Play( "aiming-loop" );
			}
			Sound.Play( "aiming-click" );
			ReticleState++;
			ReticleOffset = Reticle.WorldPosition - ReticlePosition;
			if ( ReticleState < 2 ) ReticlePosition = Reticle.WorldPosition;
			if ( ReticleState == 3 )
			{
				BoardManager.Local.WeaponInventory[BoardManager.Local.SelectedWeapon]--;
				GameManager.Instance.BroadcastFire( BoardManager.Local.Id, BoardManager.Local.SelectedWeapon.ResourcePath, Reticle.WorldPosition );
				DestroyReticle();
			}
		}

		if ( Input.Pressed( "Attack2" ) && ReticleState == 1 )
		{
			Sound.Play( "ui.navigate.back" );
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
		UpdateReticleScale();
	}

	void UpdateReticleScale()
	{
		if ( !Reticle.IsValid() || BoardManager.Local?.SelectedWeapon is null ) return;

		var spray = MathF.Max( BoardManager.Local.SelectedWeapon.Spray, MinSprayRadius );
		// The base prefab scale (0.8) represents roughly one cell (~64 units)
		var scale = (spray / 64f) * BaseReticleScale;
		var plane = Reticle.Children.FirstOrDefault();
		if ( plane.IsValid() )
		{
			plane.LocalScale = new Vector3( scale, scale, 1f );
		}
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
