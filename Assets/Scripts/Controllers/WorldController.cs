using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    //Creating an instance of world controller so that other classes can use it to do things.
    //Also creates a getter which other classes will use, but this will prevent them from being able to change it.
    public static WorldController Instance { get; protected set; }


    public World World { get; protected set; }

    // Making this static so that it belongs to the class. This means it won't get reset when we reload the scene.
    static bool loadWorld = false;

    // Start is called before the first frame update
    void OnEnable()
    {
        if(Instance != null)
        {
            Debug.LogError("There should never be more than one world controller.");
        }
        Instance = this;

        if (loadWorld)
        {
            loadWorld = false;
            CreateWorldFromSaveFile();
        }
        else
        {
            CreateEmptyWorld();
        }

    }

    void Update()
    {
        //TODO: Add pause, unpause, speed controls, etc.
        World.Update(Time.deltaTime);
    }

    public Tile getTileAtWorldCoord(Vector3 coord)
    {
        //Coords are floats, so we convert them to integers using mathf.floortoint
        int x = Mathf.RoundToInt(coord.x);
        int y = Mathf.RoundToInt(coord.y);

        //Getting tile position using the instance we created in world controller.
        return WorldController.Instance.World.GetTileAt(x, y);
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld button was clicked.");

        // Getting the current SCENE(Unity object referencing currently loaded stuff) and resetting it.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SaveWorld()
    {
        Debug.Log("Save button was clicked.");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, World);
        writer.Close();

        Debug.Log(writer.ToString());

        PlayerPrefs.SetString("SaveGame00", writer.ToString());

    }

    public void LoadWorld()
    {
        Debug.Log("Load button was clicked.");

        // Reload the scene to reset all data (and purge old references)
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld()
    {
        //Create a world with empty tiles
        World = new World(100,100);

        //Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSaveFile()
    {
        Debug.Log("CreateWorldFromSaveFile");
        //Create a world from our save file data.

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame00"));
        Debug.Log(reader.ToString()); 
        World = (World)serializer.Deserialize(reader);
        reader.Close();


        //Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
    }
}
