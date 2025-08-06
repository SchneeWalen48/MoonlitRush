using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTest : MonoBehaviour
{
    [Header("WayPoints")]
    public List<Transform> waypoints;

    [Header("Speed Control")] //웨이포인트마다 목표로 하는 속도 리스트, 커브에서 속도 줄이기, 직선 구간에서 빠르게 달리기 조절 가능
    public List<float> targetSpeedsPerWaypoint;

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0) return null;
        return waypoints[index % waypoints.Count];
    }

    public float GetSpeedLimit(int index)
    {
        if(targetSpeedsPerWaypoint == null || targetSpeedsPerWaypoint.Count == 0)
        {
            return 100f;
        }

        return targetSpeedsPerWaypoint[index % targetSpeedsPerWaypoint.Count];
    }

    public int Count => waypoints.Count;
}
