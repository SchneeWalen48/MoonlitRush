using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button OkButton;
    [SerializeField] GameObject panelRoot;

  public AudioSource source;
  public AudioClip buttonSound;

  const int MaxLen = 6;
    void OnEnable()
    {
        // 패널이 열릴 때 입력창 자동 포커스 + 커서 깜빡임 활성화
        if (inputField != null)
        {
            inputField.characterLimit = MaxLen;
            inputField.caretBlinkRate = 0.85f;
            inputField.ActivateInputField();

            inputField.onValidateInput += ValidateLetterOnly;

            inputField.onValueChanged.AddListener(SanitizeAndUpdate);
            SanitizeAndUpdate(inputField.text);

        }
    if (OkButton != null)
    {
      OkButton.onClick.RemoveAllListeners();
      OkButton.onClick.AddListener(OnSubmitNickname);
    }
  }
    void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValidateInput -= ValidateLetterOnly;
            inputField.onValueChanged.RemoveListener(SanitizeAndUpdate);
        }
    }
    private char ValidateLetterOnly(string text, int charIndex, char addedChar)
    {
        return char.IsLetter(addedChar) ? addedChar : '\0';
    }

    private void SanitizeAndUpdate(string current)
    {
        if (inputField == null) return;

        string filtered = string.IsNullOrEmpty(current)
            ? ""
            : new string(current.Where(char.IsLetter).ToArray());

        if (filtered.Length > MaxLen)
            filtered = filtered.Substring(0, MaxLen);

        if (filtered != current)
        {
            // 변경 알림은 내보내지 않고 텍스트만 교체(루프 방지)
            inputField.SetTextWithoutNotify(filtered);
            inputField.caretPosition = filtered.Length;
        }

        if (OkButton != null)
            OkButton.interactable = filtered.Length > 0;


        // 버튼 클릭 이벤트 초기화 후 다시 연결
        if (OkButton != null)
        {
            OkButton.onClick.RemoveAllListeners();
            OkButton.onClick.AddListener(OnSubmitNickname);
        }

    }



    public void OnSubmitNickname()
    {
        string nick = inputField ? inputField.text.Trim() : "";

        if (string.IsNullOrEmpty(nick))
        {
            Debug.LogWarning("닉네임이 비어 있습니다.");
            return;
        }

        // 닉네임 저장
        PlayerPrefs.SetString("PlayerNickname", nick);
        PlayerPrefs.Save();

    // 패널 닫기
    if (panelRoot) { panelRoot.SetActive(false); source.PlayOneShot(buttonSound); }

      Debug.Log($"[NicknameInput] 닉네임 저장 완료: {nick}");
    }
}
