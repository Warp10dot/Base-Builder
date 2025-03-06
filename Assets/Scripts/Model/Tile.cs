using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

//We moved this out of the tile class so that we can just type TileType.Floor, etc, instead of Tile.TileType.Floor.
public enum TileType { Empty, Floor_Rods };

public enum ENTERABILITY { Yes, Never, Soon};

public class Tile : IXmlSerializable {



    TileType type = TileType.Empty;

    //Action - A variable which stores a method to be called somewhere else. This one will hold on tile type changed.
    Action<Tile> cbTileChanged;

    public TileType Type
    {
        get
        {
            return type;
        }
        set
        {
            if (type != value)
            {
                type = value;
                //Call the callback and let things know we've changed. 
                if (cbTileChanged != null)
                {
                    cbTileChanged(this);
                }
            }
        }
    }

    Inventory inventory;

    public Room room;

    public Furniture furniture
    {
        get; protected set;
    }

    public Job pendingFurnitureJob;

    public World world { get; protected set; }
    int x;
    int y;

    public int X
    {
        get
        {
            return x;
        }
    }

    public int Y
    {
        get
        {
            return y;
        }
    }

    const float baseTileMovementCost = 1; // FIXME this is just hardcoded for now. Basiclaly a reminder of something we might want to do later.

    public float movementCost
    {
        get
        {
            if(type == TileType.Empty)
            {
                return 0; // 0 is unwalkable
            }

            if(furniture == null)
            {
                return baseTileMovementCost;
            }

            return baseTileMovementCost * furniture.movementCost;
        }
    }

    

    public Tile(World world, int x, int y) {
        this.world = world;
        this.x = x;
        this.y = y;
    }

    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
        //This method is used to pass down a function
        //Notes on action - It's secretly an array, you can have multiple functions in it which are called at the same time. 
    }

    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if(objInstance == null)
        {
            //We are uninstalling whatever was here before
            //Note that we are using the tile object's furniture variable, this is because we're removing the wall from the tile
            furniture = null;
            return true;
        }

        if(furniture != null)
        {
            Debug.LogError("Trying to assign a furniture to a tile that already has one");
            return false;
        }

        //At this point, everything's fine so we just set the tile's object to the instance of the object we gave this metod.
        furniture = objInstance;
        return true;
    }


    //Tells us if two tiles are adjacent. 
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 || //Check hori/vert adjacency. So long as there is a difference of one here, it means it is directly above or next to us
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1) ) //Check horizontal adjacency.
            ; //Horizontal check searches for a difference of two, BUT! It can't do it directly, since other tiles have a difference of two
              //to solve this, we do the X and Y checks separate, if they both have a difference of one, it means it is in a diagonal square.
    }

    public Tile[] GetNeighbours(bool diagOkay = false)
    {
        Tile[] ns;

        if(diagOkay == false)
        {
            ns = new Tile[4]; //Tile Order: N, E, S, W.
        }
        else
        {
            ns = new Tile[8]; //Tile Order: N, E, S, W, NE, SE, SW, NW.
        }

        Tile n;

        n = world.GetTileAt(X, Y + 1);
        ns[0] = n; //Setting the north tile to position 0 in the array. If there is no tile it will return null which is okay.
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;
        n = world.GetTileAt(X, Y - 1);
        ns[2] = n;
        n = world.GetTileAt(X - 1, Y);
        ns[3] = n;

        if(diagOkay == true)
        {
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;
        }

        return ns;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }

    public ENTERABILITY IsEnterable()
    {
        //This returns true if you can enter this tile right this moment.
        if(movementCost == 0)
        {
            return ENTERABILITY.Never;
        }

        // Check our furniture to check if it has a special block on enterable.
        if(furniture != null && furniture.IsEnterable != null)
        {
            return furniture.IsEnterable(furniture);
        }


        return ENTERABILITY.Yes;
    }

    public Tile North()
    {
        return world.GetTileAt(x, y + 1);
    }

    public Tile South()
    {
        return world.GetTileAt(x, y - 1);
    }

    public Tile East()
    {
        return world.GetTileAt(x+1, y);
    }

    public Tile West()
    {
        return world.GetTileAt(x-1, y + 1);
    }

}
