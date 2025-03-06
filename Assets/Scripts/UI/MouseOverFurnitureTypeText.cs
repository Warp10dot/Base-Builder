using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverFurnitureTypeText : MonoBehaviour
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

        string s = "NULL";

        if(t.furniture != null)
        {
            s = t.furniture.objectType;
        }

        myText.text = "Furniture: " + s;
    }
}
