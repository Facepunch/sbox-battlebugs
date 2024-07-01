using System;
using Sandbox;

namespace Battlebugs;

public sealed class InspectInput : Component
{
	public static InspectInput Instance { get; private set; }
	public BugSegment HighlightedSegment { get; private set; } = null;

	TimeSince timeSinceMouseMoved = 0;
	bool isPanelHovered = false;

	Vector2 lastMousePosition = Vector2.Zero;

	protected override void OnAwake()
	{
		Instance = this;
		Enabled = false;
	}

	protected override void OnUpdate()
	{
		if ( isPanelHovered ) return;
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

	public void Select( BugSegment bug, bool panelHovered = false )
	{
		isPanelHovered = panelHovered;
		HighlightedSegment = bug;
		HighlightedSegment.AddHighlight( Color.White );
	}

	public void Deselect()
	{
		isPanelHovered = false;
		if ( HighlightedSegment is null ) return;

		InspectorPanel.Instance.Segment = null;
		HighlightedSegment?.RemoveHighlight();
		HighlightedSegment = null;
	}
}
