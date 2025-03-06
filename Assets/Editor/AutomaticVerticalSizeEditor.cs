using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Editor class used to override the behavior of the vert script in the Unity editor
[CustomEditor(typeof(AutomaticVerticalSize))]
public class AutomaticVerticalSizeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector will allow the vert script to run as normal.
        DrawDefaultInspector();

        //Creates a button in the editor for vert class. When pushed it will return true to activate this if statement.
        if(GUILayout.Button("Recalc Size"))
        {
            AutomaticVerticalSize myScript = ((AutomaticVerticalSize)target);
            myScript.AdjustSize();
        }
    }
}
