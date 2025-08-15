using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;

    private void Start()
    {
    RaceManager.Instance.RegisterCheckpoint(this);
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인
       //콜라이더는 자식에 Info스크립트는 부모에 있음
        RacerInfo racer = other.GetComponentInParent<RacerInfo>();
        if (racer != null)
        {
            racer.lapCounter.PassCheckpoint(checkpointIndex);
        }
    }
}

//체크포인트 설치는 코너와 턴 전이 좋다고 한다만...