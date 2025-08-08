using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//플레이어, AI 모두 적용하는 인터페이스
public interface ISpeedUp
{
    void ApplySpeedUp(GameObject car); //감지한 차량
}
