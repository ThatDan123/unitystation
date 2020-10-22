using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using AI;

public class AiMouseInputController : MouseInputController
{
	private AiPlayer aiPlayer;

	public override void Start()
	{
		aiPlayer = GetComponent<AiPlayer>();
		base.Start();
	}

	public override void CheckMouseInput()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//don't do any game world interactions if we are over the UI
			return;
		}

		if (UIManager.IsMouseInteractionDisabled)
		{
			//still allow tooltips
			CheckHover();
			return;
		}

		if (CommonInput.GetMouseButtonDown(0))
		{

			if (KeyboardInputManager.IsControlPressed() && KeyboardInputManager.IsShiftPressed())
			{
				CheckForInteractions(AiActivate.ClickTypes.CtrlShiftClick);
				return;
			}

			//check ctrl+click for dragging
			if (KeyboardInputManager.IsControlPressed())
			{
				CheckForInteractions(AiActivate.ClickTypes.CtrlClick);
				return;
			}

			if (KeyboardInputManager.IsShiftPressed())
			{
				//like above, send shift-click request, then do nothing else.
				//Inspect();
				CheckForInteractions(AiActivate.ClickTypes.ShiftClick);
				return;
			}

			if (KeyboardInputManager.IsAltPressed())
			{
				CheckForInteractions(AiActivate.ClickTypes.AltClick);
				return;
			}

			CheckForInteractions(AiActivate.ClickTypes.NormalClick);
		}
		else
		{
			CheckHover();
		}
	}

	private void CheckForInteractions(AiActivate.ClickTypes clickType)
	{
		//TODO check FOV validation

		var handApplyTargets = MouseUtils.GetOrderedObjectsUnderMouse();

		//go through the stack of objects and call any interaction components we find
		foreach (GameObject applyTarget in handApplyTargets)
		{
			var behaviours = applyTarget.GetComponents<MonoBehaviour>()
				.Where(m => m != null && m.enabled && m is IAiInteractable<AiActivate>);
			foreach (var aiInteractable in behaviours)
			{
				var aiActivate = new AiActivate(gameObject, null, applyTarget, Intent.Help, clickType);
				var activate = aiInteractable as IAiInteractable<AiActivate>;
				InteractionUtils.RequestInteract(aiActivate, activate);
			}
		}
	}
}
