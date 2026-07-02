using UnityEngine;
using UnityEngine.EventSystems;

public class EndingCreditsClickCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EndingCreditsController controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null)
            return;

        controller.ShowSkipButtonFromScreenClick();
    }
}
