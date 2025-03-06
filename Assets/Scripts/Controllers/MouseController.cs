using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    Vector3 lastFramePosition;
    Vector3 currFramePosition;

    Vector3 dragStartPosition;

    public GameObject circleCursorPrefab;
    List<GameObject> dragPreviewGameObjects;

    // Start is called before the first frame update
    void Start()
    {
        //Initializes the previously created list. 
        dragPreviewGameObjects = new List<GameObject>();
    }

    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currFramePosition;
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.World.GetTileAt(
            Mathf.RoundToInt(currFramePosition.x), 
            Mathf.RoundToInt(currFramePosition.y)
            );
    }

    // Update is called once per frame
    void Update()
    {
        //currFramePosition will be the exact location the mouse is at. We'll be using this a lot.
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0; // Doing this because the camera is at z coords -10 and so is the default z position, when the image and cursor are too close the cursor won't show.

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();


        //Save the mouse position for this frame.
        //We don't use currFramePosition because we may have moved the camera.
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;

    }

    void UpdateDragging()
    {
        //Note: Difference between down up and normal for getmousebutton. Up and down check whether it was pushed within the last frame,
        //just GetMouseButton checks whether it's currently down

        //If we are over a UI element, bail out
        //EventSystem handles the UI elements and stuff. Current is just a pointer to the only one like Camera.main is
        //IsPointerOverGameObject doesn't check all game objects (Like Tiles in this program) but only game objects whcih have events, like buttons.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            //Just saying return will halt the method right here and prevent us from making tiles and clicking buttons at the same time.
            return;
        }

        //Handle left mouse clicks
        //Start drag behavior.
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFramePosition;
        }

        int start_x = Mathf.RoundToInt(dragStartPosition.x); //Starting position of the drag
        int end_x = Mathf.RoundToInt(currFramePosition.x); //Current position of the drag

        //We may be dragging backwards so flip things if needed
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }

        int start_y = Mathf.RoundToInt(dragStartPosition.y); //Starting position of the drag
        int end_y = Mathf.RoundToInt(currFramePosition.y); //Current position of the drag
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        //Clean up old drag positions
        while(dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0]; //Gets a gameobject from the list
            dragPreviewGameObjects.RemoveAt(0); //Removing that game object from the list (This isn't the same as destroying it)
            SimplePool.Despawn(go); //We no longer destroy the object because destroy is expensive, instead we use code which hides it for us.
        }

        if (Input.GetMouseButton(0))
        {
            //Display a preview of the drag area
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);
                    if (t != null)
                    {
                        //Display the building preview on top of this tile
                        GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        //We don't use instantiate anymore because it is expensive,
                        //we instead use this prewritten code which has a list of objects which already exist, and are simply hidden, moved and whatnot.
                        //Quaternion.identity aligns the rotation to the same as the world. 
                        //GameObject in brackets before instantiate casts the thing into a game object so we can pass it to the go variable.
                        go.transform.SetParent(this.transform,true); //This just makes the parent in the unity engine mousecontroller, instead of spamming 100 gos into the GO list.
                        dragPreviewGameObjects.Add(go);
                        //Above code adds the variable to the list of game objects.
                    }
                }
            }
        }


        //This code is for creating and deleting basic floor tiles via drag behavior.
        //Also ends drag behavior.
        if (Input.GetMouseButtonUp(0))
        {

            BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();

            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    //End drag will look up each of the tiles we are currently selecting here
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);

                    if(t != null) {
                        //If the tile isn't null, we do stuff to it.
                        //Call BuildModeController DoBuild()
                        bmc.DoBuild(t);
                    }
                }
            }
        }
    }

    void UpdateCameraMovement()
    {
        //Handle screen dragging
        //Just get mouse button checks whether the mouse button is being held down.
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))//Right or middle mouse button
        {
            //Calculates the difference between the last frame and current frame position of the mouse, this variable is then used to tell the camera where to move
            Vector3 diff = lastFramePosition - currFramePosition;
            //Takes the difference we just calculated and moves the camera by that much
            Camera.main.transform.Translate(diff);
        }

        //Scrolling. We multiple the orthographic size by the input to create a proportional change so that it doesn't feel slower at max zoom.
        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

        //Limiting the amount we can zoom in and out using mathf.clamp
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
    }
}
