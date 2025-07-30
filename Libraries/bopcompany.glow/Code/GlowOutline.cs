using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Rendering;

[Icon( "Accessibility_New" )]
public sealed class GlowOutline : PostProcess
{
	public enum DownSampleMethods
	{
		Box = 0,
		GaussianBlur = 1,
		Max = 3,
		Min = 4,
	}

	[Property, Feature( "Glow Settings" ), Title( "Default Color" )]
	private Color glowColor = new( 0.10f, 0.32f, 0.79f, 1.00f );

	[Property, Range( 0.5f, 4.0f, 0.5f ), Description( "How big you want to the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowSize = 1.5f;

	[Property, Range( 0.25f, 1, 0.25f ), Description( "How bright you want the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowIntensity = 0.75f;

	[Header( "Glow Rendering" )]

	[Property, Title( "Image Downsample" ), Description( "Which downsample method to use when we downscale the texture." ), Feature( "Glow Settings" )]
	private DownSampleMethods DownsampleMethod { get; set; } = DownSampleMethods.GaussianBlur;

	[Property, Title( "Objects" ), Feature( "Objects to Glow" ), InlineEditor( Label = false )]
	private readonly List<GlowSettings> objectsToRender = null;

	private const string TmpTexture = "TmpTexture";
	private const string MaskRT = "MaskRT";
	private const string MaskRTCopy = "MaskRTCopy";

	private Material maskMaterial;
	protected override Stage RenderStage => Stage.AfterTransparent;
	protected override int RenderOrder => 100; // Don't think this matters but idk
	RendererSetup maskRenderSetup = default;
	public static GlowOutline Instance { get; private set; }

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

		SetTransparentColorToDefault();
		Instance = this;
	}

	protected override void UpdateCommandList()
	{
		RenderOutlineEffect();
	}

	private void RenderOutlineEffect()
	{
		if ( objectsToRender == null || objectsToRender.Count <= 0 ) return;

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

			for ( int i = 0; i < objectsToRender.Count; i++ )
			{
				GlowSettings glowObject = objectsToRender[i];
				CommandList.Attributes.Set( "GlowColor", glowObject.Color );

				if ( glowObject.GameObject == null ) continue;

				if ( glowObject.Renderer == null )
				{
					glowObject.SetRenderer( glowObject.GameObject.GetComponent<ModelRenderer>() );
					objectsToRender[i] = glowObject;
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
		for ( int i = 0; i < objectsToRender.Count; i++ )
		{
			if ( objectsToRender[i].Color == Color.Transparent )
			{
				GlowSettings glowSettings = objectsToRender[i];
				glowSettings.Color = glowColor;
				objectsToRender[i] = glowSettings;
			}
		}
	}

	private Graphics.DownsampleMethod GetDownSampleMethod()
	{
		return (Graphics.DownsampleMethod)DownsampleMethod;
	}

	public void ChangeColor( GameObject item, Color color )
	{
		for ( int i = 0; i < objectsToRender.Count; i++ )
		{
			if ( objectsToRender[i].GameObject != item ) continue;

			GlowSettings glowSettings = objectsToRender[i];
			glowSettings.Color = color;
			objectsToRender[i] = glowSettings;
			break;
		}
	}

	public void Add( GameObject item )
	{
		Add( item, glowColor );
	}

	public void Add( GameObject item, Color color )
	{
		objectsToRender.Add( new GlowSettings( item, color, item.GetComponent<ModelRenderer>() ) );
	}

	public void Remove( GameObject item )
	{
		for ( int i = 0; i < objectsToRender.Count; i++ )
		{
			if ( objectsToRender[i].GameObject != item ) continue;

			RemoveAt( i );
			break;
		}
	}

	public void RemoveAt( int index )
	{
		objectsToRender.RemoveAt( index );
	}

	public void Clear()
	{
		objectsToRender.Clear();
	}


	protected override void OnDisabled()
	{
		Instance = null;
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

	public void SetRenderer( Renderer renderer )
	{
		Renderer = renderer;
	}
}
