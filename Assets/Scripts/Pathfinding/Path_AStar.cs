using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

public class Path_AStar
{
    Queue<Tile> path;

    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {
        // Check that we have a valid tileGraph
        if(world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world); // Generate a new graph if the old one was made null.
            // Tile graph is made null every time a wall or tile changes, and is only regenrated here when we do pathfinding.
        }

        // A Dictionary of all the valid walkable nodes.
        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        // Make sure our start/end tiles are in the list of nodes.
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Path_AStar: The starting tile isn't in the list of nodes.");
            return;
        }
        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Path_AStar: The ending tile isn't in the list of nodes.");
            return;
        }

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];

        // Mostly following the A* Pseudocode.

        List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();

        /*        List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();
                OpenSet.Add(start);*/

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);


        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            g_score[n] = Mathf.Infinity; // Setting the nodes to infinity (The highest possible float) this is to stop the system from using these nodes,
                                         // assuming them to be ridiculously expensive to traverse until we know better.
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            f_score[n] = Mathf.Infinity; // Setting the nodes to infinity (The highest possible float) this is to stop the system from using these nodes,
                                         // assuming them to be ridiculously expensive to traverse until we know better.
        }
        f_score[start] = heuristic_cost_estimate(start, goal);

        while (OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();

            if(current == goal)
            {
                // TODO: Return reconstruct path.
                // We have reached our goal. Let's convert this into an actual sequence of tiles to walk on then end this constructor function.
                reconstruct_path(Came_From, current);
                return;
            }

            ClosedSet.Add(current);

            foreach(Path_Edge<Tile> edge_neighbor in current.edges) // For each neighbour connected to our current node
            {
                Path_Node<Tile> neighbor = edge_neighbor.node;
                if(ClosedSet.Contains(neighbor) == true)
                {
                    continue; // Ignore this already completed neighbor. Contineu goes to the next step in the loop.
                }

                float movement_cost_to_neighbor = neighbor.data.movementCost * dist_between(current, neighbor);

                float tentative_g_score = g_score[current] + movement_cost_to_neighbor;

                if (OpenSet.Contains(neighbor) && tentative_g_score >= g_score[neighbor])
                {
                    continue;
                }

                Came_From[neighbor] = current;
                g_score[neighbor] = tentative_g_score;
                f_score[neighbor] = g_score[neighbor] + heuristic_cost_estimate(neighbor, goal);

                if(OpenSet.Contains(neighbor) == false)
                {
                    OpenSet.Enqueue(neighbor, f_score[neighbor]);
                }
                else
                {
                    OpenSet.UpdatePriority(neighbor, f_score[neighbor]);
                }
            } // Foreach neighbor
        } // While

        // If we reached here, it means that we burned through the entire open set
        // without ever reaching a point where current == goal. 
        // This happens when there is no path from start to goal. 
        // (So there's a wall or missing floor or something)

        // We don't have a failure state, maybe? It's just that the path list will be null.

    }
    
    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );
    }

    float dist_between(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // We can make assumptions because we know we're working on a grid at this point.

        // Hori/Vert neighbors have a distance of 1.
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1f;
        }



        // Diagonal neighbors have a distance of 1.41421356237
        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1.41421356237f;
        }

        // Otherwise, do the actual math to figure it out.
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );

    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current)
    {
        // At this point, our current tile IS the goal 
        // so what we want to do is walk backwards through our came_from map
        // until we reach the "end" of that map
        // which will be our starting node!

        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current.data); // The final step in the path is the goal

        while (Came_From.ContainsKey(current))
        {
            // Came_From is a map where the
            // key => value relation is really saying
            // some node => we_got_there_from_this_node.
            current = Came_From[current];
            total_path.Enqueue(current.data);
        }

        // At this point total_path is a queue that is running backwards from the end tile to the start tile.

        path = new Queue<Tile>( total_path.Reverse() ); //Have to create a new queue here because .Reverse() returns a generic.
    }


    public Tile Dequeue()
    {
        return path.Dequeue();
    }

    public int Length()
    {
        if(path == null)
        {
            return 0;
        }

        return path.Count;
    }

}
