using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostFX : MonoBehaviour
{
  [Header("FX Reference")]
  public ParticleSystem boostTrail; // Exhaust_Trail 프리팹 연결하면 됨

  [Header("Normal State")]
  public Color normalColor = Color.white;
  public float normalStartSize = 0.4f;
  public float normalStartSpeed = 2f;

  [Header("Boost State")]
  public Color boostColor = Color.cyan;
  public float boostStartSize = 1f;
  public float boostStartSpeed = 5f;

  private bool isBoosting = false;
  private float boostTimer;

  ParticleSystem.MainModule main;

  void Start()
  {
    ApplyNormalState();
  }

  void Update()
  {
    if (isBoosting)
    {
      boostTimer -= Time.deltaTime;
      if (boostTimer <= 0f)
      {
        EndBoost();
      }
    }
  }

  public void StartBoost(float duration)
  {
    boostTimer = duration;
    isBoosting = true;

    ApplyBoostState();
  }

  private void EndBoost()
  {
    isBoosting = false;
    ApplyNormalState();
  }

  private void ApplyNormalState()
  {
    if (boostTrail != null)
    {
      var main = boostTrail.main;
      main.startColor = normalColor;
      main.startSize = normalStartSize;
      main.startSpeed = normalStartSpeed;
    }
  }

  private void ApplyBoostState()
  {
    if (boostTrail != null)
    {
      var main = boostTrail.main;
      main.startColor = boostColor;
      main.startSize = boostStartSize;
      main.startSpeed = boostStartSpeed;
    }
  }
}
