using System.Collections.Generic;
using UnityEngine;

public class ChekcPointManager : MonoBehaviour
{
  public List<CheckPoint> allCheckPoints;

  void Start()
  {
    InitCheckPoints();
  }

  void InitCheckPoints()
  {
    for(int i = 0; i < allCheckPoints.Count; i++)
    {
      if(i < allCheckPoints.Count - 1)
      {
        allCheckPoints[i].SetNextCheckpoint(allCheckPoints[i + 1]);
      }
      else
      {
        allCheckPoints[i].SetNextCheckpoint(allCheckPoints[0]);
        allCheckPoints[i].isFinalCheckpoint = true;
      }
    }
  }
}
