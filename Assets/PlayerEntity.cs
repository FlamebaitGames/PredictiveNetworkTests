using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
	[SerializeField]
	private new Rigidbody rigidbody;
	public bool isController;
	public int id;
	private void OnValidate()
	{
		rigidbody = GetComponent<Rigidbody>();
	}
	private PlayerInput frameInput;
	public PlayerInput GetFrameInput()
	{
		return frameInput;
	}
	public PlayerState GetFrameState()
	{
		return new PlayerState
		{
			entityId = id,
			position = rigidbody.position,
			rotation = rigidbody.rotation,
			velocity = rigidbody.velocity,
			angularVelocity = rigidbody.angularVelocity
		};
	}
	public void SimulateOwner()
	{
		
	}
	public void SimulateController()
	{
		frameInput = new PlayerInput { entityId = id, input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized };
		Step(frameInput);
	}

	public void ResetState(PlayerState state)
	{
		rigidbody.position = state.position;
		rigidbody.rotation = state.rotation;
		rigidbody.velocity = state.velocity;
		rigidbody.angularVelocity = state.angularVelocity;
	}

	public void Step(PlayerInput input)
	{
		rigidbody.AddForce(input.input * 150f * Time.fixedDeltaTime);
		if (rigidbody.position.magnitude > 7f)
		{
			rigidbody.AddForce(-rigidbody.position, ForceMode.Acceleration);
		}
	}

	public void Replicate(PlayerState state)
	{
		rigidbody.position = state.position;
		rigidbody.rotation = state.rotation;
		rigidbody.velocity = state.velocity;
		rigidbody.angularVelocity = state.angularVelocity;
	}
}
