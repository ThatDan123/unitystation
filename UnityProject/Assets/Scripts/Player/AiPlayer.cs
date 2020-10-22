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

		[SerializeField]
		private GameObject aiCorePrefab = null;

		[HideInInspector]
		public GameObject aiCore;
		private void Start()
		{
			playerScript = GetComponent<PlayerScript>();

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
	}
}