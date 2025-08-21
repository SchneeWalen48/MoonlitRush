using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private BoxCollider collider;
  private FinalCount final; // 완주 시 게임 종료 알리는 카운트 스크립트
  private bool isFinished = false;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
    final = FindAnyObjectByType<FinalCount>();
        if(collider != null)
        {
            collider.enabled = false;
        }        
    }

   public void ActiveTrigger()
    {
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("EndTrigger 활성화");
        }
    }
  void OnTriggerEnter(Collider other)
  {
    if (!isFinished) return;
    else if (!isFinished)
    {
      isFinished = true;
      Rigidbody rb = other.GetComponent<Rigidbody>();
      rb.drag = 20;
      rb.angularDrag = 20;
      rb.isKinematic = true;

      if (other.CompareTag("Player"))
      {
        CarController p = other.GetComponent<CarController>();
        p.moveInput = 0f;
        p.steerInput = 0f;
      }
      else if (other.CompareTag("AIPlayer"))
      {
        AICarController ai = other.GetComponent<AICarController>();
        ai.moveInput = 0f;
        ai.steerInput = 0f;
      }
    }
    final.Finish();
  }

}
