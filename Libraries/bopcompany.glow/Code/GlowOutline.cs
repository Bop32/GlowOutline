using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Rendering;

[Icon( "Accessibility_New" )]
public sealed class GlowOutline : BasePostProcess<GlowOutline>
{
	public enum DownSampleMethods : byte
	{
		Box = 0,
		GaussianBlur = 1,
		Max = 3,
		Min = 4,
	}

	[Property, Feature( "Glow Settings" ), Title( "Default Color" )]
	private Color defaultGlowColor = new( 0.10f, 0.32f, 0.79f, 1.00f );

	[Property, Range( 0, 2 ), Step( 1 ), Description( "Changes the resolution of the blur (higher value means lower quality)" ), Feature( "Glow Settings" )]
	private readonly int glowMips = 2;


	[Property, Range( 0.5f, 4.0f ), Step( 0.5f ), Description( "How big you want to the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowSize = 1.5f;

	[Property, Range( 0.25f, 1 ), Step( 0.25f ), Description( "How bright you want the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowIntensity = 0.75f;

	[Property, Feature( "Glow Settings" ), Description( "If enabled, automatically finds all objects with a `Glowable` component and applies a glow effect." )]
	private readonly bool autoFindGlowables = false;

	[Header( "Glow Rendering" )]

	[Property, Title( "Glow Type" ), Description( "Which downsample method to use when we downscale the texture." ), Feature( "Glow Settings" )]
	private DownSampleMethods DownsampleMethod { get; set; } = DownSampleMethods.GaussianBlur;

	[Property, Title( "Objects" ), Feature( "Objects to Glow" ), InlineEditor( Label = false )]
	private List<GlowObject> objectsToGlow = null;

	private const string TmpTexture = "TmpTexture";

	private const string MaskRT = "MaskRT";

	private const string MaskRTCopy = "MaskRTCopy";

	private readonly Material maskMaterial = Material.FromShader( "shaders/Mask.shader" );
	private readonly Material compositeMaterial = Material.FromShader( "shaders/Composite.shader" );
	private readonly Material verticalBlurMaterial = Material.FromShader( "shaders/BlurVertical.shader" );
	private readonly Material horizontalBlurMaterial = Material.FromShader( "shaders/BlurHorizontal.shader" );
	
	RendererSetup maskRenderSetup = default;
	public static GlowOutline Instance { get; private set; }
	public int GlowCount => objectsToGlow.Count;

	private readonly CommandList commandList = new( "GlowOutline" );

	protected override void OnAwake()
	{
		// This SHOULD only happen in the editor since people like to change editor width and height etc.
		if ( Screen.Size.x % 2 == 1 || Screen.Size.y % 2 == 1 )
		{
			Log.Error( $"Glow Outline: To avoid uneven outlines, please use an even screen resolution. This message should only appear in the editor. Current resolution: {Screen.Size}." );
		}

		maskRenderSetup = new RendererSetup
		{
			Material = maskMaterial,
			Transform = null,
			Color = null
		};

		if ( objectsToGlow == null ) objectsToGlow = new();

		SetTransparentColorToDefault();

		if ( Instance != null )
		{
			Log.Info( "Here" );
			Log.Error( "GlowOutline: Only one instance of GlowOutline is supported. " +
				"If you want to use multiple GlowOutline components you will need to get call `GetComponent<GlowOutline>()` manually." );
		}
		else
		{
			Instance = this;
		}
	}

	protected override void OnStart()
	{
		if ( !autoFindGlowables ) return;

		FindGlowableObjects();
	}

	public override void Render()
	{
		if ( GlowCount <= 0 ) return;

		commandList.Reset();
		RenderOutlineEffect();

		InsertCommandList( commandList, Stage.AfterTransparent, 100, "GlowOutline" );
	}

	private void RenderOutlineEffect()
	{
		RenderTargetHandle maskRT = CreateMaskRenderTarget( MaskRT );

		Log.Info( "Hello" );
		try
		{
			RenderTargetHandle blurredRT = BlurMaskRenderTarget();
			try
			{
				Composite( blurredRT, maskRT );
			}
			finally
			{
				commandList.ReleaseRenderTarget( blurredRT );
			}
		}
		finally
		{
			commandList.ReleaseRenderTarget( maskRT );
		}
	}

	private void Composite( RenderTargetHandle blurredRT, RenderTargetHandle maskRT )
	{
		RenderTargetHandle frameRT = commandList.Attributes.GrabFrameTexture( "SceneTexture" );

		commandList.Attributes.Set( "BlurredTexture", blurredRT.ColorTexture );
		commandList.Attributes.Set( "MaskTexture", maskRT.ColorTexture );
		commandList.Attributes.Set( "GlowIntensity", glowIntensity );
		commandList.Blit( compositeMaterial );
		commandList.ReleaseRenderTarget( frameRT );
	}

	private RenderTargetHandle BlurMaskRenderTarget()
	{
		Graphics.DownsampleMethod downSampleMethod = GetDownSampleMethod();

		// Create a copy of the mask render target to avoid modifying the original mask.
		RenderTargetHandle blurredRT = CreateMaskRenderTarget( MaskRTCopy );
		try
		{
			RenderTargetHandle tmpRT = commandList.GetRenderTarget( TmpTexture, ImageFormat.RGBA8888, 4, 1 );
			try
			{
				commandList.GenerateMipMaps( blurredRT, downSampleMethod );

				commandList.SetRenderTarget( tmpRT );
				commandList.Attributes.Set( "TextureToBlur", blurredRT.ColorTexture );
				commandList.Attributes.Set( "GlowSize", glowSize );
				commandList.Attributes.Set( "MipsLevel", glowMips );
				commandList.Blit( verticalBlurMaterial );
				commandList.ClearRenderTarget();

				commandList.GenerateMipMaps( tmpRT, downSampleMethod );

				commandList.SetRenderTarget( blurredRT );
				commandList.Attributes.Set( "VerticalBlurTexture", tmpRT.ColorTexture );
				commandList.Attributes.Set( "GlowSize", glowSize );
				commandList.Attributes.Set( "MipsLevel", glowMips );
				commandList.Blit( horizontalBlurMaterial );
				commandList.ClearRenderTarget();
			}
			finally
			{
				commandList.ReleaseRenderTarget( tmpRT );
			}
		}
		catch
		{
			commandList.ReleaseRenderTarget( blurredRT );
			throw;
		}

		commandList.ClearRenderTarget();

		return blurredRT;
	}

	private RenderTargetHandle CreateMaskRenderTarget( string name )
	{
		RenderTargetHandle maskRT = commandList.GetRenderTarget( name, ImageFormat.RGBA8888, 1, 1 );
				   
		try
		{
			commandList.SetRenderTarget( maskRT );
			commandList.Clear( Color.Transparent, true, true, true );

			for ( int i = 0; i < objectsToGlow.Count; i++ )
			{
				GlowObject glowObject = objectsToGlow[i];
				commandList.Attributes.Set( "GlowColor", glowObject.Color );

				if ( glowObject.GameObject == null ) continue;

				if ( glowObject.Renderer == null )
				{
					glowObject.SetRenderer( glowObject.GameObject.GetComponent<ModelRenderer>() );
					objectsToGlow[i] = glowObject;
				}

				commandList.DrawRenderer( glowObject.Renderer, maskRenderSetup );
			}

			commandList.SetRenderTarget( null );
		}
		catch
		{
			commandList.ReleaseRenderTarget( maskRT );
			throw;
		}

		return maskRT;
	}

	private void SetTransparentColorToDefault()
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].Color == Color.Transparent )
			{
				GlowObject glowObject = objectsToGlow[i];
				glowObject.SetColor( defaultGlowColor );
				objectsToGlow[i] = glowObject;
			}
		}
	}

