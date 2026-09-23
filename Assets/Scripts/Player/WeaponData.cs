using UnityEngine;

public class WeaponData : MonoBehaviour
{
    public enum Hand { Left = 0, Right = 1 }

    [Header("Hand")]
    public Hand handAnchor;

[Header("Hand Offsets")]
    public Vector3 handPositionOffset;
    public Vector3 handRotationOffset;

    [Header("Back Offsets")]
    public Vector3 backPositionOffset;
    public Vector3 backRotationOffset;
}