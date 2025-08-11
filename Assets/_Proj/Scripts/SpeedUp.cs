using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedUp : MonoBehaviour, ISpeedUp
{
    public float boostForce = 20f;//가속력
    public float boostDuration = 3f; //지속시간

    //public void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        var controller = other.GetComponentInParent<CarController>();
    //        if (controller != null)
    //        {
    //            //controller.ApplySpeedPadBoost(boostForce, boostDuration);
    //        }
    //    }
    //}

    //raycast 방식
    public void ApplySpeedUp(GameObject car)
    {
        //AI용
        var controller = car.GetComponentInParent<CarController>();
        if (controller != null)
        {
            controller.ApplySpeedPadBoost(boostForce, boostDuration);

        }



    }

}
