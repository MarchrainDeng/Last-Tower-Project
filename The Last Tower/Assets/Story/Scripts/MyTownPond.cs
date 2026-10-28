using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// マイタウン手前の池
/// クリックされたらコントローラーへ中継し、プレイヤーが岸まで歩いて釣りを始める
/// </summary>
public class MyTownPond : MonoBehaviour, IPointerClickHandler
{
    public MyTownController controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && controller != null)
            controller.ClickPond(eventData.position);
    }
}
