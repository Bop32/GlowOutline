using Sandbox;
using System;
public sealed class AddGlow : Component
{
	[Property]
	public List<GameObject> objectsToGlow = new();

	[Property]
	private readonly GameObject center = null;

	[Property]
	private GameObject parent;
	protected override void OnStart()
	{
		//for ( int i = 0; i < 100; i++ )
		//{
		//	int random = Random.Shared.Next( 0, objectsToGlow.Count );

		//	GameObject gameObject = objectsToGlow[random].Clone();
		//	gameObject.Parent = parent;
		//	gameObject.WorldPosition = center.WorldPosition + Random.Shared.VectorInSphere( 400 );
		//	gameObject.WorldRotation = Rotation.Random;

		//	GlowOutline.Instance.Add( gameObject, new Color( Random.Shared.Float( 1.1f ), Random.Shared.Float( 1.1f ), Random.Shared.Float( 0, 1 ) ) );
		//}
	}
}
