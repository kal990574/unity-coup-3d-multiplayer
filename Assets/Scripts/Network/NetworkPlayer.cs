using System.Collections.Generic;
using Unity.Netcode;
using Coup.Game;

namespace Coup.Network
{
    [System.Serializable]
    public class NetworkPlayer
    {
        public ulong clientId;
        public string playerName;
        public int coins;
        public List<Card> influences;
        public bool isAlive;
        public bool isReady;
        
        public NetworkPlayer()
        {
            influences = new List<Card>();
            coins = GameRules.STARTING_COINS;
            isAlive = true;
            isReady = false;
        }
        
        public NetworkPlayer(ulong id, string name)
        {
            clientId = id;
            playerName = name;
            influences = new List<Card>();
            coins = GameRules.STARTING_COINS;
            isAlive = true;
            isReady = false;
        }
        
        public bool HasInfluence(CardType cardType)
        {
            foreach (var card in influences)
            {
                if (card.cardType == cardType)
                    return true;
            }
            return false;
        }
        
        public bool CanAfford(int cost)
        {
            return coins >= cost;
        }
        
        public void SpendCoins(int amount)
        {
            coins = UnityEngine.Mathf.Max(0, coins - amount);
        }
        
        public void GainCoins(int amount)
        {
            coins += amount;
        }
        
        public int GetInfluenceCount()
        {
            return influences.Count;
        }
        
        public List<CardType> GetInfluenceTypes()
        {
            var types = new List<CardType>();
            foreach (var card in influences)
            {
                types.Add(card.cardType);
            }
            return types;
        }
    }
}