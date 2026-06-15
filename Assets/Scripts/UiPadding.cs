using UnityEngine;

[System.Serializable]
public struct UiPadding
{
    public float left;
    public float right;
    public float top;
    public float bottom;

    public bool IsZero => left == 0f && right == 0f && top == 0f && bottom == 0f;

    public static UiPadding Create(float left, float right, float top, float bottom)
    {
        UiPadding padding;
        padding.left = left;
        padding.right = right;
        padding.top = top;
        padding.bottom = bottom;
        return padding;
    }

    public Rect Apply(Rect rect)
    {
        return new Rect(
            rect.x + left,
            rect.y + top,
            rect.width - left - right,
            rect.height - top - bottom);
    }
}
