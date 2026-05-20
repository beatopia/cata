using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public List<PowerupCard> cardDatabase = new();
    public GameObject cardSlotPrefab;
    public Transform cardSpawnParent;
    public TextMeshProUGUI chosenCardNameText;
    public Canvas cardPickerCanvas;
    public GameObject blackoutOverlay;
    public GameObject waitingMessage;
    public TextMeshProUGUI waitingText;

    private Queue<PlayerController> playersChoosing = new();
    private List<CardSlot> activeSlots = new();
    private bool isCardPickingInProgress = false;

    void Awake()
    {
        Debug.Log("GameManager: Awake called");
        if (Instance != null && Instance != this)
        {
            Debug.Log("GameManager: Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"GameManager: Instance set, isServer = {isServer}");
    }

    private void Start()
    {
        // Initially hide the canvas
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(false);
        }
    }

    [Server]
    public void StartInitialCardPick(List<PlayerController> allPlayers)
    {
        if (isCardPickingInProgress) return;
        isCardPickingInProgress = true;
        
        // Disable damage during card picking
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.damageEnabled = false;
        }

        playersChoosing = new Queue<PlayerController>(allPlayers);
        RpcShowCardPicker();
        BeginNextChooser();
    }

    [Server]
    public void StartLoserCardPick(List<PlayerController> losers)
    {
        if (isCardPickingInProgress) return;
        isCardPickingInProgress = true;
        
        // Disable damage during card picking
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.damageEnabled = false;
        }

        // Reset full win restrictions since a new player got a full win
        RoundManager.Instance.ResetFullWinRestrictions();

        playersChoosing = new Queue<PlayerController>(losers);
        RpcShowCardPicker();
        BeginNextChooser();
    }

    [ClientRpc]
    private void RpcShowCardPicker()
    {
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(true);
        }
        if (blackoutOverlay != null)
        {
            blackoutOverlay.SetActive(true);
        }
        if (waitingMessage != null)
        {
            waitingMessage.SetActive(true);
        }
        if (waitingText != null)
        {
            waitingText.text = "Waiting for players...";
        }
        Time.timeScale = 0f;
    }

    [Server]
    private void BeginNextChooser()
    {
        if (playersChoosing.Count == 0)
        {
            RpcClearCards();
            RpcHideCardPicker();
            isCardPickingInProgress = false;
            
            // Re-enable damage after card picking
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.damageEnabled = true;
                GameStateManager.Instance.CompleteCardPicking();
            }
            return;
        }

        var next = playersChoosing.Dequeue();
        
        // Check if the player has achieved a full win using the public method
        if (RoundManager.Instance.HasFullWins(next.netId))
        {
            // Skip this player and move to the next one
            BeginNextChooser();
            return;
        }
        
        var cards = Generate3Cards();
        var cardIds = cards.Select(c => c.cardId).ToArray();
        RpcDisplayCardsToAll(cardIds, next.netId);
    }

    [ClientRpc]
    private void RpcHideCardPicker()
    {
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(false);
        }
        if (blackoutOverlay != null)
        {
            blackoutOverlay.SetActive(false);
        }
        if (waitingMessage != null)
        {
            waitingMessage.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    [ClientRpc]
    private void RpcDisplayCardsToAll(string[] cardIds, uint chooserId)
    {
        ClearCardUI();

        PlayerController chooser = FindPlayer(chooserId);
        if (chooser == null)
        {
            Debug.LogError($"Could not find player with ID {chooserId}");
            return;
        }

        foreach (var id in cardIds)
        {
            PowerupCard card = cardDatabase.Find(c => c.cardId == id);
            if (card == null)
            {
                Debug.LogError($"Card with ID {id} not found in GameManager.cardDatabase on client.");
                continue;
            }

            var obj = Instantiate(cardSlotPrefab, cardSpawnParent);
            var slot = obj.GetComponent<CardSlot>();
            slot.Setup(card, chooser);
            activeSlots.Add(slot);
        }

        // Update waiting text
        if (waitingText != null)
        {
            waitingText.text = chooser.isLocalPlayer ? "Choose your card!" : $"Waiting for {chooser.netId} to choose...";
        }
    }

    [ClientRpc]
    private void RpcClearCards()
    {
        ClearCardUI();
    }

    private void ClearCardUI()
    {
        foreach (var slot in activeSlots)
            if (slot != null) Destroy(slot.gameObject);

        activeSlots.Clear();
    }

    [Server]
    public void CardChosen(uint playerId, string effectId)
    {
        Debug.Log($"GameManager: CardChosen called for player {playerId} with effect {effectId}");
        Debug.Log($"GameManager: isServer = {isServer}, isClient = {isClient}, isLocalPlayer = {isLocalPlayer}");
        Debug.Log($"GameManager: connectionToServer = {connectionToServer != null}, connectionToClient = {connectionToClient != null}");
        
        if (!isServer)
        {
            Debug.LogError("GameManager: CardChosen called on client!");
            return;
        }

        var player = FindPlayer(playerId);
        if (player == null)
        {
            Debug.LogError($"GameManager: Could not find player with ID {playerId}");
            return;
        }
        
        Debug.Log($"GameManager: Found player {player.netId}, isServer = {player.isServer}, isLocalPlayer = {player.isLocalPlayer}");
        Debug.Log($"GameManager: Applying effect {effectId} to player {player.netId}");
        ApplyEffect(effectId, player);
        BeginNextChooser();
    }

    [Server]
    void ApplyEffect(string effectId, PlayerController player)
    {
        if (!isServer)
        {
            Debug.LogError("GameManager: ApplyEffect called on client!");
            return;
        }

        Debug.Log($"GameManager: Applying effect {effectId} to player {player.netId}");
        switch (effectId)
        {
            case "speed_boost":
                Debug.Log($"GameManager: Applying speed boost to player {player.netId}");
                player.moveSpeed += 2f;
                break;
            case "knockback_up":
                Debug.Log($"GameManager: Applying knockback up to player {player.netId}");
                player.knockbackForce += 2f;
                break;
            case "double_bullet":
                Debug.Log($"GameManager: Applying double bullet to player {player.netId}");
                player.SetMaxBullets(2);
                break;
            case "backed_up":
                Debug.Log($"GameManager: Applying backed up to player {player.netId}");
                player.SetMaxBullets(3);
                break;
            case "dash_reset":
                Debug.Log($"GameManager: Applying dash reset to player {player.netId}");
                player.ResetDashCooldown();
                break;
            case "pounce":
                Debug.Log($"GameManager: Enabling pounce for player {player.netId}");
                player.EnablePounce();
                Debug.Log($"GameManager: After EnablePounce, hasPounce = {player.HasPounce}");
                break;
            case "naptime":
                Debug.Log($"GameManager: Applying naptime effect to player {player.netId}");
                if (player.gameObject.GetComponent<NaptimeEffect>() != null)
                {
                    Debug.Log($"GameManager: Player {player.netId} already has NaptimeEffect, removing old one");
                    Destroy(player.gameObject.GetComponent<NaptimeEffect>());
                }
                var naptimeEffect = player.gameObject.AddComponent<NaptimeEffect>();
                if (naptimeEffect != null)
                {
                    naptimeEffect.Initialize(player);
                    Debug.Log($"GameManager: Successfully added NaptimeEffect to player {player.netId}");
                }
                else
                {
                    Debug.LogError($"GameManager: Failed to add NaptimeEffect to player {player.netId}");
                }
                break;
            case "turtle_cat":
                Debug.Log($"GameManager: Applying turtle cat effect to player {player.netId}");
                // Log initial values
                Debug.Log($"GameManager: Player {player.netId} initial moveSpeed: {player.moveSpeed}");
                Debug.Log($"GameManager: Player {player.netId} initial health: {player.CurrentHealth}");
                
                // Apply the effect directly to the player
                player.moveSpeed *= 0.7f; // 30% slower
                player.Heal(50); // 50 more health
                
                // Log final values
                Debug.Log($"GameManager: Player {player.netId} final moveSpeed: {player.moveSpeed}");
                Debug.Log($"GameManager: Player {player.netId} final health: {player.CurrentHealth}");
                break;
            case "shadow_ally":
                // TODO: Implement shadow ally
                break;
            case "rabbitcat":
                Debug.Log($"GameManager: Applying Rabbit-Cat speed boost to player {player.netId}");
                // Increase move speed by 25% of the base speed
                player.moveSpeed += player.baseMoveSpeed * 0.25f; 
                break;
            case "scorpioncat":
                Debug.Log($"GameManager: Applying Scorpion-Cat damage boost to player {player.netId}");
                // Add a damageBonus field if not present, or increment it if it is
                if (!player.TryGetComponent(out DamageBonusHolder bonusHolder))
                {
                    bonusHolder = player.gameObject.AddComponent<DamageBonusHolder>();
                }
                bonusHolder.damageBonus += 0.25f;
                break;
            case "Milk":
                Debug.Log($"GameManager: Applying Milk Drunk effect to player {player.netId}");
                if (player.gameObject.GetComponent<MilkDrunkEffect>() == null)
                {
                    var milkEffect = player.gameObject.AddComponent<MilkDrunkEffect>();
                    milkEffect.Initialize(player);
                }
                break;
            case "Catnip":
                Debug.Log($"GameManager: Applying Catnip effect to player {player.netId}");
                // +25 HP
                player.Heal(25);
                // +25 DMG
                if (!player.TryGetComponent(out DamageBonusHolder catnipBonusHolder))
                {
                    catnipBonusHolder = player.gameObject.AddComponent<DamageBonusHolder>();
                }
                catnipBonusHolder.damageBonus += 0.25f;
                // +25 SPEED
                player.moveSpeed += 25f;
                // +10% Bullet Spread
                if (player.GetType().GetField("bulletSpread") != null)
                {
                    player.bulletSpread += 0.10f;
                }
                break;
            case "Scratch":
                Debug.Log($"GameManager: Applying Scratch effect to player {player.netId}");
                if (!player.TryGetComponent(out ScratchEffectHolder scratch))
                    player.gameObject.AddComponent<ScratchEffectHolder>();
                break;
            default:
                Debug.LogWarning($"GameManager: Unknown effect ID {effectId}");
                break;
        }
    }

    [ClientRpc]
    private void RpcDisplayChosenCard(string cardTitle)
    {
        if (chosenCardNameText != null)
        {
            chosenCardNameText.text = cardTitle;
        }
    }

    private List<PowerupCard> Generate3Cards()
    {
        List<PowerupCard> result = new();
        HashSet<string> usedCardIds = new HashSet<string>();
        
        while (result.Count < 3)
        {
            PowerupCard card = PullCardByRarity();
            if (!usedCardIds.Contains(card.cardId))
            {
                usedCardIds.Add(card.cardId);
                result.Add(card);
            }
        }

        return result;
    }

    private PowerupCard PullCardByRarity()
    {
        float roll = Random.Range(0f, 100f);
        Rarity rarity = Rarity.Common;

        if (roll < 2f) rarity = Rarity.Legendary;
        else if (roll < 10f) rarity = Rarity.Epic;
        else if (roll < 25f) rarity = Rarity.Rare;
        else if (roll < 50f) rarity = Rarity.Uncommon;

        return cardDatabase.Where(c => c.rarity == rarity).OrderBy(x => Random.value).FirstOrDefault();
    }

    private PlayerController FindPlayer(uint netId)
    {
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.netId == netId)
            {
                return player;
            }
        }
        return null;
    }
} 