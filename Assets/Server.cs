﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Server : MonoBehaviour
{
	[SerializeField]
	private GameObject playerPrefab;
	[SerializeField]
	private Camera playerCamera;
	private int[] layers;
	private int nClients;
	private List<PlayerInput> inputBacklog = new List<PlayerInput>();
	private BucketList bucketInput = new BucketList();
	public int frame = 0;

	private List<Client> clients = new List<Client>();
	private Scene[] scenes;
	private void Awake()
	{
		layers = new[] { 0, 1, 2, 3, 4, 5 }.Select(n => LayerMask.NameToLayer($"Client{n}")).ToArray();
		scenes = new Scene[6];
	}
	private void Start()
	{
		ClientJoin();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return)) ClientJoin();
	}

	private void FixedUpdate()
	{
		var entities = (from ent in gameObject.scene.GetRootGameObjects()
						let comp = ent.GetComponent<PlayerEntity>()
						where comp
						select comp).ToArray();
		bucketInput.CreateBucket(frame, entities.Select(e => e.GetFrameState()).ToArray());
		int earliestRecievedFrame = frame;
		foreach(var input in inputBacklog)
		{
			if (!bucketInput.Exists(input.frame))
			{
				Debug.LogWarning($"Received bad input for frame {input.frame} (entityId: {input.entityId})");
				continue;
			}
			bucketInput.Add(input, input.frame);
			earliestRecievedFrame = Mathf.Min(input.frame, earliestRecievedFrame);
		}
		inputBacklog.Clear();
		if(earliestRecievedFrame < frame)
		{
			Debug.Log($"Rewinding from frame: {earliestRecievedFrame}");
			foreach(var ent in from e in entities
							   join i in bucketInput.GetContext(earliestRecievedFrame) on e.id equals i.entityId
							   select new { entity = e, state = i })
			{
				Debug.Log($"entityId: {ent.entity.id}, frame: {ent.state.frame}");
				ent.entity.ResetState(ent.state);
			}
			for(int i = earliestRecievedFrame; i < frame; i++)
			{
				foreach (var ent in from e in entities
									join inp in bucketInput.GetInputEnumerator(i) on e.id equals inp.entityId
									select new { entity = e, input = inp })
				{
					ent.entity.SetInput(ent.input);
					ent.entity.ExecuteCommand();
				}
				gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
			}
		}

		foreach (var ent in from e in entities
							join inp in bucketInput.GetInputEnumerator(frame) on e.id equals inp.entityId
							select new { entity = e, input = inp })
		{
			if (ent.entity.isController) continue;
			ent.entity.SetInput(ent.input);
		}
		foreach (var ent in entities)
		{
			if (ent.isController) ent.SimulateController(frame);
		}
		foreach (var ent in entities)
		{
			ent.ExecuteCommand();
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		bucketInput.Trim(p => p.context.Length == p.input.Count+1); // Clear completed buckets
		frame++;
		foreach(var client in clients)
		{
			client.ServerUpdate(
				(from ent in entities
				 let input = ent.GetFrameInput()
				 let state = ent.GetFrameState()
				 select new PlayerFrame
				 {
					 frame = frame,
					 input = new PlayerInput
					 {
						 frame = frame,
						 input = input.input,
						 entityId = input.entityId
					 },
					 state = new PlayerState
					 {
						 frame = frame,
						 position = state.position,
						 rotation = state.rotation,
						 velocity = state.velocity,
						 angularVelocity = state.angularVelocity,
						 entityId = state.entityId
					}
				})
				.ToArray());
		}
		//list.Select(ent => new PlayerFrame
		//{
		//	frame = frame,
		//	input = ent.GetFrameInput(),
		//	state = ent.GetFrameState()
		//}
	}

	public void ClientInputUpdate(PlayerInput input, int frame)
	{
		input.frame = frame;
		inputBacklog.Add(input);
	}

	public void ClientJoin()
	{
		var scene = SceneManager.CreateScene($"Client Scene ({nClients})", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
		scenes[nClients] = scene;
		if(nClients == 0)
		{
			SceneManager.MoveGameObjectToScene(gameObject, scene);
		}
		else
		{
			var client = new GameObject("Client");
			SceneManager.MoveGameObjectToScene(client, scene);
			var c = client.AddComponent<Client>();
			c.server = this;
			c.frame = frame;
			clients.Add(c);
		}
		
		for(int i = 0; i <= nClients; i++)
		{
			CreatePlayerForClient(nClients, i, i == nClients ? PlayerEntity.InputMode.Automated : PlayerEntity.InputMode.Set); // create player for new client
			if (i != nClients) CreatePlayerForClient(i, nClients, PlayerEntity.InputMode.Set); // Create player remote
		}
		var camera = Instantiate(playerCamera);
		SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
		camera.targetDisplay = nClients;
		camera.gameObject.layer = layers[nClients];
		camera.cullingMask = 1 << layers[nClients];
		nClients++;
	}

	private void CreatePlayerForClient(int clientId, int playerId, PlayerEntity.InputMode inputMode = PlayerEntity.InputMode.Automated)
	{
		var player = Instantiate(playerPrefab);
		player.name = $"Player ({playerId})";
		var scene = scenes[clientId];
		SceneManager.MoveGameObjectToScene(player, scene);
		player.layer = layers[clientId];
		var ent = player.GetComponent<PlayerEntity>();
		ent.id = playerId;
		ent.inputMode = inputMode;
		ent.replayOnReset = clientId == playerId && clientId != 0;
		ent.textColor = new[] { Color.white, Color.red, Color.green, Color.blue, Color.cyan }[playerId];
		if (clientId == playerId) ent.isController = true;
		
	}
	
}
