using System;
using System.Collections;
using System.Collections.Generic;
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
		private void Start()
		{
			playerScript = GetComponent<PlayerScript>();
			playerSync = GetComponent<PlayerSync>();

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
		}

		#region Teleport

		[Command]
		public void CmdTeleportToCore()
		{
			playerSync.SetPosition(aiCore.WorldPosServer());
		}

		#endregion
	}
}