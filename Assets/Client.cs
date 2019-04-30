using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Client : MonoBehaviour
{
	public Server server;
	[SerializeField]
	private int delayMs;
	public int frame;
	private int serverFrame;
	private BucketList<PlayerFrame> frameBucket = new BucketList<PlayerFrame>();
	private List<PlayerFrame> frameBacklog = new List<PlayerFrame>();
	private List<PlayerInput> previousInput = new List<PlayerInput>();
	private struct FrameUpdate
	{
		public int frame;
		public PlayerState[] states;
	}
	public void ServerUpdate(PlayerFrame[] frame)
	{
		StartCoroutine(ReceiveUpdateDelayed(frame));
	}

	IEnumerator ReceiveUpdateDelayed(PlayerFrame[] frame)
	{
		yield return new WaitForSeconds(delayMs/1000f);
		frameBacklog.AddRange(frame);
	}

	private void FixedUpdate()
	{
		var list = new List<PlayerEntity>();
		foreach(var ent in gameObject.scene.GetRootGameObjects())
		{
			var comp = ent.GetComponent<PlayerEntity>();
			if (comp)
			{
				list.Add(comp);
			}
		}
		foreach (var upd in from entity in list
							join state in frameBacklog on entity.id equals state.state.entityId
							select new { entity, state })
		{
			//Debug.Log($"{Vector3.Distance(upd.state.state.position, upd.entity.rigidbody.position)}) < 1f");
			if (upd.entity.isController &&
				(Vector3.Distance(upd.state.state.position, upd.entity.rigidbody.position) < 1f) //upd.state.state.velocity.magnitude * Time.fixedDeltaTime
				 //&& Quaternion.Angle(upd.state.state.rotation, upd.entity.transform.rotation) < 1f
				 ) continue;
			upd.entity.ResetState(upd.state.state);
		}

		frameBacklog.Clear();

		foreach(var ent in list)
		{
			if (ent.isController)
			{
				ent.SimulateController(frame);
				ent.ExecuteCommand();
			}
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		foreach(var ent in list)
		{
			if(ent.isController) StartCoroutine(SendClientInputUpdateDelayed(ent.GetFrameInput(), frame));
		}
		frame++;
	}

	IEnumerator SendClientInputUpdateDelayed(PlayerInput input, int frame)
	{
		yield return new WaitForSeconds(delayMs/1000f);
		server.ClientInputUpdate(input, frame);
	}
}
