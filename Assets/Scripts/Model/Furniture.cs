using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

//Installed objects are things like furniture, walls, doors, etc which are bolted to the floor. 

public class Furniture : IXmlSerializable {

    /// <summary>
    /// Custom paramter for this particular piece of furniture
    /// using a dictionary because later custom LUA functions will be
    /// able to use whatever the user/modders would like
    /// basically the LUA code will bind to this dictionary.
    /// </summary>
    protected Dictionary<string, float> furnParameters;

    /// <summary>
    /// These actions are called every update. They get passed the furniture they belong to plus a deltaTime
    /// </summary>
    protected Action<Furniture, float> updateActions;

    //Actions don't returnanything, funcs do.
    public Func<Furniture, ENTERABILITY> IsEnterable;

    public void Update(float deltaTime)
    {
        if (updateActions != null)
        {
            updateActions(this, deltaTime);
        }
    }

    //This represents the BASE object of the tile, but in practice large objects may occupy multiple tiles.
    public Tile tile { get; protected set; }
        

    //This "objectType" will be queried by the visual system to know what sprite to render for this object.
    public string objectType
    {
        get;protected set;
    }

    //This is a multiplier so a value of 2 here means you move twice as slowly.
    //Tile types and other environmental effects may further act as multipliers.
    //For example a "rough" tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    //Total movement cost of 3+3+2 = 8 so you'd move through this tile at 1/8th normal speed.
    //SPECIAL: If movement cost is equal to 0 then this tile is impassable e.g. a wall.
    public float movementCost { get; protected set; }

    public bool roomEnclosure { get; protected set; }

    //Setting the width and height of the object. For example, a sofa might be 3x2, the graphics may only appear to be 3x1 but the extra row is for leg room.
    int width;
    int height;

    //Whether or not needs to change sprites to give a linked visual appearance (E.g, Walls, power cables.)
    public bool linksToNeighbour
    {
        get; protected set;
    } 

    //Action callback
    public Action<Furniture> cbOnChanged;

    //Action always returns a void, Func will return something
    //This callback type thing is used to validate whether or not the current position is valid for furniture
    Func<Tile, bool> funcPositionValidation;

    //TODO Impliment larger objects
    //TODO Impliment object rotation


    // Empty constructor is used for serialization.
    public Furniture()
    {
        furnParameters = new Dictionary<string, float>();

    }

    // Copy constructor -- don't call this directly, unless we never do ANY subclassing.
    // Instead use Clone() which is more virtual.
    protected Furniture( Furniture other )
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.roomEnclosure = other.roomEnclosure;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbour = other.linksToNeighbour;

        this.furnParameters = new Dictionary<string, float>(other.furnParameters);

        if (other.updateActions != null)
        {
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
        }

        this.IsEnterable = other.IsEnterable;
    }

    // Make a copy of the current Furniture, sub-classed should override this Clone() if a different (sub-classed) copy constructor should be run.
    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    // Create Furniture from parameters, this will probably only ever be used for prototypes.
    public Furniture(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool roomEnclosure = false )
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclosure = roomEnclosure;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;

        this.funcPositionValidation = this.DEFAULT__IsValidPosition;

        furnParameters = new Dictionary<string, float>();
    }

    //This will take an Installed Object prototype and a tile so that it knows where to place the newly installed object.
    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance position validity function returned false");
            return null;
        }

        //At this point - we know our placement validation is valid.

        Furniture furn = proto.Clone();

        furn.tile = tile;

        //FIXME: This assumes we are 1x1
        if(tile.PlaceFurniture(furn) == false)
        {
            //For some reason we weren't able to place our object in this tile. Probably it was already occupied.
            
            //Do NOT return our newly instantiated object, instead it will be garbage collected.
            return null;
        }

        if (furn.linksToNeighbour)
        {
            //This type of furniture links to neighbours
            //so we should inform our neighbours that they have a new buddy.
            //Just trigger their onChanged callback.

            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                //We have a northern neighbour with the same object type as us. So tell it that it has changed
                //by firing its callback.
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return furn;
    }

    public void RegisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged -= callbackFunc;
    }


    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    //FIXME: These functions should never be called directly so they probably shouldn't be public functions of furniture.
    // This will be replaced by validation checks fed to us from LUA files that will be customizable for each piece of furniture.
    // For example, a door might specify that it needs two walls to connect to.
    protected bool DEFAULT__IsValidPosition(Tile t)
    {
        //Make sure tile is Floor
        if(t.Type != TileType.Floor_Rods)
        {
            return false;
        }


        //Make sure tile doesn't already have a furniture.
        if(t.furniture != null)
        {
            return false;
        }

        return true;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString()); 
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);
        //writer.WriteAttributeString("movementCost", movementCost.ToString());

        foreach(string k in furnParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // X, Y, and objectType have already been set, and we should already be assigned to a tile.
        // So just read extra data.
        //movementCost = int.Parse(reader.GetAttribute("movementCost") );

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));

                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }


    /// <summary>
    /// Gets the custom furniture parameter from the string key
    /// </summary>
    /// <param name="key">Key string</param>
    /// <param name="default_value">Default value</param>
    /// <returns>The parameter value as a float</returns>
    public float GetParameter(string key, float default_value = 0)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            return default_value;
        }
        return furnParameters[key];
    }

    public void SetParameter(string key, float value)
    {
        furnParameters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            furnParameters[key] = value;
        }
        furnParameters[key] += value;
    }


    /// <summary>
    /// Register a function that will be called every update
    /// Later this implementation might change a bit as we support LUA.
    /// </summary>
    public void RegisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions += a;
    }

    public void UnregisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions -= a;
    }

}
