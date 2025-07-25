using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions
{
    // This file contains code that will likely be completely moved to some LUA files at some point
    // And will be parsed at runtime.

    public static void Door_UpdateAction (Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction");

        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness", deltaTime *4); // FIXME: Maybe a door open speed parameter.
            
            if(furn.GetParameter("openness") >= 1)
            {
                furn.SetParameter("is_opening", 0);
            }
        }
        else
        {
            furn.ChangeParameter("openness", deltaTime * -4);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")) );

        if (furn.cbOnChanged != null)
        {
            furn.cbOnChanged(furn);
        }
    }

    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }
}
