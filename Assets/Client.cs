using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Client : MonoBehaviour
{
	public Server server;


	private List<PlayerFrame> frameBacklog = new List<PlayerFrame>();
	private struct FrameUpdate
	{
		public int frame;
		public PlayerState[] states;
	}

	public void ServerUpdate(PlayerFrame frame)
	{
		frameBacklog.Add(frame);
	}
	public void ServerUpdate(IEnumerable<PlayerFrame> frame)
	{
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

		foreach(var ent in list)
		{
			if (ent.isController) ent.SimulateController();
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		foreach(var ent in list)
		{
			server.ClientInputUpdate(ent.GetFrameInput());
		}
	}
}
