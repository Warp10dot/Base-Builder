using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Node<T>
{
    public T data;

    public Path_Edge<T>[] edges; //Nodes leading OUT from this node
    // Nodes don't actually know what edges lead into it
}
