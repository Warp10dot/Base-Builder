using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureSpriteController : MonoBehaviour
{
    Dictionary<Furniture, GameObject> furnitureGameObjectMap;
    //Dictionary for furniture sprites
    Dictionary<string, Sprite> furnitureSprites;

    //Creating a getter which returns the world instance. This is only for ease of writing and doesn't change anything.
    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadSprites();

        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        //Registering callback so that we know whenever a furniture changes.
        World.RegisterFurnitureCreated(OnFurnitureCreated);

        // Go through any EXISTING Furniture (I.E, from a save that was loaded OnEnable() and call the oncreated event manually)
        foreach (Furniture furn in World.furnitures)
        {
            OnFurnitureCreated(furn);
        }
    }

    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        //Resources will load things from the resources folder. Typically images and whatnot are not loaded at runtime without a reference unless we do this
        //This is important because we want to switch between wall graphics at runtime! 
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            furnitureSprites[s.name] = s;
        }
    }

    public void OnFurnitureCreated(Furniture furn)
    {
        //Create a game object linked to this data.

        //FIXME: Does not consider multi-tile objects nor rotated objects.

        //Converting current looped at coordinates into a game object (unity thing)
        GameObject furn_go = new GameObject();

        //Adding the tile data (Key, class) and the tile go (GameObject, sprite) to the map.
        furnitureGameObjectMap.Add(furn, furn_go);

        furn_go.name = furn.objectType + "_" + furn.tile.X + "_" + furn.tile.Y;

        //Setting the position on screen for each tile
        furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);

        //Setting the tile owner/parent to the world controller, this just makes them appear under world controller in the hierarchy to clean it up a bit
        furn_go.transform.SetParent(this.transform, true);

        //FIXME: This hard coding is not ideal!
        if (furn.objectType == "Door")
        {
            // By default the door graphic is meant for walls on the east and west.
            // Check to see if we actually have a wall north/south, then if so rotate this gameobject by 90 degrees.
            Tile northTile = World.GetTileAt(furn.tile.X, furn.tile.Y + 1);
            Tile southTile = World.GetTileAt(furn.tile.X, furn.tile.Y - 1);

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null
                && northTile.furniture.objectType == "Wall" && southTile.furniture.objectType == "Wall")
            {
                furn_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        //Sprite
        SpriteRenderer sr = furn_go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForFurniture(furn);
        sr.sortingLayerName = "FurnitureUI";

        //This used to assign a callback function to each tile, this is called whenever the tile type is set so that it also updates the sprite graphic.
        //Still don't fully get how this works but whateva
        //Register our callback so that our GameObject gets updated whenever the furniture's type gets changed.
        furn.RegisterOnChangedCallback(OnFurnitureChanged);
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        string spriteName = furn.objectType;

        if (furn.linksToNeighbour == false)
        {
            // If this is a DOOR, let's check openness and update the sprite
            // FIXME: All this hardcodingn eeds to be generalized later.
            if (furn.objectType == "Door")
            {
                if (furn.GetParameter("openness") < 0.5f)
                {
                    // Door is closed.
                    spriteName = "Door";
                }
                else if (furn.GetParameter("openness") < 0.5f)
                {
                    // Door is a bit open.
                    spriteName = "Door_openness_1";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {
                    // Door is a lot open.
                    spriteName = "Door_openness_2";
                }
                else
                {
                    // Door is open.
                    spriteName = "Door_openness_3";
                }
            }

            return furnitureSprites[spriteName];
        }

        //Otherwise the sprite is more complicated.

        spriteName = furn.objectType + "_";

        int x = furn.tile.X;
        int y = furn.tile.Y;

        //Check for neighbors. North, East, South, West order.
        Tile t;

        t = World.GetTileAt(x, y + 1);
        if (t!=null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "N";
        }
        t = World.GetTileAt(x+1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "E";
        }
        t = World.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "S";
        }
        t = World.GetTileAt(x-1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "W";
        }

        //For example, if this object has all 4 neighbours of the same type then the string will be _NESW

        if (furnitureSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("No sprite with name " + spriteName);
            return null;
        }

        return furnitureSprites[spriteName];
    }

    public Sprite GetSpriteForFurniture(string objectType)
    {
        if (furnitureSprites.ContainsKey(objectType))
        {
            return furnitureSprites[objectType];
        }

        if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            return furnitureSprites[objectType + "_"];
        }
        Debug.LogError("No sprite with name " + objectType);
        return null;
    }


    void OnFurnitureChanged(Furniture furn)
    {
        //Make sure the furniture's graphics are correct.
        if(furnitureGameObjectMap.ContainsKey(furn) == false)
        {
            Debug.LogError("OnFurnitureChanged -- trying to change a visual for a furniture not in our map.");
            return;
        }

        GameObject furn_go = furnitureGameObjectMap[furn];
        furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
    }
}