	/// <summary>
	/// Changes the glow color of a specific GameObject.
	/// </summary>

	public void SetGlowColor( GameObject item, Color color )
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].GameObject != item ) continue;

			GlowObject glowObject = objectsToGlow[i];
			glowObject.SetColor( color );
			objectsToGlow[i] = glowObject;
			break;
		}
	}

	/// <summary>
	/// Returns the GlowObject for a specific GameObject if it exists.
	/// </summary>

	public GlowObject GetGlowObject( GameObject item )
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].GameObject == item ) return objectsToGlow[i];
		}

		return new GlowObject( null, null, null );
	}

	public List<GlowObject> GlowingObjects()
	{
		return objectsToGlow;
	}

	/// <summary>
	/// Adds a GameObject with the default glow color.
	/// </summary>
	public void Add( GameObject item )
	{
		Add( item, defaultGlowColor );
	}

	/// <summary>
	/// Tries to add a GameObject with a specific glow color. 
	/// Returns false if the GameObject is already present.
	/// </summary>
	public bool TryAdd( GameObject item, Color color )
	{
		bool itemExists = Contains( item );

		if ( itemExists ) return false;

		Add( item, color );

		return true;
	}

	/// <summary>
	/// Tries to add a GameObject with the default glow color. 
	/// Returns false if the GameObject is already present.
	/// </summary>
	public bool TryAdd( GameObject item )
	{
		return TryAdd( item, defaultGlowColor );
	}

	/// <summary>
	/// Adds a GameObject with the specified glow color and associated ModelRenderer.
	/// </summary>
	public void Add( GameObject item, Color color )
	{
		objectsToGlow.Add( new GlowObject( item, color, item.GetComponent<ModelRenderer>() ) );
	}

	/// <summary>
	/// Checks if the specified GameObject is already in the list.
	/// </summary>
	public bool Contains( GameObject item )
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].GameObject == item ) return true;
		}

		return false;
	}

	/// <summary>
	/// Removes the specified GameObject from the list, if it exists.
	/// </summary>
	public void Remove( GameObject item )
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].GameObject != item ) continue;

			RemoveAt( i );
			break;
		}
	}

	/// <summary>
	/// Removes the GameObject at the specified index.
	/// </summary>
	public void RemoveAt( int index )
	{
		objectsToGlow.RemoveAt( index );
	}

	/// <summary>
	/// Clears all GameObjects from the list.
	/// </summary>
	public void Clear()
	{
		objectsToGlow.Clear();
	}

	private Graphics.DownsampleMethod GetDownSampleMethod()
	{
		return (Graphics.DownsampleMethod)DownsampleMethod;
	}

	private void FindGlowableObjects()
	{
		IEnumerable<Glowable> glowableObjects = Scene.GetAll<Glowable>();

		foreach ( Glowable item in glowableObjects )
		{
			if ( !item.AddOnStart || Contains(item.GameObject)) continue;

			item.AddSelf( this );
		}
	}

	protected override void OnDisabled()
	{
		Instance = null;
		commandList.Reset();
	}
}

