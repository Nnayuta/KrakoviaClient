using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshMap : MonoBehaviour
{
    private NavMeshSurface surface;
    void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
        surface.enabled = true;
    }
}
