using TMPro; // если используете TextMeshPro
using UnityEngine;
using UnityEngine.UI; // для UI-изображений, либо TMPro для текста

/// <summary>
/// Отображает карту в сцене. Принимает данные через метод SetCard.
/// Может быть расширен анимацией переворота, свечением и т.д.
/// </summary>

public class CardView : MonoBehaviour
{

    [Header("UI элементы (назначаются в префабе)")]
    [SerializeField] private Image suitIcon;       // иконка масти
    [SerializeField] private TextMeshProUGUI rankText; // текст ранга (6, Q, A)
    [SerializeField] private Image background;     // фон (стиль зависит от CardType)
    [SerializeField] private GameObject backSide;  // рубашка карты

    [Header("Отладка (только чтение)")]
    [SerializeField] private string debugCardRank;
    [SerializeField] private string debugCardSuit;
    [SerializeField] private string debugCardType;
    [SerializeField] private int debugBlackGregValue;

    public ICard cardData { get; set; }

    /// <summary>
    /// Присваивает карте данные
    /// </summary>
    /// 

    public void SetCard(ICard card)
    {
        cardData = card;
        UpdateDebugInfo();
    }

    private void UpdateDebugInfo()
    {
        if (cardData != null)
        {
            debugCardRank = cardData.CardRank.ToString();
            debugCardSuit = cardData.CardSuit.ToString();
            debugCardType = cardData.CardType.ToString();
            debugBlackGregValue = cardData.BlackGregValue;
        }
        else
        {
            debugCardRank = debugCardSuit = debugCardType = "";
            debugBlackGregValue = 0;
        }
    }

    private string RankToString(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => "A",
            CardRank.King => "K",
            CardRank.Queen => "Q",
            CardRank.Jack => "J",
            _ => ((int)rank).ToString()
        };
    }

}

