using Sandbox;

// This script is for you to attach to game objects you want to potentially glow.
// Example, you can attach this to a button so when you look at it, it glows.
// Basically just attach this component to game objects you might want to glow in the future.

[Icon( "Accessibility" )]
public sealed class Glowable : Component
{
	[Property]
	public Color GlowColor { get; set; } = Color.White;

	[Property, Description( "If true, on scene load it will automatically start glowing the object" )]
	public bool AddOnStart { get; set; } = false;

	public void SetColor( GlowOutline glowOutline )
	{
		glowOutline.SetGlowColor( GameObject, GlowColor );
	}
	public void SetColor( GlowOutline glowOutline, Color color )
	{
		glowOutline.SetGlowColor( GameObject, color );
	}

	public void RemoveSelf( GlowOutline glowOutline )
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

	public bool TryAddSelf( GlowOutline glowOutline )
	{
		return glowOutline.TryAdd( GameObject, GlowColor );
	}

	public bool TryAddSelf( GlowOutline glowOutline, Color color )
	{
		return glowOutline.TryAdd( GameObject, color );
	}
}
