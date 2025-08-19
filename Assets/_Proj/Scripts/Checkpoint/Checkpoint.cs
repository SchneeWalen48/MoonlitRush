using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Check Point에 스크립트 적용
public class Checkpoint : MonoBehaviour
{
    public int checkpointId;
    public bool isFinalCheckpoint = false;
    public Checkpoint nextCheckpoint;

    public void SetNextCheckpoint(Checkpoint next)
    {
        nextCheckpoint = next;
    }
}
