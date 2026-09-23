// Cliente/Scripts/Audio/SurfaceType.cs (NOVO ARQUIVO)
using UnityEngine;

[CreateAssetMenu(fileName = "New Surface Type", menuName = "RPG/Audio/Surface Type")]
public class SurfaceType : ScriptableObject
{
    [Header("Sons de Passos")]
    [Tooltip("A lista de clipes de áudio para este tipo de superfície.")]
    public AudioClip[] FootstepSounds;
}