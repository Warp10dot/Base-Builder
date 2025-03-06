using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutomaticVerticalSize : MonoBehaviour
{
    public float childHeight = 35f;

    // Start is called before the first frame update
    void Start()
    {
        AdjustSize();
    }

    public void AdjustSize()
    {
        //We can't set it directly, so we use the below code to pass the size data to a variable
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        //Below code changes the y value of the size object based on the number of children and their desired height
        size.y = this.transform.childCount * childHeight;
        //Pass the entire size object to sizeDelta because we can't modify individual varibles in it.
        this.GetComponent<RectTransform>().sizeDelta = size;

    }
}
