using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Client : MonoBehaviour
{
	public Server server;
	[SerializeField]
	private int delayMs;
	private int frame;


	private List<PlayerFrame> frameBacklog = new List<PlayerFrame>();
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
		foreach(var upd in from entity in list
						   join state in frameBacklog on entity.id equals state.state.entityId
						   select new { entity, state })
		{
			upd.entity.ResetState(upd.state.state);
		}
		frameBacklog.Clear();

		foreach(var ent in list)
		{
			if (ent.isController) ent.SimulateController();
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		foreach(var ent in list)
		{
			StartCoroutine(SendClientInputUpdateDelayed(ent.GetFrameInput(), frame));
		}
		frame++;
	}

	IEnumerator SendClientInputUpdateDelayed(PlayerInput input, int frame)
	{
		yield return new WaitForSeconds(delayMs/1000f);
		server.ClientInputUpdate(input, frame);
	}
}
