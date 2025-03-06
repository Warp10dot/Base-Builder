using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpriteController : MonoBehaviour
{

    public Sprite floorSprite; //FIXME - We aren't going to want sprites hard coded in
    public Sprite emptySprite;

    //Creating a dictionary to map a Tile to its Game Object. Dictionaries are just maps from Java.
    Dictionary<Tile, GameObject> tileGameObjectMap;
    
    //Creating a getter which returns the world instance. This is only for ease of writing and doesn't change anything.
    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void Start()
    {

        //Instantiate our dictionary which tracks which gameobject is rendering which tile data. 
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

        //Loops through all tiles and creates a game object for our tiles so they show visually 
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                //Tile data is literally just the data of the tile (coords)
                Tile tile_data = World.GetTileAt(x, y);

                //Converting current looped at coordinates into a game object (unity thing)
                GameObject tile_go = new GameObject();

                //Adding the tile data (Key, class) and the tile go (GameObject, sprite) to the map.
                tileGameObjectMap.Add(tile_data, tile_go);

                tile_go.name = "Tile_" + x + "_" + y;

                //Setting the position on screen for each tile
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);

                //Setting the tile owner/parent to the world controller, this just makes them appear under world controller in the hierarchy to clean it up a bit
                tile_go.transform.SetParent(this.transform, true);

                //Sprite
                SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
                sr.sprite = emptySprite;
                sr.sortingLayerName = "TileUI";

                onTileChanged(tile_data);
            }
        }

        //Registering callbacks so that we know whenever a tile or furniture changes.
        World.RegisterTileChanged(onTileChanged);
    }

    //THIS IS AN EXAMPLE -- NOT CURRENTLY USED.
    void DestroyAllTileGameObjects()
    {
        //This function might get called when we change levels/maps.
        //We need to destroy all visual GameObjects but not the tile data.

        while(tileGameObjectMap.Count > 0)
        {
            //Getting the tile data at key 0 and putting it into temp variable
            Tile tile_data = tileGameObjectMap.Keys.First();
            //Finding the corresponding game object to the above code and assigning to variable
            GameObject tile_go = tileGameObjectMap[tile_data];
            //Removing the pair from the map
            tileGameObjectMap.Remove(tile_data);

            //Unregister the callback!
            //This means that when the tile type gets changed, it won't call onTileChanged function!!!!!
            tile_data.UnregisterTileTypeChangedCallback(onTileChanged);

            //Destroy the game object
            Destroy(tile_go);
        }

        //Presumably after this function gets called we'd be calling another function to fill all the game objects for the tiles on the new floor/level.
    }

    //This runs every time a tile type is changed to upgrade the sprite graphic (Will also probably do other shit later)
    void onTileChanged(Tile tile_data)
    {
        if(tileGameObjectMap.ContainsKey(tile_data) == false)
        {
            //Checking to see if there is an actual game object assigned to that key. Return just breaks the function immediately.
            Debug.LogError("tileGameObjectMap doesn't contain the tile data. Did you forget to add the tile to the dicktionary? Or maybe forget to unregister callback?");
            return; 
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if(tile_go == null)
        {
            //Same as above error check 
            Debug.LogError("tileGameObjectMap's gameobject is null. Did you forget to add the tile to the dicktionary? Or maybe forget to unregister callback?");
            return;
        }

        if(tile_data.Type == TileType.Floor_Rods)
        {
            //If the data type is floor rods, change sprite to appropriate one
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;
        }
        else if(tile_data.Type == TileType.Empty)
        {
            //Else change it to empty
            tile_go.GetComponent<SpriteRenderer>().sprite = emptySprite;
        }
        else
        {
            Debug.LogError("On Tile Type Changed - Unrecognized tile type");
        }
    }
}
