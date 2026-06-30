using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonClickSfxBinding : MonoBehaviour
{
    private Button button;
    private bool isBound;

    public void Bind(Button targetButton)
    {
        if (isBound)
            return;

        button = targetButton != null ? targetButton : GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.AddListener(PlayButtonClick);
        isBound = true;
    }

    private void OnDestroy()
    {
        if (button != null && isBound)
            button.onClick.RemoveListener(PlayButtonClick);
    }

    private void PlayButtonClick()
    {
        if (button != null && !button.interactable)
            return;

        if (UISfxController.Instance == null)
            return;

        UISfxController.Instance.PlayButtonClick();
    }
}
