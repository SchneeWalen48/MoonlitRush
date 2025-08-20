using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private BoxCollider collider;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if(collider != null)
        {
            collider.enabled = false;
        }        
    }

   public void ActiveTrigger()
    {
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("EndTrigger È°¼ºÈ­");
        }
    }


}
