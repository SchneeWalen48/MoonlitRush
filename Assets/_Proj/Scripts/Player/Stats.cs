using System;
using UnityEngine;

[Serializable]
public class Stats
{
  [Header("Movement Settings")]
  public float currSpeed;
  public float speed = 20f;
  public float acceleration = 10f;
  public float steer = 20f;
  public float brakeForce = 30f;
  [Min(0.001f)]
  public float limitSpeed;
  [Min(0.001f)]
  public float reverseSpeed;
  public float reverseAccel;
  [Tooltip("The speed at which the car starts accelerating from 0.")]
  [Range(0.2f, 1)]
  public float AccelerationCurve;
  public float braking;
  [Tooltip("The time it takes for the car to come to a complete stop when there is no input.")]
  public float dragTime;
  [Range(0.0f, 1.0f)]
  [Tooltip("The amount of side-to-side friction.")]
  public float grip;
  [Tooltip("Additional gravity for when the car is in the air.")]
  public float addedGravity;


  public Stats() { }
  public Stats(Stats original)
  {
    this.limitSpeed = original.limitSpeed;
    this.acceleration = original.acceleration;
    this.steer = original.steer;
    this.brakeForce = original.brakeForce;
    this.speed = original.speed;
    this.reverseSpeed = original.reverseSpeed;
    this.reverseAccel = original.reverseAccel;
    this.AccelerationCurve = original.AccelerationCurve;
    this.braking = original.braking;
    this.dragTime = original.dragTime;
    this.grip = original.grip;
    this.addedGravity = original.addedGravity;
  }
  public static Stats operator +(Stats a, Stats b)
  {
    return new Stats
    {
      acceleration = a.acceleration + b.acceleration,
      limitSpeed = a.limitSpeed + b.limitSpeed,
      reverseSpeed = a.reverseSpeed + b.reverseSpeed,
      reverseAccel = a.reverseAccel + b.reverseAccel,
      braking = a.braking + b.braking,
      steer = a.steer + b.steer,
      grip = a.grip + b.grip,
      addedGravity = a.addedGravity + b.addedGravity
    };
  }
}
