using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CardSlot : NetworkBehaviour
{
    public Image frontImage;
    public Image backImage;
    public GameObject shineEffect;
    public Button button;

    private PowerupCard cardData;
    private PlayerController assignedPlayer;
    private bool revealed = false;

    public void Setup(PowerupCard card, PlayerController chooser)
    {
        if (card == null)
        {
            Debug.LogError("CardSlot.Setup was called with a null card!");
            return;
        }

        cardData = card;
        assignedPlayer = chooser;
        frontImage.sprite = card.frontImage;
        frontImage.gameObject.SetActive(false);
        backImage.gameObject.SetActive(true);
        shineEffect.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnClick());
    }



    public void OnHoverEnter()
    {
        if (!revealed)
        {
            Reveal();
            CmdBroadcastHover(cardData.title);
        }

        transform.localScale = Vector3.one * 1.1f;
        shineEffect.SetActive(true);
    }

    public void OnHoverExit()
    {
        transform.localScale = Vector3.one;
        shineEffect.SetActive(false);
    }

    private void Reveal()
    {
        revealed = true;
        frontImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (assignedPlayer.isLocalPlayer)
        {
            // Check if player has full wins
            if (RoundManager.Instance.HasFullWins(assignedPlayer.netId))
            {
                Debug.Log($"Player {assignedPlayer.netId} has full wins and cannot select a card");
                return;
            }
            Debug.Log($"CardSlot: Player {assignedPlayer.netId} selected card {cardData.title} with effectId {cardData.effectId}");
            CmdChooseCard(assignedPlayer.netId, cardData.effectId);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdChooseCard(uint playerId, string effectId)
    {
        Debug.Log($"CardSlot: CmdChooseCard called for player {playerId} with effect {effectId}");
        if (GameManager.Instance != null)
        {
            Debug.Log($"CardSlot: Notifying GameManager of card selection for player {playerId}");
            GameManager.Instance.CardChosen(playerId, effectId);
        }
        else
        {
            Debug.LogError("CardSlot: GameManager.Instance is null!");
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdBroadcastHover(string title)
    {
        RpcHoverVisual(title);
    }

    [ClientRpc]
    private void RpcHoverVisual(string title)
    {
        // Optional: Add a global hover effect if needed
    }
}
