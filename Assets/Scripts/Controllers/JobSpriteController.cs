using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{

    //This bare-bones controller is mostly just going to piggyback on furniture sprite controller
    //because we don't yet fully know what our job system is going to look like in the end.

    FurnitureSpriteController fsc;

    Dictionary<Job, GameObject> jobGameObjectMap;

    // Start is called before the first frame update
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();

        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();

        //FIXME: No such thing as a jobqueue yet.
        WorldController.Instance.World.jobQueue.RegisterJobCreationCallback(OnJobCreated);
    }

    void OnJobCreated(Job job)
    {
        //FIXME: We can only do furniture jobs right now.

        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("OnJobCreated - for a jobGO that already exists -- Most likely job being requeued as opposed to being created");
            return;
        }

        //TODO: Sprite.        
        GameObject job_go = new GameObject();

        //Adding the tile data (Key, class) and the tile go (GameObject, sprite) to the map.
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_" + job.tile.X + "_" + job.tile.Y;

        //Setting the position on screen for each tile
        job_go.transform.position = new Vector3(job.tile.X, job.tile.Y, 0);

        //Setting the tile owner/parent to the world controller, this just makes them appear under jobsprite controller in the hierarchy to clean it up a bit
        job_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);

        //Assigning a sorting layer so that furniture appears before tiles else.
        job_go.GetComponent<SpriteRenderer>().sortingLayerName = "Jobs";

        //FIXME: This hard coding is not ideal!
        if (job.jobObjectType == "Door")
        {
            // By default the door graphic is meant for walls on the east and west.
            // Check to see if we actually have a wall north/south, then if so rotate this gameobject by 90 degrees.
            Tile northTile = job.tile.world.GetTileAt(job.tile.X, job.tile.Y + 1);
            Tile southTile = job.tile.world.GetTileAt(job.tile.X, job.tile.Y - 1);

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null
                && northTile.furniture.objectType == "Wall" && southTile.furniture.objectType == "Wall")
            {
                job_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        //FIXME: We can only do furniture jobs right now.

        //This executes whether a job was completed or cancelled.

        GameObject job_go = jobGameObjectMap[job];

        job.UnregisterJobCompleteCallback(OnJobEnded);
        job.UnregisterJobCancelCallback(OnJobEnded);

        Destroy(job_go);
    }
}
