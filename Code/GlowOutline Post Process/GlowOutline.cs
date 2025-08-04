using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Rendering;

[Icon( "Accessibility_New" )]
public sealed class GlowOutline : PostProcess
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

	[Property, Range( 0.5f, 4.0f ), Step( 0.5f ), Description( "How big you want to the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowSize = 1.5f;

	[Property, Range( 0.25f, 1 ), Step( 0.25f ), Description( "How bright you want the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowIntensity = 0.75f;

	[Header( "Glow Rendering" )]

	[Property, Title( "Image Downsample" ), Description( "Which downsample method to use when we downscale the texture." ), Feature( "Glow Settings" )]
	private DownSampleMethods DownsampleMethod { get; set; } = DownSampleMethods.GaussianBlur;

	[Property, Title( "Objects" ), Feature( "Objects to Glow" ), InlineEditor( Label = false )]
	private List<GlowSettings> objectsToGlow = null;

	private const string TmpTexture = "TmpTexture";

	private const string MaskRT = "MaskRT";

	private const string MaskRTCopy = "MaskRTCopy";

	private Material maskMaterial;

	RendererSetup maskRenderSetup = default;
	public static GlowOutline Instance { get; private set; }
	public int GlowCount => objectsToGlow.Count;
	protected override Stage RenderStage => Stage.AfterTransparent;
	protected override int RenderOrder => 100; // Don't think this matters but idk

	protected override void OnAwake()
	{
		// This SHOULD only happen in the editor since people like to change editor width and height etc.
		if ( Screen.Size.x % 2 == 1 || Screen.Size.y % 2 == 1 )
		{
			Log.Error( $"Glow Outline: To avoid uneven outlines, please use a screen resolution that is even. Current resolution: `{Screen.Size}`." );
		}

		maskMaterial = Material.FromShader( "shaders/Mask.shader" );
		maskRenderSetup = new RendererSetup
		{
			Material = maskMaterial,
			Transform = null,
			Color = null
		};

		if ( objectsToGlow == null ) objectsToGlow = new();

		SetTransparentColorToDefault();
		Instance = this;
	}

	protected override void UpdateCommandList()
	{
		RenderOutlineEffect();
	}

	private void RenderOutlineEffect()
	{
		if ( objectsToGlow.Count <= 0 ) return;

		RenderTargetHandle maskRT = CreateMaskRenderTarget( MaskRT );

		try
		{
			RenderTargetHandle blurredRT = BlurMaskRenderTarget();
			try
			{
				Composite( blurredRT, maskRT );
			}
			finally
			{
				CommandList.ReleaseRenderTarget( blurredRT );
			}
		}
		finally
		{
			CommandList.ReleaseRenderTarget( maskRT );
		}
	}

	private void Composite( RenderTargetHandle blurredRT, RenderTargetHandle maskRT )
	{
		//Sets SceneTexture in the Composite shader.
		RenderTargetHandle frameRT = CommandList.Attributes.GrabFrameTexture( "SceneTexture" );

		CommandList.Attributes.Set( "BlurredTexture", blurredRT.ColorTexture );
		CommandList.Attributes.Set( "MaskTexture", maskRT.ColorTexture );
		CommandList.Attributes.Set( "GlowIntensity", glowIntensity );
		CommandList.Blit( Material.FromShader( "shaders/Composite.shader" ) );
		CommandList.ReleaseRenderTarget( frameRT );
	}

	private RenderTargetHandle BlurMaskRenderTarget()
	{
		Graphics.DownsampleMethod downSampleMethod = GetDownSampleMethod();

		// Create a copy of the mask render target to avoid modifying the original mask.
		RenderTargetHandle blurredRT = CreateMaskRenderTarget( MaskRTCopy );
		try
		{
			RenderTargetHandle tmpRT = CommandList.GetRenderTarget( TmpTexture, ImageFormat.RGBA8888, 2, 1 );
			try
			{

				CommandList.GenerateMipMaps( blurredRT, downSampleMethod );

				CommandList.SetRenderTarget( tmpRT );
				CommandList.Attributes.Set( "TextureToBlur", blurredRT.ColorTexture );
				CommandList.Attributes.Set( "GlowSize", glowSize );
				CommandList.Blit( Material.FromShader( "shaders/BlurVertical.shader" ) );
				CommandList.ClearRenderTarget();

				CommandList.GenerateMipMaps( tmpRT, downSampleMethod );

				CommandList.SetRenderTarget( blurredRT );
				CommandList.Attributes.Set( "VerticalBlurTexture", tmpRT.ColorTexture );
				CommandList.Attributes.Set( "GlowSize", glowSize );
				CommandList.Blit( Material.FromShader( "shaders/BlurHorizontal.shader" ) );
				CommandList.ClearRenderTarget();
			}
			finally
			{
				CommandList.ReleaseRenderTarget( tmpRT );
			}
		}
		catch
		{
			CommandList.ReleaseRenderTarget( blurredRT );
			throw;
		}

		CommandList.ClearRenderTarget();

		return blurredRT;
	}

	private RenderTargetHandle CreateMaskRenderTarget( string name )
	{
		RenderTargetHandle maskRT = CommandList.GetRenderTarget( name, ImageFormat.RGBA8888, 2, 1 );

		try
		{
			CommandList.SetRenderTarget( maskRT );
			CommandList.Clear( Color.Transparent, true, true, true );

			for ( int i = 0; i < objectsToGlow.Count; i++ )
			{
				GlowSettings glowObject = objectsToGlow[i];
				CommandList.Attributes.Set( "GlowColor", glowObject.Color );

				if ( glowObject.GameObject == null ) continue;

				if ( glowObject.Renderer == null )
				{
					glowObject.SetRenderer( glowObject.GameObject.GetComponent<ModelRenderer>() );
					objectsToGlow[i] = glowObject;
				}

				CommandList.DrawRenderer( glowObject.Renderer, maskRenderSetup );
			}

			CommandList.SetRenderTarget( null );
		}
		catch
		{
			CommandList.ReleaseRenderTarget( maskRT );
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
				GlowSettings glowSettings = objectsToGlow[i];
				glowSettings.SetColor( defaultGlowColor );
				objectsToGlow[i] = glowSettings;
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

			GlowSettings glowSettings = objectsToGlow[i];
			glowSettings.SetColor( color );
			objectsToGlow[i] = glowSettings;
			break;
		}
	}

	/// <summary>
	/// Returns the GlowSettings for a specific GameObject if it exists.
	/// </summary>

	public GlowSettings GetGlowObject( GameObject item )
	{
		for ( int i = 0; i < objectsToGlow.Count; i++ )
		{
			if ( objectsToGlow[i].GameObject == item ) return objectsToGlow[i];
		}

		return new GlowSettings( null, null, null );
	}

	public List<GlowSettings> GlowingObjects()
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
		objectsToGlow.Add( new GlowSettings( item, color, item.GetComponent<ModelRenderer>() ) );
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

	protected override void OnDisabled()
	{
		Instance = null;
		CommandList.Reset();
	}
}

//WARNING: If you add / remove any fields from this struct, it will remove all objects in the list.
public struct GlowSettings
{
	public GameObject GameObject { get; set; }
	[Hide]
	public Renderer Renderer { get; set; }

	[Description( "If kept transparent (#00000000) it will set it to default color automatically" )]
	public Color Color { get; set; } = Color.White;

	public GlowSettings()
	{

	}

	public GlowSettings( Color color )
	{
		Color = color;
	}

	public GlowSettings( GameObject gameObject, Color color, Renderer renderer )
	{
		Color = color;
		GameObject = gameObject;
		Renderer = renderer;
	}

	/// <summary>
	/// Sets the renderer. Since this is a struct, changes won't stay unless you re-assign it.
	/// Example:
	/// <code>
	/// GlowSettings copy = glowingObjects[i];
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
	/// GlowSettings copy = glowingObjects[i];
	/// copy.SetColor(Color.Blue);
	/// glowingObjects[i] = copy;
	/// </code>
	/// </summary>
	public void SetColor( Color color )
	{
		Color = color;
	}
}
