using Sandbox;
using System;

public class ObservableCollectionsWidgetButton : IconButton
{
	public ObservableCollectionsWidgetButton( string icon, string toolTip, float controlRowHeight, Action onClick = null, Widget parent = null ) : base( icon, onClick, parent )
	{
		Background = Theme.ControlBackground;
		ToolTip = toolTip;
		FixedWidth = controlRowHeight;
		FixedHeight = controlRowHeight;
		AcceptDrops = true;
	}
}
