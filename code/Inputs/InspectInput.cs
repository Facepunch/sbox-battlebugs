using System;
using Sandbox;

namespace Battlebugs;

public sealed class InspectInput : Component
{
	public static InspectInput Instance { get; private set; }
	BugSegment HighlightedSegment = null;

	TimeSince timeSinceMouseMoved = 0;

	Vector2 lastMousePosition = Vector2.Zero;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		if ( !BoardManager.Local.IsValid() ) return;
		if ( Mouse.Position != lastMousePosition )
		{
			lastMousePosition = Mouse.Position;
			timeSinceMouseMoved = 0;
		}

		var tr = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 8000f )
			.WithAnyTags( "bug" )
			.Run();

		if ( tr.Hit )
		{
			var bug = tr.GameObject.Components.Get<BugSegment>();
			if ( bug is not null && bug.IsVisible )
			{
				Deselect();
				Select( bug );
			}
		}
		else
		{
			Deselect();
		}

		if ( HighlightedSegment is not null && timeSinceMouseMoved > 0.5f && InspectorPanel.Instance.Segment is null )
		{
			InspectorPanel.Instance.Segment = HighlightedSegment;
		}
	}

	protected override void OnDisabled()
	{
		Deselect();
	}

	void Select( BugSegment bug )
	{
		HighlightedSegment = bug;
		HighlightedSegment.AddHighlight( Color.White );
	}

	void Deselect()
	{
		if ( HighlightedSegment is null ) return;

		InspectorPanel.Instance.Segment = null;
		HighlightedSegment?.RemoveHighlight();
		HighlightedSegment = null;
	}
}
