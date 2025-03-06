using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Floor_Rods;
    string buildModeObjectType;

    // Start is called before the first frame update
    void Start()
    {

    } 

    public void SetMode_BuildFloor()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Floor_Rods;
    }

    public void SetMode_Bulldoze()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        //Wall is not a tile. Wall is an installed object that exists on top of a tile.
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

    public void Pathingtest()
    {
        WorldController.Instance.World.SetupPathfindingExample();
    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects == true)
        {
            //Create the installed object and assign it to the tile.

            //Check to see if we can actually build the furniture here.
            string furnitureType = buildModeObjectType; //Creating a temp variable for this just in case it changes. 

            if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, t) && t.pendingFurnitureJob == null)
            {
                //This tile position is valid for thsi furniture
                //Create a job for it to be built.

                //FIXME: This instantly builds walls.
                //Using a lambda here (anonymous function) to call OnFurnitureJobComplete in order to pass it a function for the callback
                //Add the job to the queue.
                Job j = new Job(t, furnitureType, (theJob) => { WorldController.Instance.World.PlaceFurniture(furnitureType, theJob.tile); t.pendingFurnitureJob = null; });
                
                //FIXME: I don't like having to manually and explicitly set flags that prevent conflicts
                //It's too easy to forget to set/clear them.
                t.pendingFurnitureJob = j;

                j.RegisterJobCancelCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });

                WorldController.Instance.World.jobQueue.Enqueue(j); //Queueing this job up.

            }
        }
        else
        {
            //We are in tile-changing mode.
            t.Type = buildModeTile;
        }
    }
}
