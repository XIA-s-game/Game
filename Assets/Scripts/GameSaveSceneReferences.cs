using UnityEngine;

public class GameSaveSceneReferences : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Awake()
    {
        GameSaveManager.RegisterPlayer(player);
        GameSaveManager.ApplyLoadedSceneStateForCurrentScene();
    }
}
