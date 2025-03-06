using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public float atmos02  = 0;
    public float atmosN   = 0;
    public float atmosCO2 = 0;

    List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
        {
            // This tile is already in this room
            return;
        }

        if(t.room != null)
        {
            // Belongs to another room.
            t.room.tiles.Remove(t);
        }

        t.room = this;
        tiles.Add(t);
    }

    public void UnassignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom(); 
        }
        tiles = new List<Tile>();
    }

    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        // Sourcefurniture is the piece of furniture that may be splitting two existing rooms.
        // Or may be the final enclosing piece to form a new room.
        // Check the N E S W neighbors of the furniture's tile.
        // And do flood fill from them.

        World world = sourceFurniture.tile.world;

        Room oldRoom = sourceFurniture.tile.room;

        // Try building a new rooms for each of our NESW directions
        foreach  (Tile t in sourceFurniture.tile.GetNeighbours())
        {
            ActualFloodFill(t, oldRoom);
        }

        // Removing the source Furniture's tile to prevent errors 
        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        // If this piece of furniture was added to an existing room, which should always be true assuming we consider outside to be a big room.
        // Delete that room and assign all tiles within it to be outside for now

        if (oldRoom != world.GetOutsideRoom())
        {
            // At this point, oldRoom shouldn't have any more tiles left in it
            // So in practice this delete room should mostly only need to remove the room from the world's list.

            if(oldRoom.tiles.Count > 0)
            {
                Debug.LogError("oldRoom still has tiles assigned to it. This is clearly wrong.");
            }

            world.DeleteRoom(oldRoom);
        }
    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        if(tile == null)
        {
            // We are trying to flood fill off the map, so just return without doing anything.
            return;
        }

        if(tile.room != oldRoom)
        {
            // This tile was already assigned to another new room which means that the direction picked isn't isolated so we can just return without creating a new room.
            return;
        }

        if(tile.furniture != null && tile.furniture.roomEnclosure)
        {
            // This tile has a wall/door/etc in it, so clearly we can't do a room in it
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            // This tile is empty space and must remain part of the outside
            return;
        }

        // If we get to this point then we know that we need to create a new room.

        Room newRoom = new Room();

        // The current tile clearly belongs to this new room
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0) 
        {
            Tile t = tilesToCheck.Dequeue();


            if(t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns)
                {
                    if(t2 == null || t2.Type == TileType.Empty)
                    {
                        // We have hit open space, either by being the edge of the map or being an empty tile
                        // so this room we're building is actually part of the outside, therefore we can immediately
                        // end the flood fill (Which otherwise would take ages).
                        // And more importantly we need to delete this newRoom and reassign all the tiles to outside.
                        newRoom.UnassignAllTiles();
                        return;
                    }

                    // We know t2 is not null nor is it an empty tile so just make sure it hasn't already been processed and isn't a wall type tile
                    if (t2.room == oldRoom && (t2.furniture == null || t2.furniture.roomEnclosure == false))
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }

        // Copy data from the old room into the new room.
        newRoom.atmos02 = oldRoom.atmos02;
        newRoom.atmosCO2 = oldRoom.atmosCO2;
        newRoom.atmosN = oldRoom.atmosN;

        // Tell the world that a new room has been formed.
        tile.world.AddRoom(newRoom);

    }
}
