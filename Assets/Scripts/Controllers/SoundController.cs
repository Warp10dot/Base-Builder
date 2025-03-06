using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    
    float soundCooldown = 0f;

    // Start is called before the first frame update
    void Start()
    {
        WorldController.Instance.World.RegisterFurnitureCreated(OnFurnitureCreated);
        WorldController.Instance.World.RegisterTileChanged(OnTileTypeChanged);
    }

    // Update is called once per frame
    void Update()
    {
        if (soundCooldown > 0)
        {
            soundCooldown -= Time.deltaTime;
        }
    }

    void OnTileTypeChanged(Tile tile_data)
    {
        if(soundCooldown > 0)
        {
            //Cooldown timer. If it's not 0 than we bail out
            return;
        }
        //FIXME
        AudioClip ac = Resources.Load<AudioClip>("Sounds/Floor_OnCreated");
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }

    void OnFurnitureCreated(Furniture furn)
    {
        if (soundCooldown > 0)
        {
            //Cooldown timer. If it's not 0 than we bail out
            return;
        }
        //FIXME
        AudioClip ac = Resources.Load<AudioClip>("Sounds/" + furn.objectType + "_OnCreated");

        if(ac == null)
        {
            // No specific sound for that type of furniture
            //So we just use a default sound
            ac = Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
        }

        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }
}
