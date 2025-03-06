using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{
    Dictionary<Character, GameObject> characterGameObjectMap;
    //Dictionary for character sprites
    Dictionary<string, Sprite> characterSprites;

    //Creating a getter which returns the world instance. This is only for ease of writing and doesn't change anything.
    World World
    {
        get { return WorldController.Instance.World; }
    }



    // Start is called before the first frame update
    void Start()
    {
        LoadSprites();

        characterGameObjectMap = new Dictionary<Character, GameObject>();

        //Registering callback so that we know whenever a furniture changes.
        World.RegisterCharacterCreated(OnCharacterCreated);

        //c.SetDestination(World.GetTileAt(World.Width / 2 + 5, World.Height / 2));

        // Check for pre-existing characters, which don't do the callback.
        foreach (Character c in World.characters)
        {
            OnCharacterCreated(c);
        }

    }

    void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        //Resources will load things from the resources folder. Typically images and whatnot are not loaded at runtime without a reference unless we do this
        //This is important because we want to switch between wall graphics at runtime! 
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");

        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(Character c)
    {
        //Create a game object linked to this data.

        //Converting current looped at coordinates into a game object (unity thing)
        GameObject char_go = new GameObject();

        //Adding the tile data (Key, class) and the tile go (GameObject, sprite) to the map.
        characterGameObjectMap.Add(c, char_go);

        char_go.name = "Character";

        //Setting the position on screen for each tile
        char_go.transform.position = new Vector3(c.X, c.Y+0.2f, 0); //NOTE: Adding 0.2f to the Y coord to make the character appear to stand on the tile correctly. This is because center pivot. Maybe come back to this? OR future sprites will beb etteR?

        //Setting the tile owner/parent to the world controller, this just makes them appear under world controller in the hierarchy to clean it up a bit
        char_go.transform.SetParent(this.transform, true);

        //Sprite
        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = characterSprites["p1_front"]; //FIXME
        sr.sortingLayerName = "CharacterUI";

        //This used to assign a callback function to each tile, this is called whenever the tile type is set so that it also updates the sprite graphic.
        //Still don't fully get how this works but whateva
        //Register our callback so that our GameObject gets updated whenever the furniture's type gets changed.
        c.RegisterOnChangedCallback(OnCharacterChanged);
    }

    void OnCharacterChanged(Character c)
    {
        //Make sure the character's graphics are correct.
        if (characterGameObjectMap.ContainsKey(c) == false)
        {
            Debug.LogError("OnCharacterChanged -- trying to change a visual for a character not in our map.");
            return;
        }

        GameObject char_go = characterGameObjectMap[c];
        //char_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(character);

        char_go.transform.position = new Vector3(c.X, c.Y+0.2f, 0);
    }
}
