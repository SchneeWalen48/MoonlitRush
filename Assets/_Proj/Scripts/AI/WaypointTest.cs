using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTest : MonoBehaviour
{
    [Header("WayPoints")]
    public List<Transform> waypoints;
    
    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0) return null;
        return waypoints[index % waypoints.Count];
    }
    
    public int Count => waypoints.Count;
}
