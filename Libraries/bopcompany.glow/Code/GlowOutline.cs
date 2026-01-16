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
	}

	public enum OutlinePresets : byte
	{
		Valve,
		Other,
	}

	[Property, Feature( "Glow Settings" ), Title( "Default Color" )]
	private Color defaultGlowColor = new( 0.10f, 0.32f, 0.79f, 1.00f );

	[Property, Range( 0, 3 ), Step( 1 ), Description( "Changes the resolution of the blur (higher value means lower quality)" ), Feature( "Glow Settings" )]
	private int glowMips = 2;

	[Property, Range( 0.25f, 10.0f ), Step( 0.25f ), Description( "How bright you want the glow to be." ), Feature( "Glow Settings" )]
	private float glowIntensity = 5.0f;

	[Property, Feature( "Glow Settings" ), Description( "If enabled, automatically finds all objects with a `Glowable` component and applies a glow effect." )]
	private readonly bool autoFindGlowables = false;

	[Header( "Glow Rendering" )]

	[Property, Title( "Glow Types" ), Description( "Changes the downsample method that is used to create different outline effects." ), Feature( "Glow Settings" )]
	private DownSampleMethods DownSampleMethod { get; set; } = DownSampleMethods.GaussianBlur;

	[Property, Title( "Glow Presets" ), Description( "Some presets if you are lazy." ), Feature( "Glow Settings" )]
	private OutlinePresets OutlinePreset { get; set; } = OutlinePresets.Valve;

	[Property, Title( "Glow Presets" ), Button( "Apply Preset" ), Feature( "Glow Settings" )]
	public void ApplyPreset()
	{
		if ( OutlinePreset == OutlinePresets.Valve )
		{
			glowMips = 2;
			DownSampleMethod = DownSampleMethods.GaussianBlur;
			glowIntensity = 5.0f;
		}
		else
		{
			glowMips = 1;
			DownSampleMethod = DownSampleMethods.Box;
			glowIntensity = 5.0f;
		}
	}

	[Property, Title( "Objects" ), Feature( "Objects to Glow" ), InlineEditor( Label = false )]
	private List<GlowObject> objectsToGlow = null;

	private const string MASK_RENDER_TARGET = "MaskRT";

	private const string MASK_RENDER_TARGET_COPY = "MaskRTCopy";

	private const string GLOW_COLOR_ATTRIBUTE = "GlowColor";

	private readonly Material maskMaterial = Material.FromShader( "shaders/Mask.shader" );
	private readonly Material compositeMaterial = Material.FromShader( "shaders/Composite.shader" );
	private readonly CommandList commandList = new( "GlowOutline" );

	RendererSetup maskRenderSetup = default;
	public int Count => objectsToGlow.Count;

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

		objectsToGlow ??= new();

		SetTransparentColorToDefault();
	}

	protected override void OnStart()
	{
		if ( !autoFindGlowables ) return;

		FindGlowableObjects();
	}

	public override void Render()
	{
		if ( Count <= 0 ) return;

		commandList.Reset();
		RenderOutlineEffect();
		InsertCommandList( commandList, Stage.AfterTransparent, 100, "GlowOutline" );
	}

	private void RenderOutlineEffect()
	{
		RenderTargetHandle maskRT = CreateMaskRenderTarget( MASK_RENDER_TARGET, 1 );

		try
		{
			RenderTargetHandle downScaledRT = DownScaledRenderTarget();
			try
			{
				Composite( downScaledRT, maskRT );
			}
			finally
			{
				commandList.ReleaseRenderTarget( downScaledRT );
			}
		}
		finally
		{
			commandList.ReleaseRenderTarget( maskRT );
		}
	}

	private void Composite( RenderTargetHandle downScaledRT, RenderTargetHandle maskRT )
	{
		RenderTargetHandle frameRT = commandList.Attributes.GrabFrameTexture( "SceneTexture" );

		commandList.Attributes.Set( "DownScaledTexture", downScaledRT.ColorTexture );
		commandList.Attributes.Set( "MaskTexture", maskRT.ColorTexture );
		commandList.Attributes.Set( "GlowIntensity", glowIntensity );
		commandList.Attributes.Set( "GlowMips", glowMips );
		commandList.Blit( compositeMaterial );
		commandList.ReleaseRenderTarget( frameRT );
	}

	private RenderTargetHandle DownScaledRenderTarget()
	{
		RenderTargetHandle downScaledRT = CreateMaskRenderTarget( MASK_RENDER_TARGET_COPY, 4 );

		try
		{
			// Generate mipmaps (just downscales the texture)
			commandList.GenerateMipMaps( downScaledRT, GetDownSampleMethod() );

			return downScaledRT;
		}
		catch
		{
			commandList.ReleaseRenderTarget( downScaledRT );
			throw;
		}
	}

	private RenderTargetHandle CreateMaskRenderTarget( string name, int mipsLevel = 1 )
	{
		RenderTargetHandle maskRT = commandList.GetRenderTarget( name, ImageFormat.RGBA8888, mipsLevel, 1 );

		try
		{
			commandList.SetRenderTarget( maskRT );
			commandList.Clear( Color.Transparent, true, true, true );

			for ( int i = 0; i < objectsToGlow.Count; i++ )
			{
				GlowObject glowObject = objectsToGlow[i];

				if ( glowObject.GameObject == null ) continue;

				commandList.Attributes.Set( GLOW_COLOR_ATTRIBUTE, glowObject.Color );
				glowObject.Renderer ??= glowObject.GameObject.GetComponent<ModelRenderer>();
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
			if ( objectsToGlow[i].Color != Color.Transparent ) continue;

			objectsToGlow[i].Color = defaultGlowColor;
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

			objectsToGlow[i].Color = color;
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
		return (Graphics.DownsampleMethod)DownSampleMethod;
	}

	private void FindGlowableObjects()
	{
		IEnumerable<Glowable> glowableObjects = Scene.GetAll<Glowable>();

		foreach ( Glowable item in glowableObjects )
		{
			if ( !item.AddOnStart || Contains( item.GameObject ) ) continue;

			item.AddSelf( this );
		}
	}

	protected override void OnDisabled()
	{
		commandList.Reset();
	}
}

//WARNING: If you add / remove any fields from this class, it will remove all objects in the list.
public class GlowObject
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
}
