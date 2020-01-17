﻿using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

/// <summary>
/// Holds and provides functionality for all the MetaDataTiles for a given matrix.
/// </summary>
public class MetaDataLayer : MonoBehaviour
{
	private MetaDataDictionary nodes = new MetaDataDictionary();

	private SubsystemManager subsystemManager;
	private ReactionManager reactionManager;
	private Matrix matrix;
	private MetaTileMap metaTileMap;

	private void Awake()
	{
		subsystemManager = GetComponentInParent<SubsystemManager>();
		reactionManager = GetComponentInParent<ReactionManager>();
		matrix = GetComponent<Matrix>();
		metaTileMap = GetComponent<MetaTileMap>();
	}

	public MetaDataNode Get(Vector3Int position, bool createIfNotExists = true)
	{
		if (!nodes.ContainsKey(position))
		{
			if (createIfNotExists)
			{
				nodes[position] = new MetaDataNode(position, reactionManager);
			}
			else
			{
				return MetaDataNode.None;
			}
		}

		return nodes[position];
	}

	public bool IsSpaceAt(Vector3Int position)
	{
		return Get(position, false).IsSpace;
	}

	public bool IsRoomAt(Vector3Int position)
	{
		return Get(position, false).IsRoom;
	}

	public bool IsEmptyAt(Vector3Int position)
	{
		return !Get(position, false).Exists;
	}

	public bool IsOccupiedAt(Vector3Int position)
	{
		return Get(position, false).IsOccupied;
	}

	public bool ExistsAt(Vector3Int position)
	{
		return Get(position, false).Exists;
	}

	public bool IsSlipperyAt(Vector3Int position)
	{
		return Get(position, false).IsSlippery;
	}

	public void MakeSlipperyAt(Vector3Int position, bool canDryUp=true)
	{
		var tile = Get(position, false);
		if (tile == MetaDataNode.None || tile.IsSpace)
		{
			return;
		}
		tile.IsSlippery = true;
		if ( canDryUp )
		{
			if (tile.CurrentDrying != null)
			{
				StopCoroutine(tile.CurrentDrying);
			}
			tile.CurrentDrying = DryUp(tile);
			StartCoroutine(tile.CurrentDrying);
		}
	}

	/// <summary>
	/// Release reagents at provided coordinates, making them react with world
	/// </summary>
	public void ReagentReact(Dictionary<string, float> reagents, Vector3Int worldPosInt, Vector3Int localPosInt)
	{
		if (MatrixManager.IsTotallyImpassable(worldPosInt, true))
		{
			return;
		}

		bool didSplat = false;

		foreach (KeyValuePair<string, float> reagent in reagents)
		{
			if(reagent.Value < 1)
			{
				continue;
			}
			if (reagent.Key == "water")
			{
				matrix.ReactionManager.ExtinguishHotspot(localPosInt);

				foreach (var livingHealthBehaviour in matrix.Get<LivingHealthBehaviour>(localPosInt, true))
				{
					livingHealthBehaviour.Extinguish();
				}

				Clean(worldPosInt, localPosInt, true);
			}
			else if (reagent.Key == "cleaner")
			{
				Clean(worldPosInt, localPosInt, false);
			}
			else if (reagent.Key == "welding_fuel")
			{
				//temporary: converting spilled fuel to plasma
				Get(localPosInt).GasMix.AddGas(Gas.Plasma, reagent.Value);
			}
			else if (reagent.Key == "lube")
			{ //( ͡° ͜ʖ ͡°)
				if (!Get(localPosInt).IsSlippery)
				{
					EffectsFactory.WaterSplat(worldPosInt);
					MakeSlipperyAt(localPosInt, false);
				}
			}
			else
			{ //for all other things leave a chem splat
				if (!didSplat)
				{
					EffectsFactory.ChemSplat(worldPosInt);
					didSplat = true;
				}
			}
		}
	}

	public void Clean(Vector3Int worldPosInt, Vector3Int localPosInt, bool makeSlippery)
	{
		Get(localPosInt).IsSlippery = false;

		var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

		for (var i = 0; i < floorDecals.Count; i++)
		{
			floorDecals[i].TryClean();
		}

		if (!MatrixManager.IsSpaceAt(worldPosInt, true) && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.WaterSplat(worldPosInt);

			// Sets a tile to slippery
			MakeSlipperyAt(localPosInt);
		}
	}

	private IEnumerator DryUp(MetaDataNode tile)
	{
		yield return WaitFor.Seconds(Random.Range(10,21));
		tile.IsSlippery = false;

		var floorDecals = matrix.Get<FloorDecal>(tile.Position, isServer: true);
		foreach ( var decal in floorDecals )
		{
			if ( decal.CanDryUp )
			{
				Despawn.ServerSingle(decal.gameObject);
			}
		}
	}


	public void UpdateSystemsAt(Vector3Int position)
	{
		subsystemManager.UpdateAt(position);
	}
}