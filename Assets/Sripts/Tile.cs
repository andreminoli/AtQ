using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int gridPosition;
    private Renderer rend;
    private Color originalColor;
    private Action<Tile> onClickCallback;

    public void Init(Vector2Int position)
    {
        gridPosition = position;
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    public void Highlight(Color color, Action<Tile> onClick)
    {
        rend.material.color = color;
        onClickCallback = onClick;
    }

    public void ClearHighlight()
    {
        rend.material.color = originalColor;
        onClickCallback = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClickCallback != null)
        {
            Debug.Log($"✅ Tile clicked at {gridPosition}");
            onClickCallback.Invoke(this);
        }
    }
}
