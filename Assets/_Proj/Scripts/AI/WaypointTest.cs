using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTest : MonoBehaviour
{
    [Header("WayPoints")]
    public List<Transform> waypoints;

    //[Header("Speed Control")] //웨이포인트마다 목표로 하는 속도 리스트, 커브에서 속도 줄이기, 직선 구간에서 빠르게 달리기 조절 가능
    //public List<float> targetSpeedsPerWaypoint;

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0) return null;
        return waypoints[index % waypoints.Count];
    }

    //public float GetSpeedLimit(int index)
    //{
    //    if(targetSpeedsPerWaypoint == null || targetSpeedsPerWaypoint.Count == 0)
    //    {
    //        return 100f;
    //    }

    //    return targetSpeedsPerWaypoint[index % targetSpeedsPerWaypoint.Count];
    //}

    public int Count => waypoints.Count;
}

//수정
//웨이포인트마다 목표로 하는 속도 지정(수동)말고 고정적인 속도를 주는 게 나을수도
//왜냐면 커브 구간 등에서 강제 감속 코드를 넣었기 때문 그리고 스피드 발판의 존재로 수동으로 속도를 지정하다보니
//잠시 맥스스피드가 늘어도 지정한 속도가 맥스스피드에 못 미치기에 있으나 마나가 됨