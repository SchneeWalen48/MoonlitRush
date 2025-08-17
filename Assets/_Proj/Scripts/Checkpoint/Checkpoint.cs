using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Check Point에 스크립트 적용
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public int checkpointId;
    public bool isFinalCheckpoint = false;
    public Checkpoint nextCheckpoint;

    public void SetNextCheckpoint(Checkpoint next)
    {
        nextCheckpoint = next;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인
        //콜라이더는 자식에 Info스크립트는 부모에 있음
        RacerInfo racer = other.GetComponentInParent<RacerInfo>();
        if (racer != null)
        {
            racer.lapCounter.PassCheckpoint(this);
        }
    }
}
