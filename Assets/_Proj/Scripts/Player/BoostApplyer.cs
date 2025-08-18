using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostApplyer : MonoBehaviour
{
  [Header("FX")]
  public BoostFX fx;
  public CarController controller;
  [Header("Timing")]
  public float fadeTime = 0.15f;

  float boostEndTime = -1f;
  Coroutine fxRoutine;

  public void ApplyBoost(float duration, float sizeMul = 1f, float speedMul = 1f)
  {
    float newEnd = Time.time + Mathf.Max(0f, duration);
    boostEndTime = Mathf.Max(boostEndTime, newEnd);

    if(fxRoutine != null) StopCoroutine(fxRoutine);
    fxRoutine = StartCoroutine(CoBoostFx(sizeMul, speedMul));
  }

  IEnumerator CoBoostFx(float sizeMul, float speedMul)
  {
    fx.LerpToBoost(fadeTime, sizeMul, speedMul);
    while (Time.time < boostEndTime)
    {
      yield return null;
    }
    fx.LerpToNormal(fadeTime);
    fxRoutine = null;
  }
}
