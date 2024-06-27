using System;
using Sandbox;

namespace Battlebugs;

public sealed class InspectInput : Component
{
	public static InspectInput Instance { get; private set; }
	BugSegment HighlightedSegment = null;

	TimeSince timeSinceMouseMoved = 0;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		if ( !BoardManager.Local.IsValid() ) return;
		if ( Input.MouseDelta.Length > 0 ) timeSinceMouseMoved = 0;

		var tr = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 8000f )
			.WithAnyTags( "bug" )
			.Run();

		if ( tr.Hit )
		{
			var bug = tr.GameObject.Components.Get<BugSegment>();
			if ( bug is not null && bug.IsVisible )
			{
				Deselect();
				HighlightedSegment = bug;
				HighlightedSegment.AddHighlight( Color.White );
			}
		}
		else
		{
			Deselect();
		}
	}

	protected override void OnDisabled()
	{

	}

	void Deselect()
	{
		if ( HighlightedSegment is null ) return;

		HighlightedSegment.RemoveHighlight();
	}
}
