using System.Collections;
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
		var list = new List<PlayerFrame>();
		foreach (var ent in gameObject.scene.GetRootGameObjects())
		{
			var comp = ent.GetComponent<PlayerEntity>();
			if (comp)
			{
				comp.SimulateOwner();
				if (comp.isController) comp.SimulateController();
				gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
				list.Add(new PlayerFrame
				{
					input = comp.GetFrameInput(),
					state = comp.GetFrameState()
				});
			}
		}
		foreach(var scene in scenes)
		{
			if (!scene.IsValid()) continue;
			foreach (var entFrame in list)
			{
				scene.GetRootGameObjects().First(g => g.GetComponent<Client>()).GetComponent<Client>().ServerUpdate(list);
			}
		}
		
	}

	public void ClientInputUpdate(PlayerInput input)
	{
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
			client.AddComponent<Client>().server = this;
		}
		
		for(int i = 0; i <= nClients; i++)
		{
			CreatePlayerForClient(nClients, i); // create player for new client
			if (i != nClients) CreatePlayerForClient(i, nClients); // Create player remote
		}
		var camera = Instantiate(playerCamera);
		SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
		camera.targetDisplay = nClients;
		camera.gameObject.layer = layers[nClients];
		camera.cullingMask = 1 << layers[nClients];
		nClients++;
	}

	private void CreatePlayerForClient(int clientId, int playerId)
	{
		var player = Instantiate(playerPrefab);
		var scene = scenes[clientId];
		SceneManager.MoveGameObjectToScene(player, scene);
		player.layer = layers[clientId];
		var ent = player.GetComponent<PlayerEntity>();
		ent.id = playerId;
		if (clientId == playerId) ent.isController = true;
		
	}
	
}
