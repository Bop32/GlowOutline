using Sandbox;

// This script is for you to attach to game objects you want to potentially glow.
// Example, you can attach this to a button so when you look at it, it glows.
// Basically just attach this component to game objects you might want to glow in the future.

[Icon( "Accessibility" )]
public sealed class Glowable : Component
{
	public GlowSettings GlowSettings => GlowOutline.Instance.GetGlowObject( GameObject );
}
