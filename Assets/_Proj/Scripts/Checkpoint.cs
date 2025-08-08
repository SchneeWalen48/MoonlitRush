using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int index;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var tracker = other.GetComponent<RaceManager>();
        if (tracker != null)
        {
            
        }
    }
}
