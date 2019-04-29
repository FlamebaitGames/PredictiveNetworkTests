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
	private int frame = 0;

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
		var list = new List<PlayerEntity>();
		foreach (var ent in gameObject.scene.GetRootGameObjects())
		{
			var comp = ent.GetComponent<PlayerEntity>();
			if (comp)
			{
				if (inputBacklog.Any(p => p.entityId == comp.id))
				{
					var inp = inputBacklog.First(p => p.entityId == comp.id);
					comp.SetInput(inp);
				}
				comp.SimulateOwner();
				if (comp.isController) comp.SimulateController();
				
				list.Add(comp);
			}
		}

		foreach(var ent in list)
		{
			ent.ExecuteCommand();
		}
		gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		inputBacklog.Clear();
		frame++;
		foreach(var client in clients)
		{
			client.ServerUpdate(list.Select(ent => new PlayerFrame
			{
				frame = frame,
				input = ent.GetFrameInput(),
				state = ent.GetFrameState()
			}).ToArray());
		}
		
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
		ent.textColor = new[] { Color.white, Color.red, Color.green, Color.blue, Color.cyan }[playerId];
		if (clientId == playerId) ent.isController = true;
		
	}
	
}
