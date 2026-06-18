using UnityEngine;

// Registers scene-specific objects with the shared save/load manager.
public class GameSaveSceneReferences : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Awake()
    {
        // Apply any loaded position/state after the scene has provided its player reference.
        GameSaveManager.RegisterPlayer(player);
        GameSaveManager.ApplyLoadedSceneStateForCurrentScene();
    }
}
