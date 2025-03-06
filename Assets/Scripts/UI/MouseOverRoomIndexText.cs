using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomIndexText: MonoBehaviour
{

    // Every frame this script checks to see which tile is under the mouse
    // And then updates the GetComponent<Text>.text parameter of the game object it is attached to.


    Text myText;
    MouseController mouseController;

    // Start is called before the first frame update
    void Start()
    {
        myText = GetComponent<Text>();

        if(myText == null)
        {
            Debug.LogError("MouseOverTileTypeText: No textUI component on this object");
            this.enabled = false;
            return;
        }

        mouseController = GameObject.FindObjectOfType<MouseController>();
        if(mouseController == null)
        {
            Debug.LogError("How do we not have an instance of MouseController?");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Tile t = mouseController.GetMouseOverTile();

        // IndexOf will find the index of the given object in that array
        // If it is not in that array it will return -1.
        myText.text = "Room Index: " + t.world.rooms.IndexOf(t.room).ToString();
    }
}
