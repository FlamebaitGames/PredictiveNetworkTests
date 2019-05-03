using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerState
{
	public int frame;
	public int entityId;
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 velocity;
	public Vector3 angularVelocity;
}

public struct PlayerInput
{
	public bool isValid;
	public int frame;
	public int entityId;
	public Vector3 input;
}

public struct PlayerFrame
{
	public int frame;
	public PlayerState state;
	public PlayerInput input;
}