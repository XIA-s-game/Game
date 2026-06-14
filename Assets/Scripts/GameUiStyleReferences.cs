using UnityEngine;

public class GameUiStyleReferences : MonoBehaviour
{
    [SerializeField] private Texture2D panelTexture;
    [SerializeField] private Texture2D dialoguePanelTexture;
    [SerializeField] private Texture2D bagTexture;
    [SerializeField] private Texture2D menuTexture;

    private void Awake()
    {
        GameUiStyle.SetTextures(panelTexture, dialoguePanelTexture, bagTexture, menuTexture);
    }
}
