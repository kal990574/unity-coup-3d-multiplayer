using System.Collections.Generic;
using UnityEngine;

namespace Coup.Game
{
    public enum CardType
    {
        Duke,
        Assassin,
        Captain,
        Ambassador,
        Contessa
    }

    public enum ActionType
    {
        Income,
        ForeignAid,
        Coup,
        Tax,           // Duke
        Assassinate,   // Assassin
        Steal,         // Captain
        Exchange       // Ambassador
    }

    [System.Serializable]
    public class Card
    {
        public CardType cardType;
        public string cardName;
        public string description;
        public ActionType primaryAction;
        public List<ActionType> blockableActions;

        public Card(CardType type)
        {
            cardType = type;
            SetupCardData();
        }

        private void SetupCardData()
        {
            blockableActions = new List<ActionType>();
            
            switch (cardType)
            {
                case CardType.Duke:
                    cardName = "Duke";
                    description = "Take 3 coins (Tax). Blocks Foreign Aid.";
                    primaryAction = ActionType.Tax;
                    blockableActions.Add(ActionType.ForeignAid);
                    break;

                case CardType.Assassin:
                    cardName = "Assassin";
                    description = "Pay 3 coins to force opponent to lose influence (Assassinate).";
                    primaryAction = ActionType.Assassinate;
                    break;

                case CardType.Captain:
                    cardName = "Captain";
                    description = "Take 2 coins from another player (Steal). Blocks stealing.";
                    primaryAction = ActionType.Steal;
                    blockableActions.Add(ActionType.Steal);
                    break;

                case CardType.Ambassador:
                    cardName = "Ambassador";
                    description = "Exchange cards with Court deck. Blocks stealing.";
                    primaryAction = ActionType.Exchange;
                    blockableActions.Add(ActionType.Steal);
                    break;

                case CardType.Contessa:
                    cardName = "Contessa";
                    description = "Blocks assassination.";
                    primaryAction = ActionType.Income; // Contessa has no unique action
                    blockableActions.Add(ActionType.Assassinate);
                    break;
            }
        }

        public bool CanBlock(ActionType action)
        {
            return blockableActions.Contains(action);
        }

        public static List<Card> CreateDeck()
        {
            var deck = new List<Card>();
            
            // 각 카드 타입당 3장씩
            for (int i = 0; i < 3; i++)
            {
                deck.Add(new Card(CardType.Duke));
                deck.Add(new Card(CardType.Assassin));
                deck.Add(new Card(CardType.Captain));
                deck.Add(new Card(CardType.Ambassador));
                deck.Add(new Card(CardType.Contessa));
            }
            
            return deck;
        }
    }
}