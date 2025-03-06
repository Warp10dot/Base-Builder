using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_TileGraph
{

    //This class constructs a simple pathfinding compatible graph of our world.
    //Each tile is a node, and each walkable neighbor from a tile is linked via an edge connection

    public Dictionary<Tile, Path_Node<Tile>> nodes;

    public Path_TileGraph(World world)
    {

        Debug.Log("Path_TileGraph");

        // Loops through all tiles of the world. For each tile create a node.
        // Do we create nodes for empty tiles? NO!
        // Do we create nodes for tiles which are unwalkable, I.E, walls? NO!

        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile t = world.GetTileAt(x, y);
                //if(t.movementCost > 0) // Tiles with a movecost of 0 are unwalkable.
               //{
                    Path_Node<Tile> n = new Path_Node<Tile>();
                    n.data = t;
                    nodes.Add(t, n);
                //}
            }
        }

        Debug.Log("Path_TileGraph created: "+ nodes.Count+" nodes");

        int edgeCount = 0; //Debug

        // Now loopthrough all nodes 
        // Create edges for neighbors. 

        foreach (Tile t in nodes.Keys)
        {
            Path_Node<Tile> n = nodes[t];

            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

            //Get a list of neighbors for the tile
            Tile[] neighbours = t.GetNeighbours(true); // NOTE: Some of the array spots could be null.

            //then if neighbor is walkable, create an edge to the relevant node.
            for (int i = 0; i < neighbours.Length; i++)
            {
                if(neighbours[i] != null && neighbours[i].movementCost > 0)
                {
                    //The neighbour exists and is walkable. So create an edge.

                    // But first make sure we aren't clipping a diagonal or trying to squeeze inappropriately.
                    if(IsClippingCorner(t, neighbours[i]))
                    {
                        continue; // Skip to the next neighbor without building an edge.
                    }

                    Path_Edge<Tile> e = new Path_Edge<Tile>();
                    e.cost = neighbours[i].movementCost;
                    e.node = nodes[ neighbours[i] ];

                    //Add the edge to our temporary (and growable!) list.
                    edges.Add(e);

                    edgeCount++; //Debug
                }
            }

            n.edges = edges.ToArray();

        }
        Debug.Log("Path_TileGraph created: " + edgeCount + " edges");
    }

    bool IsClippingCorner(Tile curr, Tile neigh)
    {
        // If the movement from curr to neigh is diagonal (Example: NE.)
        // Then check to make sure we aren't clipping. (E.G. N and E are walkable.

        if(Mathf.Abs(curr.X - neigh.X) + Mathf.Abs(curr.Y - neigh.Y) == 2) // Abs makes negative numbers positive. We are checking that we differ by exactly one on each direction, i.e, diagonal.
        {
            // We are diagonal

            int dX = curr.X - neigh.X;
            int dY = curr.Y - neigh.Y;

            if( curr.world.GetTileAt(curr.X - dX, curr.Y).movementCost == 0)
            {
                // East or west is unwalkable thereforere this would be a clipped movement
                return true;
            }

            if (curr.world.GetTileAt(curr.X, curr.Y - dY).movementCost == 0)
            {
                // North or south is unwalkable thereforere this would be a clipped movement
                return true;
            }

            
        }
        return false;
    }


}
