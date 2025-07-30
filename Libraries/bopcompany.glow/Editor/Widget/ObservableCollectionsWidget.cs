using System.Collections.ObjectModel;
using static Sandbox.Component;

[CustomEditor( typeof( ObservableCollection<> ) )]
public class ObservableCollectionWidget : ControlObjectWidget
{
	// Whether or not this control supports multi-editing (if you have multiple GameObjects selected)
	public override bool SupportsMultiEdit => false;

	private ObservableCollectionsWidgetButton addElementButton;
	private SerializedCollection collection;
	private Layout content;

	public ObservableCollectionWidget( SerializedProperty property ) : base( property, false )
	{
		Layout = Layout.Row();
		Layout.Spacing = 2;

		collection = GetCollection( property );	

		//If an element gets added we recall RenderWidget so it will re-render everything
		collection.OnEntryAdded = RenderWidget;


		content = Layout.Column();

		Layout.Add( content );
		RenderWidget();
	}
	
	private void RenderWidget()
	{
		//Log.Info( "In Render Loop" );
		content.Clear( true);
		content.Margin = 0;

		//Needed otherwise when you add an element it will cause the inspector to adjust and you can see some artifact
		using SuspendUpdates _ = SuspendUpdates.For( this );

		Layout column = Layout.Column();

		CreateSpaceForElement( column );
		CreateButton( column );

		//Log.Info( "End of Render Loop" );

		content.Add( column );
	}

	private void CreateButton( Layout column )
	{
		Layout buttonRow = Layout.Row();
		buttonRow.Margin = new Sandbox.UI.Margin( Theme.ControlHeight + 2, 0, 0, 0 );

		addElementButton = new ObservableCollectionsWidgetButton( "add", "Element to add", Theme.ControlHeight )
		{
			MouseClick = AddElement,
		};

		buttonRow.Add( addElementButton );
		buttonRow.AddStretchCell( 1 );
		column.Add( buttonRow );
	}

	private void CreateSpaceForElement( Layout column )
	{
		int index = 0;
		//Creates space for the widget but it still won't render yet.
		foreach ( SerializedProperty entry in collection )
		{
			column.Add( new ObservableCollectionEntries( this, entry, index ) );
			index++;
		}

		//Log.Info( $"Added {index} items to collection" );
	}

	private void AddElement()
	{
		//Log.Info( "Adding new element!" );
		collection.Add( null );
		//Log.Info( "Finished Adding!" );
	}

	private static SerializedCollection GetCollection( SerializedProperty property )
	{
		if ( property == null ) return null;

		if ( !property.TryGetAsObject( out SerializedObject so ) || so is not SerializedCollection sc ) return null;

		//Log.Info( $"GetCollections: Successfully got the collection!" );
		return sc;
	}

	protected override void OnPaint()
	{
		// Overriding and doing nothing here will prevent the default background from being painted
	}

	public void RemoveAt( int index )
	{
		//Log.Info( $"Did we remove: {collection.RemoveAt( index )}");

		collection.RemoveAt( index );

		RenderWidget();
	}
}
