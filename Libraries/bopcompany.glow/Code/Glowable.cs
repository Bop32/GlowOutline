using Sandbox;

// This script is for you to attach to game objects you want to potentially glow.
// Example, you can attach this to a button so when you look at it, it glows.
// Basically just attach this component to game objects you might want to glow in the future.

[Icon( "Accessibility" )]
public sealed class Glowable : Component
{
	[Property]
	public Color GlowColor { get; set; } = Color.White;

	[Property, Description("If true, it will start glowing when game is started. Otherwise nothing will happen.")]
	public bool AddOnStart { get; set; } = false;

	public GlowObject GlowObject => GlowOutline.Instance.GetGlowObject( GameObject );

	public void SetColor( Color color )
	{
		GlowOutline.Instance.SetGlowColor( GameObject, color );
	}

	public void SetColor()
	{
		GlowOutline.Instance.SetGlowColor( GameObject, GlowColor );
	}

	public void SetColor( GlowOutline glowOutline )
	{
		glowOutline.SetGlowColor( GameObject, GlowColor );
	}
	public void SetColor( GlowOutline glowOutline, Color color )
	{
		glowOutline.SetGlowColor( GameObject, color );
	}

	public void RemoveSelf()
	{
		GlowOutline.Instance.Remove( GameObject );
	}

	public void RemoveSelf(GlowOutline glowOutline)
	{
		glowOutline.Remove( GameObject );
	}

	public void AddSelf( GlowOutline glowOutline )
	{
		glowOutline.Add( GameObject, GlowColor );
	}

	public void AddSelf( GlowOutline glowOutline, Color color )
	{
		glowOutline.Add( GameObject, color );
	}

	public void AddSelf()
	{
		GlowOutline.Instance.Add( GameObject, GlowColor );
	}

	public void AddSelf( Color color )
	{
		GlowOutline.Instance.Add( GameObject, color );
	}

	public bool TryAddSelf( GlowOutline glowOutline )
	{
		return glowOutline.TryAdd( GameObject, GlowColor );
	}

	public bool TryAddSelf( GlowOutline glowOutline, Color color )
	{
		return glowOutline.TryAdd( GameObject, color );
	}

	public bool TryAddSelf()
	{
		return GlowOutline.Instance.TryAdd( GameObject, GlowColor );
	}

	public bool TryAddSelf( Color color )
	{
		return GlowOutline.Instance.TryAdd( GameObject, color );
	}
}
