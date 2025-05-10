using Sandbox;
using System.Collections.ObjectModel;

public sealed class MoveCameraWithKeyboard : Component
{
	protected override void OnUpdate()
	{
		Vector3 movementDirection = Input.AnalogMove;

		Vector3 wishAngle = Rotation.From( LocalRotation ) * movementDirection.Normal * 100;

		float cameraSprintSpeed = 1.0f;

		if ( Input.Down( "Run" ) )
		{
			cameraSprintSpeed = 5.0f;
		}

		Angles angles = LocalRotation.Angles();
		angles.roll = 0;
		angles += Input.AnalogLook;
		angles = angles.Normal;

		LocalRotation = angles;
		LocalPosition += wishAngle * cameraSprintSpeed * Time.Delta;
	}

}
