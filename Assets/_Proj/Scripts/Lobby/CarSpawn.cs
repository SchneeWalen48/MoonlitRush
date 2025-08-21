using UnityEngine;

public class CarSpawn : MonoBehaviour
{
  public GameObject[] playerCar;
  public Transform spawnPoint; // 차량 시작 위치

  public GameObject lastSpawned { get; private set; }

  void Start()
  {
    int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);

    if (selectedCarIndex >= 0 && selectedCarIndex < playerCar.Length)
    {
      GameObject go = Instantiate(playerCar[selectedCarIndex], spawnPoint.position, spawnPoint.rotation);

      go.SetActive(true);
      Rigidbody rb = go.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.WakeUp();
      }

      // Intro Countdown 스크립트 연결 시 삭제. 해당 스크립트에서 Time.timeScale = 1f; 줘야 함.
      //Time.timeScale = 1f;

      go.transform.parent = null;
      lastSpawned = go;
    }
    else
    {
      print("선택된 차량 idx가 잘못 됨");
    }
  }
}
