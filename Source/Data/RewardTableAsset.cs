using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>전투 종류별 보상 후보. UI나 전투 코드에 카드 목록을 넣지 않는다.</summary>
[CreateAssetMenu(menuName = "Data/Reward Table")]
public sealed class RewardTableAsset : ScriptableObject
{
    [Serializable]
    public sealed class CardPool
    {
        public NodeType nodeType;
        public List<PassiveCardData> cards = new List<PassiveCardData>();
    }
    [SerializeField] private List<CardPool> cardPools = new List<CardPool>();

    /// <summary>런마다 한 번 변환한다. 여러 보상 목록에 들어 있는 같은 카드는 한 번만 등록한다.</summary>
    public PassiveCardRules BuildPassiveRules()
    {
        var rules = new PassiveCardRules();
        var registeredCards = new Dictionary<string, PassiveCardData>(StringComparer.Ordinal);
        foreach (var pool in cardPools)
        {
            if (pool == null) continue;
            foreach (var card in pool.cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.cardId)) throw new InvalidOperationException("카드 데이터와 ID를 확인하세요.");
                if (registeredCards.TryGetValue(card.cardId, out var registered))
                {
                    if (registered != card) throw new InvalidOperationException("서로 다른 카드가 같은 ID를 사용합니다: " + card.cardId);
                    continue;
                }
                registeredCards.Add(card.cardId, card);
                rules.RegisterCard(card.cardId, card.effectType, card.effectValue);
            }
        }
        return rules;
    }

    public IReadOnlyList<string> GetCardIds(NodeType nodeType)
    {
        CardPool selectedPool = null;
        foreach (var pool in cardPools)
            if (pool != null && pool.nodeType == nodeType)
            {
                if (selectedPool != null) throw new InvalidOperationException("중복된 보상 노드 종류: " + nodeType);
                selectedPool = pool;
            }
        if (selectedPool == null) throw new InvalidOperationException("보상 테이블에 없는 노드 종류: " + nodeType);
        var ids = new List<string>(selectedPool.cards.Count);
        var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in selectedPool.cards)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.cardId) || !uniqueIds.Add(card.cardId))
                throw new InvalidOperationException("보상 테이블의 카드가 없거나 ID가 중복됩니다: " + nodeType);
            ids.Add(card.cardId);
        }
        if (ids.Count < 2) throw new InvalidOperationException("보상 후보가 2장 이상 필요합니다: " + nodeType);
        return ids;
    }

    public PassiveCardData GetCard(string cardId)
    {
        PassiveCardData found = null;
        foreach (var pool in cardPools)
            if (pool != null)
                foreach (var card in pool.cards)
                    if (card != null && card.cardId == cardId)
                    {
                        if (found != null && found != card) throw new InvalidOperationException("서로 다른 카드가 같은 ID를 사용합니다: " + cardId);
                        found = card;
                    }
        if (found == null) throw new InvalidOperationException("카드 데이터를 찾을 수 없습니다: " + cardId);
        return found;
    }
}
