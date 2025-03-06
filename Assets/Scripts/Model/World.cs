using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

public class World : IXmlSerializable {

    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room> rooms;

    //The pathfinding graph used to navigate our world map.
    public Path_TileGraph tileGraph;

    //Dictionary of our prototypes. 
    Dictionary<string, Furniture> furniturePrototypes;

    int width;
    public int Width
    {
        get
        {
            return width;
        }
    }
    
    int height;
    public int Height
    {
        get
        {
            return height;
        }
    }

    Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;
    Action<Character> cbCharacterCreated;

    public JobQueue jobQueue;

    public World(int width, int height)
    {
        // Creates an empty world.
        SetupWorld(width, height);

        // Make a character
        CreateCharacter(GetTileAt(width / 2, height / 2));
    }

    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room r)
    {
        rooms.Add(r);
    }

    public void DeleteRoom(Room r)
    {
        if(r == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete the outside room");
            return;
        }

        // Remove this room from our rooms list
        rooms.Remove(r);
        // All tiles that belonged to this room should be reassigned to the outside
        r.UnassignAllTiles();
    }

    void SetupWorld(int width, int height)
    {
        jobQueue = new JobQueue();

        this.width = width;
        this.height = height;

        tiles = new Tile[width, height];

        rooms = new List<Room>();
        rooms.Add(new Room()); // Create the outside

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = GetOutsideRoom(); // Rooms 0 is always going to be outside and thati s our default group
            }
        }

        Debug.Log("World created with " + (width * height) + " tiles.");

        CreateInstalledObjectPrototypes();

        characters = new List<Character>();
        furnitures = new List<Furniture>();
    }

    public void Update(float deltaTime)
    {
        foreach(Character c in characters)
        {
            c.Update(deltaTime);
        }

        foreach (Furniture f in furnitures)
        {
            f.Update(deltaTime);
        }
    }

    public Character CreateCharacter(Tile t)
    {
        Character c = new Character(t);

        characters.Add(c);

        if (cbCharacterCreated != null)
        {
            cbCharacterCreated(c);
        }

        return c;
    }

    void CreateInstalledObjectPrototypes()
    {
        // This will be replaced by a function that reads all of our furniture data from a text file in the future.

        //Instantiating the dictionary
        furniturePrototypes = new Dictionary<string, Furniture>();

        furniturePrototypes.Add("Wall", 
            new Furniture(
                "Wall", 
                0, //Impassable
                1, //Width
                1, //Height
                true, //Links to neighbours and changes sprite accordingly
                true // Encloses room
                ));

        furniturePrototypes.Add("Door",
            new Furniture(
                "Door",
                1, //Movement cost
                1, //Width
                1, //Height
                false, //Links to neighbours and changes sprite accordingly
                true // Encloses room
                ));

        // What if the object behaviors were scriptable and were therefore part of the object text file we are reading in now.

        furniturePrototypes["Door"].SetParameter("openness", 0);
        furniturePrototypes["Door"].SetParameter("is_opening", 0);
        furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);

        furniturePrototypes["Door"].IsEnterable = FurnitureActions.Door_IsEnterable;
    }

    public void RandomizeTiles()
    {
        Debug.Log("Randomized Tiles.");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(Random.Range(0, 2) == 0)
                {
                    tiles[x, y].Type = TileType.Empty;
                }
                else
                {
                    tiles[x, y].Type = TileType.Floor_Rods;
                }
            }
        }
    }

    public void SetupPathfindingExample()
    {
        int l = width / 2 - 5;
        int h = height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++){
            for (int y = h - 5; y < h + 15; y++){
                tiles[x, y].Type = TileType.Floor_Rods;

                if( x == l || x == (l + 9) || y == h || y == (h + 9) )
                {
                    if (x != (l+9) && y != (h + 4))
                    {
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    public Tile GetTileAt(int x, int y)
    {
        if(x >= width || x < 0 || y >= height || y < 0)
        {
            //Debug.LogError("Tile ("+x+","+y+") is out of bounds.");
            return null;
        }
        return tiles[x, y];
    }

    public int getWidth()
    {
        return width;
    }
    
    public int getHeight()
    {
        return height;
    }


    public Furniture PlaceFurniture(string objectType, Tile t)
    {
        //TODO: This function assumes 1x1 objects with no rotation, change this later.
        
        //Error checking to make sure that key actually exists.
        if(furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("Installed Object prototypes doesn't contain prototype or key" + objectType);
            return null;
        }

        //Placing theo object on the given tile.
        //Also assign that object to a variable because it returns the installedobject in the same place function.
        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[objectType], t);

        if(furn == null)
        {
            // Failed to place object. Most likely there was already something there.
            return null;
        }

        furnitures.Add(furn);

        // Do we need to recalculate our rooms
        if (furn.roomEnclosure)
        {
            Room.DoRoomFloodFill(furn);
        }
        
        if(cbFurnitureCreated != null)
        {
            cbFurnitureCreated(furn);

            if (furn.movementCost != 1)
            {
                // Since tiles return movementcost as their base cost multiplied by the furniture's base cost
                // A furniture movement cost of exactly 1 doesn't impact our pathfinding system.
                // So we can occassionally avoid invalidating our pathfinding netowkr.
                InvalidateTileGraph(); // Reset the pathfinding system.
            }
        }

        return furn;
    }

    public void RegisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated += callbackfunc;
    }

    public void UnregisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated -= callbackfunc;
    }

    public void RegisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated += callbackfunc;
    }

    public void UnregisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated -= callbackfunc;
    }

    public void RegisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileChanged -= callbackfunc;
    }


    // Gets called whenever any tile changes.
    void OnTileChanged(Tile t)
    {
        if(cbTileChanged == null)
        {
            //If it's null then it will just return right away
            return;
        }
        //Otherwise it will call the registered functions in the callback.
        cbTileChanged(t);

        InvalidateTileGraph();
    }

    // This should be called whenever a change to the world means that our old pathfinding info is invalid
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    public bool IsFurniturePlacementValid(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    public Furniture GetFurniturePrototype(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("No furniture with type: " + objectType);
            return null;
        }

        return furniturePrototypes[objectType];
    }


    ////////////////////////////////////////////////////////////////////////////////
    /////
    /////                       SAVING & LOADING
    /////
    ////////////////////////////////////////////////////////////////////////////////
    
    public World()
    {
        
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].Type != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitures)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Character c in characters)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        Debug.Log("World::ReadXML");
        // Load info here

        width = int.Parse( reader.GetAttribute("Width") );
        height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(width, height);

        // Reader.read will read through the entire XML file one node at a time.
        while (reader.Read())
        {
            switch(reader.Name)
            {
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;

                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;

                case "Characters":
                    ReadXml_Characters(reader);
                    break;
            }
        }
    }

    void ReadXml_Tiles(XmlReader reader)
    {
        // We are in the "Tiles" element, so read elements until we run out of tile nodes.

        if (reader.ReadToDescendant("Tile"))
        {
            // We have at least one tile so do something.
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        // We are in the "Tiles" element, so read elements until we run out of tile nodes.
        if (reader.ReadToDescendant("Furniture"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y]);
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));

        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        // We are in the "Tiles" element, so read elements until we run out of tile nodes.
        if(reader.ReadToDescendant("Character"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character c = CreateCharacter(tiles[x, y]);

                c.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }

}
