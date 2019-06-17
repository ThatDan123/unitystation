
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Version of Interactable which supports HandApply and extends NetworkBehavior rather than MonoBehavior
/// </summary>
public abstract class NBHandApplyInteractable
	: NetworkBehaviour, IInteractable<HandApply>, IInteractionProcessor<HandApply>
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<HandApply> coordinator;

	protected void Start()
	{
		EnsureCoordinatorInit();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinator == null)
		{
			coordinator = new InteractionCoordinator<HandApply>(this,  WillInteract, ServerPerformInteraction);
		}
	}


	/// <summary>
	/// Decides if interaction logic should proceed. On client side, the interaction
	/// request will only be sent to the server if this returns true. On server side,
	/// the interaction will only be performed if this returns true.
	///
	/// Each interaction has a default implementation of this which should apply for most cases.
	/// By overriding this and adding more specific logic, you can reduce the amount of messages
	/// sent by the client to the server, decreasing overall network load.
	/// </summary>
	/// <param name="interaction">interaction to validate</param>
	/// <param name="side">which side of the network this is being invoked on</param>
	/// <returns>True/False based on whether the interaction logic should proceed as described above.</returns>
	protected virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(HandApply interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(HandApply interaction) { }

	public bool Interact(HandApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinator, ClientPredictInteraction);
	}

	public bool ServerProcessInteraction(HandApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinator);
	}
}
