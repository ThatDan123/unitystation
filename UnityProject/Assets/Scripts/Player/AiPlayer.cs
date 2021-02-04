using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Mirror;
using UnityEngine;

namespace AI
{
	public class AiPlayer : NetworkBehaviour
	{
		private PlayerScript playerScript;
		private PlayerSync playerSync;

		[SerializeField]
		private GameObject aiCorePrefab = null;

		[HideInInspector]
		public GameObject aiCore;

		private Camera mainCamera;
		private LightingSystem lightingSystem;
		private ConeOfSight coneOfSight;
		private LayerMask hitMask;

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			playerSync = GetComponent<PlayerSync>();
			coneOfSight = GetComponent<ConeOfSight>();
			hitMask = LayerMask.GetMask( "Players");
		}

		private void Start()
		{
			if (!CustomNetworkManager.IsServer) return;

			var result = Spawn.ServerPrefab(aiCorePrefab, playerScript.WorldPos, gameObject.transform);

			if (!result.Successful)
			{
				Debug.LogError("Failed to spawn AiCore");
				return;
			}

			aiCore = result.GameObject;

			//TODO RPC this
			playerScript.IsPlayerSemiGhost = true;
			playerScript.IsAI = true;
		}

		private void OnEnable()
		{
			var aiHud = UIManager.Display.hudBottomAi.GetComponent<UI_Ai>();
			aiHud.aiPlayer = this;
			aiHud.controller = GetComponent<AiMouseInputController>();

			mainCamera = Camera.main;
			lightingSystem = mainCamera.GetComponent<LightingSystem>();

			UpdateManager.Add(TrySwitchCamera, 0.1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TrySwitchCamera);
		}

		#region SecCameraSwitching

		[Client]
		private void TrySwitchCamera()
		{
			var cameras = new List<GameObject>();

			foreach (var secCamera in SecurityCameraManager.Instance.SecurityCameras)
			{
				if(secCamera.Key == null) continue;

				if(Vector3.Distance(secCamera.Key.WorldPosClient(), gameObject.WorldPosClient()) > 30) continue;

				if(secCamera.Value != SecurityCameraChannels.Station) continue;

				cameras.Add(secCamera.Key);
			}

			cameras.OrderBy(n => Vector3.Distance(gameObject.WorldPosClient(), n.WorldPosClient()));

			//Debug.LogError($"amount of cameras in range: {cameras.Count}");

			foreach (var camera in cameras)
			{
				var linecast = MatrixManager.Linecast(
					gameObject.WorldPosClient(),
					LayerTypeSelection.Walls | LayerTypeSelection.Windows, null,
					camera.WorldPosClient());

				if (!linecast.ItHit)
				{
					//Debug.LogError($"{camera.name} was hit by linecast");
					Debug.DrawLine(gameObject.WorldPosClient(), camera.WorldPosClient(), Color.blue, 1f);

					var cameraPos = camera.WorldPosClient();
					var mainCameraPos = mainCamera.transform.position;

					var newOffset = new Vector3(0, 0, 0);

					if (cameraPos.x < mainCameraPos.x)
					{
						newOffset.x = cameraPos.x - mainCameraPos.x;
					}
					else
					{
						newOffset.x = mainCameraPos.x - cameraPos.x;
					}

					if (cameraPos.y < mainCameraPos.y)
					{
						newOffset.y = cameraPos.y - mainCameraPos.y;
					}
					else
					{
						newOffset.y = mainCameraPos.y - cameraPos.y;
					}

					lightingSystem.otherFOV = newOffset;

					break;
				}
			}
		}

		#endregion

		#region Teleport

		[Command]
		public void CmdTeleportToCore()
		{
			playerSync.SetPosition(aiCore.WorldPosServer());
		}

		#endregion
	}
}