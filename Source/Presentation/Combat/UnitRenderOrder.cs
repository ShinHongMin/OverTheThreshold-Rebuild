using UnityEngine;
using UnityEngine.Rendering;

/// <summary>발 위치가 아래인 유닛을 앞에 그린다. 배경 순서와 유닛 내부 장식 순서는 유지한다.</summary>
[DisallowMultipleComponent]
public sealed class UnitRenderOrder : MonoBehaviour
{
    [SerializeField] private int baseSortingOrder = 1000;
    [SerializeField] private int sortingStepsPerWorldUnit = 20;
    private SortingGroup _unitSortingGroup;

    private void Awake()
    {
        _unitSortingGroup = GetComponent<SortingGroup>();
        if (_unitSortingGroup == null) _unitSortingGroup = gameObject.AddComponent<SortingGroup>();
        // 기존 프리팹의 Shadow는 모델보다 높은 3이었다. 그룹 내부에서는 본체 뒤로 배치한다.
        foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
            if (spriteRenderer.gameObject.name == "Shadow") spriteRenderer.sortingOrder = -1;
        RefreshSortingOrder();
    }

    private void LateUpdate() => RefreshSortingOrder();

    public void RefreshSortingOrder()
    {
        if (_unitSortingGroup == null) return;
        _unitSortingGroup.sortingOrder = Mathf.Clamp(
            baseSortingOrder - Mathf.RoundToInt(transform.position.y * sortingStepsPerWorldUnit), 10, 32000);
    }
}
