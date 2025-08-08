using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedUp : MonoBehaviour, ISpeedUp
{
    public float boostForce = 10f;//가속력
    public float boostDuration = 3f; //지속시간

    //raycast 방식
    public void ApplySpeedUp(GameObject car)
    {
        //AI용
        var controller = car.GetComponent<AICarController>();
        if (controller != null) {
        controller.ApplySpeedPadBoost(boostForce, boostDuration);
            
        }

        //플레이어용 raycast 감지

    }

    


    //트리거 방식
    //private void OnTriggerEnter(Collider other)
    //{
    //    var speedPad = other.GetComponent<ISpeedUp>();
    //    if (speedPad != null)
    //    {
    //        speedPad.ApplySpeedUp(boostForce, boostDuration);
    //    }
    //}
}
