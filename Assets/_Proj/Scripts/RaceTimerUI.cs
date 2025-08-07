using UnityEngine;
using TMPro; // Use Text Mesh Pro(Not Legacy Text)

public class RaceTimerUI : MonoBehaviour
{
  public TMP_Text tmpText;

  void Update()
  {
    if (TimeManager.Instance == null) return;

    string timeStr = TimeManager.Instance.GetFormatRaceTime();

    if (tmpText != null)
      tmpText.text = timeStr;
  }
}
