using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace Core.Networking
{
	public class CustomInterestManager : InterestManagement
	{
		[Tooltip("The maximum range that objects will be visible at.")]
        public int visRange = 30;

        // if we see 8 neighbors then 1 entry is visRange/3
        public int resolution => visRange / 3;

        [Tooltip("How many rebuilds per frame")]
        public float identitiesInFrame = 300;

        private bool rebuilding;
        private int count;

        // the grid
        Grid2D<NetworkConnection> grid = new Grid2D<NetworkConnection>();

        // project 3d world position to grid position
        Vector2Int ProjectToGrid(Vector3 position) => Vector2Int.RoundToInt(new Vector2(position.x, position.y) / resolution);

        public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnection newObserver)
        {
	        //If this is player then always add ghost and admin observers
	        if (identity.connectionToClient != null)
	        {
		        var player = PlayerList.Instance.GetByConnection(newObserver);

		        if (player != null && player.ViewerScript != null)
		        {
			        return true;
		        }

		        if (player != null && ((player.Script != null && player.Script.IsGhost) || PlayerList.Instance.IsAdmin(player)))
		        {
			        return true;
		        }
	        }

	        // calculate projected positions
            Vector2Int projected = ProjectToGrid(identity.transform.position);
            Vector2Int observerProjected = ProjectToGrid(newObserver.identity.transform.position);

            // distance needs to be at max one of the 8 neighbors, which is
            //   1 for the direct neighbors
            //   1.41 for the diagonal neighbors (= sqrt(2))
            // => use sqrMagnitude and '2' to avoid computations. same result.
            return (projected - observerProjected).sqrMagnitude <= 2;
        }

        public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnection> newObservers, bool initialize)
        {
	        //If this is player then always add ghost and admin observers
	        foreach (var player in PlayerList.Instance.loggedIn)
	        {
		        if(player.Connection == identity.connectionToClient) continue;

		        if (player.ViewerScript != null)
		        {
			        newObservers.Add(player.Connection);
		        }

		        if (identity.connectionToClient != null && ((player.Script != null && player.Script.IsGhost) || PlayerList.Instance.IsAdmin(player)))
		        {
			        newObservers.Add(player.Connection);
		        }
	        }

            // add everyone in 9 neighbour grid
            // -> pass observers to GetWithNeighbours directly to avoid allocations
            //    and expensive .UnionWith computations.
            Vector2Int current = ProjectToGrid(identity.transform.position);
            grid.GetWithNeighbours(current, newObservers);
        }

        // update everyone's position in the grid
        // (internal so we can update from tests)
        internal void Update()
        {
            // only on server
            if (!NetworkServer.active) return;

            // IMPORTANT: refresh grid every update!
            // => newly spawned entities get observers assigned via
            //    OnCheckObservers. this can happen any time and we don't want
            //    them broadcast to old (moved or destroyed) connections.
            // => players do move all the time. we want them to always be in the
            //    correct grid position.
            // => note that the actual 'rebuildall' doesn't need to happen all
            //    the time.
            // NOTE: consider refreshing grid only every 'interval' too. but not
            //       for now. stability & correctness matter.

            // clear old grid results before we update everyone's position.
            // (this way we get rid of destroyed connections automatically)
            //
            // NOTE: keeps allocated HashSets internally.
            //       clearing & populating every frame works without allocations
            grid.ClearNonAlloc();

            // put every connection into the grid at it's main player's position
            // NOTE: player sees in a radius around him. NOT around his pet too.
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                // authenticated and joined world with a player?
                if (connection.isAuthenticated && connection.identity != null)
                {
                    // calculate current grid position
                    Vector2Int position = ProjectToGrid(connection.identity.transform.position);

                    // put into grid
                    grid.Add(position, connection);
                }
            }

            // rebuild all spawned entities' observers every 'interval'
            // this will call OnRebuildObservers which then returns the
            // observers at grid[position] for each entity.
            if (rebuilding == false)
            {
	            rebuilding = true;
	            Rebuild();
            }
        }

        private async void Rebuild()
        {
	        foreach (var identity in NetworkIdentity.spawned.Values.ToList())
	        {
		        NetworkServer.RebuildObservers(identity, false);

		        count++;

		        if (count % identitiesInFrame == 0)
		        {
			        await Task.Delay(1);
		        }
	        }

	        count = 0;
	        rebuilding = false;
        }
	}
}
