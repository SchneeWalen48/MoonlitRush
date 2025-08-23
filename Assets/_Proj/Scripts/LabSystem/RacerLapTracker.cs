using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RacerLapTracker : MonoBehaviour
{
    [Header("State")]
    public int currentLap = 0; 
    public bool hasArmed = false;  
    public bool finished = false;

    [HideInInspector] public float lastCrossTime = -999f;
    [HideInInspector] public float lapStartTime = 0f;
}
