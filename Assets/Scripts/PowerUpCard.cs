using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName = "Powerup Card")]
public class PowerupCard : ScriptableObject
{
    public string cardId;         // Must be unique (e.g., "speed_boost")
    public string title;
    public string description;
    public Sprite frontImage;
    public Rarity rarity;
    public string effectId;       // Used to apply logic (e.g., "speed_boost")
}

public enum Rarity
{
    Common, Uncommon, Rare, Epic, Legendary
}

