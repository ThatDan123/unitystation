using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for AI interactions
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAiInteractable<T> : ICheckedInteractable<AiActivate>
	where T : Interaction
{

}
