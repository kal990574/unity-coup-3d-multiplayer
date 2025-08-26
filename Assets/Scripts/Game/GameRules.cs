using System.Collections.Generic;
using UnityEngine;

namespace Coup.Game
{
    public enum GameState
    {
        WaitingForPlayers,
        Starting,
        Playing,
        WaitingForResponse, // 챌린지나 블록 대기
        GameOver
    }

    public enum ResponseType
    {
        None,
        Challenge,
        Block,
        Allow
    }

    public struct GameAction
    {
        public int playerId;
        public ActionType actionType;
        public int targetPlayerId;
        public CardType claimedCard;
        
        public GameAction(int player, ActionType action, int target = -1, CardType card = CardType.Duke)
        {
            playerId = player;
            actionType = action;
            targetPlayerId = target;
            claimedCard = card;
        }
    }

    public struct PendingResponse
    {
        public GameAction originalAction;
        public List<int> respondingPlayers;
        public float timeRemaining;
        
        public PendingResponse(GameAction action, List<int> players, float time = 15f)
        {
            originalAction = action;
            respondingPlayers = new List<int>(players);
            timeRemaining = time;
        }
    }

    public static class GameRules
    {
        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 6;
        public const int STARTING_COINS = 2;
        public const int STARTING_INFLUENCES = 2;
        public const int COUP_COST = 7;
        public const int ASSASSINATE_COST = 3;
        public const int FORCED_COUP_THRESHOLD = 10; // 10코인이면 반드시 쿠데타 해야함
        
        public static bool IsValidPlayerCount(int count)
        {
            return count >= MIN_PLAYERS && count <= MAX_PLAYERS;
        }

        public static bool CanPerformAction(Player player, GameAction action, List<Player> allPlayers)
        {
            if (!player.isAlive) return false;

            switch (action.actionType)
            {
                case ActionType.Income:
                    return true;

                case ActionType.ForeignAid:
                    return true;

                case ActionType.Coup:
                    return player.CanAfford(COUP_COST) && IsValidTarget(action.targetPlayerId, allPlayers);

                case ActionType.Tax:
                    return true; // 블러핑 가능하므로 항상 시도 가능

                case ActionType.Assassinate:
                    return player.CanAfford(ASSASSINATE_COST) && IsValidTarget(action.targetPlayerId, allPlayers);

                case ActionType.Steal:
                    return IsValidTarget(action.targetPlayerId, allPlayers);

                case ActionType.Exchange:
                    return true; // 블러핑 가능하므로 항상 시도 가능

                default:
                    return false;
            }
        }

        public static bool IsValidTarget(int targetId, List<Player> allPlayers)
        {
            if (targetId < 0 || targetId >= allPlayers.Count) return false;
            return allPlayers[targetId].isAlive;
        }

        public static bool RequiresTarget(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Coup:
                case ActionType.Assassinate:
                case ActionType.Steal:
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanBeBlocked(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.ForeignAid:
                case ActionType.Steal:
                case ActionType.Assassinate:
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanBeChallenged(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Tax:
                case ActionType.Assassinate:
                case ActionType.Steal:
                case ActionType.Exchange:
                    return true; // 특정 카드를 요구하는 액션들
                default:
                    return false;
            }
        }

        public static CardType GetRequiredCard(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Tax:
                    return CardType.Duke;
                case ActionType.Assassinate:
                    return CardType.Assassin;
                case ActionType.Steal:
                    return CardType.Captain;
                case ActionType.Exchange:
                    return CardType.Ambassador;
                default:
                    return CardType.Duke; // 기본값
            }
        }

        public static List<int> GetPotentialBlockers(GameAction action, List<Player> allPlayers)
        {
            var blockers = new List<int>();

            switch (action.actionType)
            {
                case ActionType.ForeignAid:
                    // 모든 살아있는 플레이어가 Duke로 블록 가능
                    for (int i = 0; i < allPlayers.Count; i++)
                    {
                        if (allPlayers[i].isAlive && i != action.playerId)
                            blockers.Add(i);
                    }
                    break;

                case ActionType.Steal:
                    // 타겟만 Captain이나 Ambassador로 블록 가능
                    if (action.targetPlayerId >= 0 && allPlayers[action.targetPlayerId].isAlive)
                        blockers.Add(action.targetPlayerId);
                    break;

                case ActionType.Assassinate:
                    // 타겟만 Contessa로 블록 가능
                    if (action.targetPlayerId >= 0 && allPlayers[action.targetPlayerId].isAlive)
                        blockers.Add(action.targetPlayerId);
                    break;
            }

            return blockers;
        }

        public static List<int> GetPotentialChallengers(GameAction action, List<Player> allPlayers)
        {
            var challengers = new List<int>();

            if (!CanBeChallenged(action.actionType)) return challengers;

            // 액션을 수행하는 플레이어를 제외한 모든 살아있는 플레이어
            for (int i = 0; i < allPlayers.Count; i++)
            {
                if (allPlayers[i].isAlive && i != action.playerId)
                    challengers.Add(i);
            }

            return challengers;
        }

        public static bool IsForcedCoup(Player player)
        {
            return player.coins >= FORCED_COUP_THRESHOLD;
        }

        public static bool IsGameOver(List<Player> players)
        {
            int alivePlayers = 0;
            foreach (var player in players)
            {
                if (player.isAlive) alivePlayers++;
            }
            return alivePlayers <= 1;
        }

        public static int GetWinnerId(List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].isAlive)
                    return i;
            }
            return -1; // 승자 없음 (에러 상황)
        }

        public static int CalculateActionCost(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Coup:
                    return COUP_COST;
                case ActionType.Assassinate:
                    return ASSASSINATE_COST;
                default:
                    return 0;
            }
        }

        public static int CalculateActionGain(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Income:
                    return 1;
                case ActionType.ForeignAid:
                    return 2;
                case ActionType.Tax:
                    return 3;
                case ActionType.Steal:
                    return 2; // 최대값, 실제로는 상대방이 가진 코인에 따라 달라짐
                default:
                    return 0;
            }
        }
    }
}