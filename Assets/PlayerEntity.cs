using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntity : MonoBehaviour
{
	
	public new Rigidbody rigidbody { get; private set; }
	public Color textColor = Color.white;
	[SerializeField]
	private GameObject ui;
	public enum InputMode
	{
		Standard,
		Alternate,
		Automated,
		Set
	}
	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}
	public void SetInput(PlayerInput input)
	{
		if(input.frame > frameInput.frame) frameInput = input;
	}
	public InputMode inputMode = InputMode.Automated;
	public bool isController;
	public int id;
	public bool replayOnReset = false;
	
	private List<PlayerInput> previousInputs = new List<PlayerInput>();
	private PlayerInput frameInput;
	public PlayerInput GetFrameInput()
	{
		return frameInput;
	}

	private void Start()
	{
		var rend = GetComponent<Renderer>();
		var mat = rend.material = new Material(rend.sharedMaterial);
		mat.color = textColor;
		if (isController)
		{
			var canv = Instantiate(ui);
			canv.layer = gameObject.layer;
			UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canv, gameObject.scene);
			foreach(var o in gameObject.scene.GetRootGameObjects())
			{
				var cam = o.GetComponent<Camera>();
				if (cam)
				{
					var c = canv.GetComponent<Canvas>();
					c.renderMode = RenderMode.ScreenSpaceCamera;
					c.worldCamera = cam;
				}
			}
			var txt = canv.transform.Find("Text");
			txt.gameObject.layer = gameObject.layer;
			var t = txt.GetComponent<Text>();
			t.color = textColor;
			t.text = name;
		}
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

	public void SimulateController(int frame)
	{
		switch (inputMode)
		{
			case InputMode.Standard:
				frameInput = new PlayerInput { entityId = id, input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized };
				break;
			case InputMode.Alternate:
				frameInput = new PlayerInput { entityId = id, input = new Vector3(Input.GetAxis("Horizontal2"), Input.GetAxis("Vertical2")).normalized };
				break;
			case InputMode.Automated:
				frameInput = new PlayerInput { entityId = id, input = new Vector3(Mathf.Sin(Time.time), Mathf.Tan(Time.time)).normalized };
				break;
			case InputMode.Set:
				break;
		}
		frameInput.frame = frame;
		if(replayOnReset) previousInputs.Add(frameInput);
		//Step(frameInput);
	}
	public void ExecuteCommand()
	{
		Step(frameInput);
	}

	public void ResetState(PlayerState state)
	{
		rigidbody.position = state.position;
		rigidbody.rotation = state.rotation;
		rigidbody.velocity = state.velocity;
		rigidbody.angularVelocity = state.angularVelocity;
		previousInputs.RemoveAll(p => p.frame <= state.frame);
		if(isController) Debug.Log($"{state.frame}: Resetting, replaying {previousInputs.Count} frames");
		foreach(var i in previousInputs)
		{
			if (isController) Debug.Log($"Frame {i.frame}");
			frameInput = i;
			ExecuteCommand();
			gameObject.scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
		}
		//if (previousInputs.Count > 10) UnityEditor.EditorApplication.isPaused = true;
		previousInputs.Clear();
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
