using Sandbox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public sealed class GlowOutline : PostProcess
{
	public enum DownSampleMethods
	{
		Box = 0,
		GaussianBlur = 1,
		Max = 3,
		Min = 4,
	}

	[Property, Title( "Objects" ), Feature( "Objects to Glow" ), InlineEditor( Label = false )]

	// The reason it is an ObservableCollection is so that we can use the CollectionChanged event to update the SceneObject when we add/remove objects.
	// Might just change this to a list not sure yet, I think it's nice to preview how glow can look on a object while you are in the editor
	private readonly ObservableCollection<GlowSettings> objectsToRender = null;

	[Property, Feature( "Glow Settings" ), Title( "Default Color" )]
	private Color glowColor = new( 0.10f, 0.32f, 0.79f, 1.00f );

	[Property, Range( 1.0f, 4.0f, 0.50f ), Description( "How big you want to the glow to be." ), Feature( "Glow Settings" )]
	private readonly float glowSize = 2f;

	[Property, Range( 0.25f, 15, 0.25f ), Description( "How bright you want the glow to be." ), Feature( "Glow Settings" )]
	private float glowIntensity = 15.0f;

	[Header( "Glow Rendering" )]

	[Property, Title( "Blur Amount" ), Description( "Controls the amount of blur on the edges (Higher = more blur).\r\nSetting this too high may impact performance." ), Range( 1, 10, 1 ), Feature( "Glow Settings" )]
	private int iterativeBlurCount { get; set; } = 1;

	[Property, Title( "Image Downsample" ), Description( "Which downsample method to use when we downscale the texture." ), Feature( "Glow Settings" )]
	private readonly DownSampleMethods downsampleMethod = DownSampleMethods.GaussianBlur;

	private IDisposable renderHook;
	private static RenderAttributes renderAttributes;

	private static Texture blurredTexture;
	private static Texture tmpTexture;
	private static Texture maskRT;

	private Material maskMaterial;

	private IReadOnlyCollection<SceneObject> sceneObjects;

	private bool ignoreCollectionChangedEvent = false;

	protected override void OnStart()
	{
		if ( Screen.Size.x % 2 == 1 || Screen.Size.y % 2 == 1 )
		{
			Log.Warning( $"Glow Outline Warning: To avoid uneven outlines, please use a screen resolution that is even. Current resolution: `{Screen.Size}`." );
		}

		blurredTexture = null;
		tmpTexture = null;
		maskRT = null;

		renderHook = Camera.AddHookAfterTransparent( "RenderOutlineEffect", 0, RenderOutlineEffect );
		renderAttributes = new RenderAttributes();
		sceneObjects = [];
		objectsToRender.CollectionChanged += HandleCollectionChangedEvent;
		maskMaterial = Material.FromShader( "shaders/GlowMask.shader" );
	}

	private void RenderOutlineEffect( SceneCamera sceneCamera )
	{
		if ( objectsToRender == null || objectsToRender.Count <= 0 )
		{
			Log.Error( "Error: Trying to glow objects when you have no objects added!" );
			return;
		}

		//Needed since OnStart won't set sceneObjects correctly which is dumb
		if ( sceneObjects.Count == 0 )
		{
			//Checks the objectsToGlow list then finds the SceneObject of that object
			InitalizeSceneObjects();
		}

		CreateMaskTexture();
		BlurMaskTexture( maskRT );
		Composite( maskRT );
	}

	private void Composite( Texture maskRT )
	{
		//Sets SceneTexture in the Composite shader.
		using RenderTarget cameraRT = Graphics.GrabFrameTexture( "SceneTexture" );

		//renderAttributes.Clear();
		renderAttributes.Set( "BlurredTexture", blurredTexture );
		renderAttributes.Set( "SilhouetteTexture", maskRT );
		renderAttributes.Set( "GlowIntensity", glowIntensity );

		Graphics.Blit( Material.FromShader( "shaders/Composite.shader" ), renderAttributes );
	}

	private Graphics.DownsampleMethod GetDownSampleMethod()
	{
		return (Graphics.DownsampleMethod)downsampleMethod;
	}

	// After we find the objects we want to glow and it's in a mask, from that mask we copy it to blurredTexture so that it can blur.
	private void BlurMaskTexture( Texture objectMask )
	{
		EnsureBlurTexturesIsCreated( objectMask );

		Graphics.CopyTexture( objectMask, blurredTexture );

		Graphics.DownsampleMethod downSampleMethod = GetDownSampleMethod();

		for ( int i = 0; i < iterativeBlurCount; i++ )
		{
			Graphics.GenerateMipMaps( blurredTexture, downSampleMethod, 0, 1 );

			Graphics.RenderTarget = RenderTarget.From( tmpTexture );
			renderAttributes.Set( "Size", blurredTexture.Size );
			renderAttributes.Set( "TextureToBlur", blurredTexture );
			renderAttributes.Set( "GlowSize", glowSize );
			Graphics.Blit( Material.FromShader( "shaders/BlurVertical.shader" ), renderAttributes );
			Graphics.RenderTarget = null;

			Graphics.GenerateMipMaps( tmpTexture, downSampleMethod, 0, 1 );

			Graphics.RenderTarget = RenderTarget.From( blurredTexture );
			renderAttributes.Set( "Size", tmpTexture.Size );
			renderAttributes.Set( "VerticalBlurTexture", tmpTexture );
			renderAttributes.Set( "GlowSize", glowSize );
			Graphics.Blit( Material.FromShader( "shaders/BlurHorizontal.shader" ), renderAttributes );
			Graphics.RenderTarget = null;
		}
	}

	//Function is here since we can only create the texture once and inform the GPU we will update it often improving perforamnce (I hope).
	private static void EnsureBlurTexturesIsCreated( Texture colorTarget )
	{
		if ( blurredTexture == null || blurredTexture.Size != colorTarget.Size )
		{
			blurredTexture = Texture.CreateRenderTarget()
			.WithFormat( ImageFormat.RGBA8888 )
			.WithUAVBinding()
			.WithDynamicUsage()
			.WithMips( 2 )
			.WithSize( colorTarget.Size )
			.Create( "TextureToBlur" );

			tmpTexture = Texture.CreateRenderTarget()
			.WithFormat( ImageFormat.RGBA8888 )
			.WithUAVBinding()
			.WithDynamicUsage()
			.WithMips( 2 )
			.WithSize( blurredTexture.Size )
			.Create( "TmpTexture" );
		}
	}

	//Finds objects we want to glow and create a mask out of it.
	private void CreateMaskTexture()
	{
		EnsureMaskTextureIsCreated();

		Graphics.RenderTarget = RenderTarget.From( maskRT );

		Graphics.Clear( Color.Transparent, true, true, true );

		foreach ( GlowSettings glowObject in objectsToRender )
		{
			Graphics.Attributes.Set( "GlowColor", glowObject.Color );
			Graphics.Render( glowObject.SceneObject, null, null, maskMaterial );
		}

		Graphics.RenderTarget = null;
	}

	private static void EnsureMaskTextureIsCreated()
	{
		if ( maskRT != null && maskRT.Size == Screen.Size ) return;

		maskRT = Texture.CreateRenderTarget()
				.WithFormat( ImageFormat.RGBA8888 )
				.WithSize( Screen.Size )
				.WithDynamicUsage()
				.Create( "MaskTest" );

	}

	private void InitalizeSceneObjects()
	{
		RefreshSceneObjects();

		foreach ( SceneObject sceneObject in sceneObjects )
		{
			GameObject gameObject = sceneObject.GetGameObject();
			for ( int i = 0; i < objectsToRender.Count; i++ )
			{
				GlowSettings glowObjectSettings = objectsToRender[i];

				if ( gameObject != glowObjectSettings.GameObject ) continue;

				glowObjectSettings.SceneObject = sceneObject;
				objectsToRender[i] = glowObjectSettings;
				break;
			}
		}
	}

	private void HandleCollectionChangedEvent( object sender, NotifyCollectionChangedEventArgs e )
	{
		if ( ignoreCollectionChangedEvent ) return;

		RefreshSceneObjects();

		if ( e.NewStartingIndex == e.OldStartingIndex )
		{
			foreach ( SceneObject sceneObject in sceneObjects )
			{
				GameObject gameObject = sceneObject.GetGameObject();

				for ( int i = 0; i < objectsToRender.Count; i++ )
				{
					GlowSettings glowObjectSettings = objectsToRender[i];

					if ( gameObject != glowObjectSettings.GameObject ) continue;

					// Basically if we modify anything in our ObservableCollection we need to ignore the event
					ignoreCollectionChangedEvent = true;
					try
					{
						glowObjectSettings.SceneObject = sceneObject;
						glowObjectSettings.Color = GetGlowColor( glowObjectSettings.Color );
						objectsToRender[i] = glowObjectSettings;
					}
					finally
					{
						ignoreCollectionChangedEvent = false;
					}
					break;
				}
			}
		}
	}

	private Color GetGlowColor( Color objectGlowColor )
	{
		return objectGlowColor == Color.Transparent ? glowColor : objectGlowColor;
	}

	private void RefreshSceneObjects()
	{
		// Can be quite slow since we are using Linq + IsValid + ToArray()
		IReadOnlyCollection<SceneObject> worldSceneObjects = Scene.SceneWorld.SceneObjects;
		if ( sceneObjects.Count != worldSceneObjects.Count )
		{
			sceneObjects = worldSceneObjects;
		}
	}

	public void Add( GameObject item )
	{
		Add( item, glowColor );
	}

	public void Add( GameObject item, Color color )
	{
		RefreshSceneObjects();

		foreach ( SceneObject obj in sceneObjects )
		{
			GameObject gameObject = obj.GetGameObject();

			if ( gameObject != item ) continue;

			ignoreCollectionChangedEvent = true;
			try
			{
				objectsToRender.Add( new GlowSettings( gameObject, color, obj ) );
			}
			finally
			{
				ignoreCollectionChangedEvent = false;
			}
		}
	}

	public void Remove( GameObject item )
	{
		RefreshSceneObjects();

		for ( int i = objectsToRender.Count - 1; i >= 0; i-- )
		{
			if ( objectsToRender[i].GameObject != item ) continue;

			RemoveAt( i );
			break;
		}
	}

	public void RemoveAt( int index )
	{
		ignoreCollectionChangedEvent = true;

		try
		{
			objectsToRender.RemoveAt( index );
		}
		finally
		{
			ignoreCollectionChangedEvent = false;
		}
	}

	public void Clear()
	{
		ignoreCollectionChangedEvent = true;

		try
		{
			objectsToRender.Clear();
		}
		finally
		{
			ignoreCollectionChangedEvent = false;
		}
	}

	protected override void OnDisabled()
	{
		renderHook?.Dispose();
		renderHook = null;

		blurredTexture = null;
		tmpTexture = null;
		maskRT = null;
	}
}

//WARNING: IF YOU MDIFY THIS STRUCTURE WITH ACTIVE GAMEOBJECTS IN THE LIST IT WILL CLEAR THE ENTIRE LIST
public struct GlowSettings
{
	[Hide]
	public SceneObject SceneObject;
	public GameObject GameObject { get; set; }

	[Description( "If kept transparent (#00000000) it will set it to default color automatically" )]
	public Color Color { get; set; }

	public GlowSettings( Color color, SceneObject sceneObject = null )
	{
		Color = color;
		SceneObject = sceneObject;
	}

	public GlowSettings( GameObject gameObject, Color color, SceneObject sceneObject = null )
	{
		Color = color;
		SceneObject = sceneObject;
		GameObject = gameObject;
	}
}