//WARNING: If you add / remove any fields from this struct, it will remove all objects in the list.
public struct GlowObject
{
	public GameObject GameObject { get; set; }
	[Hide]
	public Renderer Renderer { get; set; }

	[Description( "If kept transparent (#00000000) it will set it to default color automatically" )]
	public Color Color { get; set; } = Color.White;

	public GlowObject()
	{

	}

	public GlowObject( Color color )
	{
		Color = color;
	}

	public GlowObject( GameObject gameObject, Color color, Renderer renderer )
	{
		Color = color;
		GameObject = gameObject;
		Renderer = renderer;
	}

	/// <summary>
	/// Sets the renderer. Since this is a struct, changes won't stay unless you re-assign it.
	/// Example:
	/// <code>
	/// GlowObject copy = glowingObjects[i];
	/// copy.SetRenderer(GameObject.GetComponent(ModelRenderer));
	/// glowingObjects[i] = copy;
	/// </code>
	/// </summary>
	public void SetRenderer( Renderer renderer )
	{
		Renderer = renderer;
	}
	/// <summary>
	/// Sets the color. Since this is a struct, changes won't stay unless you re-assign it.
	/// Example:
	/// <code>
	/// GlowObject copy = glowingObjects[i];
	/// copy.SetColor(Color.Blue);
	/// glowingObjects[i] = copy;
	/// </code>
	/// </summary>
	public void SetColor( Color color )
	{
		Color = color;
	}
}
