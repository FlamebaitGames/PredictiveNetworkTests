using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	private new Rigidbody rigidbody;
	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}
	private void FixedUpdate()
	{
		var input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		input.Normalize();
		rigidbody.AddForce(input * 150f * Time.fixedDeltaTime);
		if(rigidbody.position.magnitude > 7f)
		{
			rigidbody.AddForce(-rigidbody.position, ForceMode.Acceleration);
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
	}
}
