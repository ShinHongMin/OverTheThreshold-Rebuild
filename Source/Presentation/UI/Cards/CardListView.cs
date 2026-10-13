using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>획득 순서대로 카드 목록을 그리는 공통 모듈. 팝업과 결산창이 같은 행 프리팹과 풀을 사용한다.</summary>
public sealed class CardListView : MonoBehaviour
{
    [SerializeField] private OwnedCardRowView cardRowPrefab;
    [SerializeField] private RectTransform cardContent;
    [SerializeField] private ScrollRect cardScroll;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField, Min(0)] private int initialRowCount = 6;
    [SerializeField, Min(1)] private int rowCapacity = 64;
    private readonly List<OwnedCardRowView> _visibleRows = new List<OwnedCardRowView>();
    private IReadOnlyList<string> _cardIds;
    private RewardTableAsset _cardTable;
    private PrefabPoolManager _pool;

    public void Configure(PrefabPoolManager pool)
    {
        ResetDisplay();
        _pool = pool;
        if (_pool != null && cardRowPrefab != null) _pool.Prewarm(cardRowPrefab.gameObject, initialRowCount, rowCapacity);
    }

    public void Bind(IReadOnlyList<string> cardIds, RewardTableAsset cardTable)
    {
        _cardIds = cardIds;
        _cardTable = cardTable;
    }

    public void ShowCards()
    {
        ClearDisplay();
        if (_cardIds == null || _cardTable == null || _pool == null || cardRowPrefab == null)
            throw new InvalidOperationException("카드 목록·데이터 테이블·풀 연결이 필요합니다.");
        emptyText.gameObject.SetActive(_cardIds.Count == 0);
        try
        {
            for (int index = 0; index < _cardIds.Count; index++)
            {
                var card = _cardTable.GetCard(_cardIds[index]);
                var rented = _pool.Rent(cardRowPrefab.gameObject, cardContent);
                if (rented == null) throw new InvalidOperationException("카드 목록의 풀 용량이 부족합니다.");
                var row = rented.GetComponent<OwnedCardRowView>();
                _visibleRows.Add(row);
                row.transform.SetAsLastSibling();
                row.Bind(card);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContent);
            cardScroll.StopMovement();
            cardScroll.verticalNormalizedPosition = 1f;
        }
        catch
        {
            ClearDisplay();
            throw;
        }
    }

    public void ClearDisplay() => ClearRows();

    public void ResetDisplay()
    {
        ClearRows();
        _cardIds = null;
        _cardTable = null;
    }

    private void ClearRows(bool moveToStorage = true)
    {
        if (_pool != null)
        {
            // 역순으로 반환하면 다음 대여 때 같은 카드가 같은 행을 받아 높이 계산을 재사용할 수 있다.
            for (int index = _visibleRows.Count - 1; index >= 0; index--)
            {
                var row = _visibleRows[index];
                if (row != null) _pool.Return(row.gameObject, moveToStorage);
            }
        }
        _visibleRows.Clear();
    }

    private void OnDisable() => ClearRows(false);
}
