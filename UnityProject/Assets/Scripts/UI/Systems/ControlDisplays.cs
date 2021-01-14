using UnityEngine;
using System.Collections;
using Audio.Managers;
using Audio.Containers;
using Blob;
using DatabaseAPI;
using JetBrains.Annotations;
using ServerInfo;

public class ControlDisplays : MonoBehaviour
{
	/// <summary>
	/// Represents which screen to open with generic function
	/// </summary>
	public enum Screens
	{
		SlotReset,
		Lobby,
		Game,
		PreRound,
		Joining,
		TeamSelect,
		JobSelect
	}
	public GameObject hudBottomHuman;
	public GameObject hudBottomGhost;
	public GameObject hudBottomBlob;
	public GameObject hudBottomAi;
	public GameObject currentHud;
	public GameObject jobSelectWindow;
	public GameObject teamSelectionWindow;
	[CanBeNull] public GameObject disclaimer;
	public RectTransform panelRight;
	public GUI_PreRoundWindow preRoundWindow;

	[SerializeField]
	private GameObject rightClickManager = null;

	[SerializeField] private Animator uiAnimator = null;
	[SerializeField] private VideoPlayerController videoController = null;
	public VideoPlayerController VideoPlayer => videoController;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerSpawned, DeterminUI);
		EventManager.AddHandler(EVENT.GhostSpawned, DeterminUI);
		EventManager.AddHandler(EVENT.BlobSpawned, DeterminUI);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.PlayerSpawned, DeterminUI);
		EventManager.RemoveHandler(EVENT.GhostSpawned, DeterminUI);
		EventManager.RemoveHandler(EVENT.BlobSpawned, DeterminUI);
	}

	public void RejoinedEvent()
	{
		//for some reason this is getting called when ControlDisplays is already destroyed when client rejoins while
		//a ghost, this check prevents a MRE
		if (!this) return;
		StartCoroutine(DetermineRejoinUI());
	}

	IEnumerator DetermineRejoinUI()
	{
		//Wait for the assigning
		while (PlayerManager.LocalPlayerScript == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		DeterminUI();
	}

	private void DeterminUI()
	{
		//TODO: make better system for handling lots of different UIs
		if (PlayerManager.LocalPlayerScript.IsBlob)
		{
			SetUi(hudBottomBlob);
			PlayerManager.LocalPlayerScript.GetComponent<BlobPlayer>()?.TurnOnClientLight();
		}
		if (PlayerManager.LocalPlayerScript.IsAI)
		{
			SetUi(hudBottomAi);
		}
		else if (PlayerManager.LocalPlayerScript.playerHealth == null)
		{
			SetUi(hudBottomGhost);
		}
		else
		{
			SetUi(hudBottomHuman);
		}
	}

	public void SetUi(GameObject newUi)
	{
		if (currentHud == null)
		{
			PlayerManager.LocalPlayerScript.Ui = hudBottomHuman;
			currentHud = hudBottomHuman;
		}

		//Turn off old UI
		currentHud.SetActive(false);

		PlayerManager.LocalPlayerScript.Ui = newUi;
		currentHud = newUi;

		//Turn on new UI
		currentHud.SetActive(true);

		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		panelRight.gameObject.SetActive(true);
		rightClickManager.SetActive(true);
		preRoundWindow.gameObject.SetActive(false);
		MusicManager.SongTracker.Stop();
	}

	void Update()
	{
		TempFixMissingRightHud();
	}

	//Temp fix for strange bug where right hud is missing when joining headless server
	void TempFixMissingRightHud()
	{
		if (CustomNetworkManager.Instance == null) return;
		if (CustomNetworkManager.Instance._isServer) return;
		if (PlayerManager.LocalPlayerScript == null) return;
		if (PlayerManager.LocalPlayerScript.playerHealth == null) return;
		if (!PlayerManager.LocalPlayerScript.playerHealth.IsDead &&
		    !UIManager.PlayerHealthUI.gameObject.activeInHierarchy)
		{
			UIManager.PlayerHealthUI.gameObject.SetActive(true);
		}
		if (!PlayerManager.LocalPlayerScript.playerHealth.IsDead &&
		    !UIManager.PlayerHealthUI.humanUI)
		{
			UIManager.PlayerHealthUI.humanUI = true;
		}

	}

	/// <summary>
	/// Generic UI changing function for net messages
	/// </summary>
	/// <param name="screen">The UI action to perform</param>
	public void SetScreenFor(Screens screen)
	{
		Logger.Log($"Setting screen for {screen}", Category.UI);
		switch (screen)
		{
			case Screens.SlotReset:
				ResetUI();
				break;
			case Screens.Lobby:
				SetScreenForLobby();
				break;
			case Screens.Game:
				SetScreenForGame();
				break;
			case Screens.PreRound:
				SetScreenForPreRound();
				break;
			case Screens.Joining:
				SetScreenForJoining();
				break;
			case Screens.TeamSelect:
				SetScreenForTeamSelect();
				break;
			case Screens.JobSelect:
				SetScreenForJobSelect();
				break;
		}
	}

	/// <summary>
	///     Clears all of the UI slot items
	/// </summary>
	public void ResetUI()
	{
		foreach (UI_ItemSlot itemSlot in GetComponentsInChildren<UI_ItemSlot>())
		{
			itemSlot.Reset();
		}
	}

	public void SetScreenForLobby()
	{
		SoundAmbientManager.StopAllAudio();
		MusicManager.SongTracker.StartPlayingRandomPlaylist();
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIActionManager.Instance.OnRoundEnd();
		currentHud.SetActive(false);
		panelRight.gameObject.SetActive(false);
		rightClickManager.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.gameObject.SetActive(false);
		if (disclaimer != null) disclaimer.SetActive(true);
		UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(false);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(false);
	}

	public void SetScreenForGame()
	{
		currentHud.SetActive(false);
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		panelRight.gameObject.SetActive(true);
		rightClickManager.SetActive(false);
		uiAnimator.Play("idle");
		if (disclaimer != null) disclaimer.SetActive(false);
		preRoundWindow.gameObject.SetActive(true);
		preRoundWindow.SetUIForMapLoading();
	}

	public void SetScreenForPreRound()
	{
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		currentHud.SetActive(false);
		panelRight.gameObject.SetActive(false);
		rightClickManager.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.gameObject.SetActive(true);
		preRoundWindow.SetUIForCountdown();

		ServerInfoMessageClient.Send(ServerData.UserID);
	}

	public void SetScreenForJoining()
	{
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		currentHud.SetActive(false);
		panelRight.gameObject.SetActive(false);
		rightClickManager.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.gameObject.SetActive(true);
		preRoundWindow.SetUIForJoining();
	}

	public void SetScreenForTeamSelect()
	{
		preRoundWindow.gameObject.SetActive(false);
		teamSelectionWindow.SetActive(true);
	}

	public void SetScreenForJobSelect()
	{
		preRoundWindow.gameObject.SetActive(false);
		jobSelectWindow.SetActive(true);
	}

	public void PlayStrandedVideo()
	{
		uiAnimator.Play("StrandedVideo");
	}
}