using System.Collections.Generic;
using UnityEngine;

namespace Coup.Game
{
    [System.Serializable]
    public class Player
    {
        public int playerId;
        public string playerName;
        public int coins;
        public List<Card> influences; // 플레이어가 보유한 카드들
        public bool isAlive;
        public bool isReady;

        public Player(int id, string name)
        {
            playerId = id;
            playerName = name;
            coins = 2; // 게임 시작시 코인 2개
            influences = new List<Card>();
            isAlive = true;
            isReady = false;
        }

        public void AddInfluence(Card card)
        {
            if (influences.Count < 2)
            {
                influences.Add(card);
            }
        }

        public bool RemoveInfluence(CardType cardType)
        {
            for (int i = 0; i < influences.Count; i++)
            {
                if (influences[i].cardType == cardType)
                {
                    influences.RemoveAt(i);
                    
                    if (influences.Count == 0)
                    {
                        isAlive = false;
                    }
                    
                    return true;
                }
            }
            return false;
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
            coins = Mathf.Max(0, coins - amount);
        }

        public void GainCoins(int amount)
        {
            coins += amount;
        }

        public bool CanBlock(ActionType actionType)
        {
            foreach (var card in influences)
            {
                if (card.CanBlock(actionType))
                    return true;
            }
            return false;
        }

        public bool CanPerformAction(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Income:
                case ActionType.ForeignAid:
                    return true; // 모든 플레이어가 할 수 있는 기본 액션

                case ActionType.Coup:
                    return coins >= 7; // 쿠데타는 7코인 필요

                case ActionType.Assassinate:
                    return coins >= 3 && HasInfluence(CardType.Assassin);

                case ActionType.Tax:
                    return HasInfluence(CardType.Duke);

                case ActionType.Steal:
                    return HasInfluence(CardType.Captain);

                case ActionType.Exchange:
                    return HasInfluence(CardType.Ambassador);

                default:
                    return false;
            }
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

        // 블러핑용: 실제로 카드를 가지고 있지 않아도 액션을 시도할 수 있음
        public bool CanClaimAction(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Tax:
                case ActionType.Assassinate:
                case ActionType.Steal:
                case ActionType.Exchange:
                    return true; // 블러핑 가능

                case ActionType.Income:
                case ActionType.ForeignAid:
                case ActionType.Coup:
                    return CanPerformAction(actionType); // 블러핑 불가능한 기본 액션

                default:
                    return false;
            }
        }
    }
}