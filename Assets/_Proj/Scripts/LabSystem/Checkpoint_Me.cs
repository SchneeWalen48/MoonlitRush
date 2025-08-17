using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint_Me : MonoBehaviour
{
    public int checkpointIndex;

    private void Start()
    {
   // RaceManager.Instance.RegisterCheckpoint(this);
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인
       //콜라이더는 자식에 Info스크립트는 부모에 있음
        RacerInfo racer = other.GetComponentInParent<RacerInfo>();
        if (racer != null)
        {
         //   racer.lapCounter.PassCheckpoint(checkpointIndex);
        }
    }
}
