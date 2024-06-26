using Sandbox;

namespace Battlebugs;

public sealed class RotateConstantly : Component
{
	[Property] Angles RotationSpeed { get; set; }

	protected override void OnUpdate()
	{
		Transform.Rotation *= RotationSpeed * Time.Delta;
	}
}
