using Sandbox.UI;

public class ObservableCollectionEntries : Widget
{
	private readonly ObservableCollectionWidget observableCollectionEntries;
	//Creates a line at index
	public ObservableCollectionEntries( ObservableCollectionWidget parent, SerializedProperty property, int index ) : base( parent )
	{
		observableCollectionEntries = parent;

		Layout = Layout.Row();
		Layout.Margin = new Margin( 0, 2 );
		Layout.Spacing = 2;

		ReadOnly = parent.ReadOnly;
		Enabled = parent.Enabled;

		ToolTip = $"Element {index}";

		ControlWidget control = ControlWidget.Create( property );
		control.ReadOnly = ReadOnly;
		control.Enabled = Enabled;

		ObservableCollectionsWidgetButton removeButton = new ObservableCollectionsWidgetButton( "clear", "Remove",
			Theme.RowHeight, () => RemoveAt( index ) );

		Layout.Add( control );
		Layout.Add( removeButton );

		AcceptDrops = true;
	}

	private void RemoveAt( int index )
	{
		observableCollectionEntries.RemoveAt( index );
	}
}

