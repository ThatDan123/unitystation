using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class UI_Ai : MonoBehaviour
{
	[HideInInspector]
	public AiPlayer aiPlayer = null;

	[HideInInspector]
	public AiMouseInputController controller = null;

	public void JumpToCore()
	{
		if (aiPlayer == null) return;

		aiPlayer.CmdTeleportToCore();
	}
}
