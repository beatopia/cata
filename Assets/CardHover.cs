using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject cardInfoPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image cardImage;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private float scaleSpeed = 10f; // How fast it grows/shrinks

    private CardPicker cardPicker;  // Reference to the CardPicker script
    private PowerupCard card;      // Reference to the PowerupCard

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Find the CardPicker component in the scene
        cardPicker = FindFirstObjectByType<CardPicker>();

        // Find the CardDisplay component on this card object
        card = GetComponent<CardDisplay>().card;  // Get the card from CardDisplay component

        // Hide the panel initially
        if (cardInfoPanel != null)
        {
            cardInfoPanel.SetActive(false);
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    // When the mouse enters the card
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * 1.1f; // Grow to 110%
    }

    // When the mouse exits the card
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale; // Shrink back to normal
    }

    // When the card is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ensure we have a valid card and cardPicker
        if (card != null && cardPicker != null)
        {
            // Check if the local player has full wins
            PlayerController player = NetworkClient.localPlayer?.GetComponent<PlayerController>();
            if (player != null && player.isLocalPlayer)
            {
                bool hasFullWins = false;
                if (NetworkServer.active)
                {
                    hasFullWins = RoundManager.Instance.HasFullWins(player.netId);
                }
                else
                {
                    // On client, we can't check HasFullWins directly
                    // Instead, we'll use the hasPicked state as an indicator
                    hasFullWins = player.hasPicked;
                }

                if (hasFullWins)
                {
                    Debug.Log($"Player {player.netId} has full wins and cannot select a card");
                    return;
                }
            }
            Debug.Log($"CardHover: Clicking card {card.title} with effectId {card.effectId}");
            // Use CardPicker's OnCardClicked method which handles the networking properly
            cardPicker.OnCardClicked(card);
        }
    }

    public void OnCardHoverEnter(PowerupCard card)
    {
        if (card == null) return;

        // Get the local player
        PlayerController localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();
        if (localPlayer != null)
        {
            // Check if player has full wins
            bool hasFullWins = false;
            if (NetworkServer.active)
            {
                hasFullWins = RoundManager.Instance.HasFullWins(localPlayer.netId);
            }
            else
            {
                // On client, we can't check HasFullWins directly
                // Instead, we'll use the hasPicked state as an indicator
                hasFullWins = localPlayer.hasPicked;
            }

            if (hasFullWins)
            {
                // Don't show card info for players with full wins
                return;
            }
        }

        // Show the panel
        if (cardInfoPanel != null)
        {
            cardInfoPanel.SetActive(true);
        }

        // Update the UI elements
        if (titleText != null)
        {
            titleText.text = card.title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = card.description;
        }

        if (cardImage != null && card.frontImage != null)
        {
            cardImage.sprite = card.frontImage;
        }
    }

    public void OnCardHoverExit()
    {
        // Hide the panel
        if (cardInfoPanel != null)
        {
            cardInfoPanel.SetActive(false);
        }
    }
}
