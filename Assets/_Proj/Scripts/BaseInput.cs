using UnityEngine;

// 다른 컨트롤러 사용의 확장성을 고려하여 상속 형식으로 분리
public struct InputData
{
  public bool Accelerate;
  public bool Brake;
  public float TurnInput;
  public bool Reverse;
}

public interface IInput
{
  InputData GenerateInput();
}

public abstract class BaseInput : MonoBehaviour, IInput
{
  public abstract InputData GenerateInput();
}