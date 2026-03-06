using UnityEngine;
using UnityEngine.UI;

namespace FlashFriends
{
    /// <summary>
    /// Joue le son de clic UI (via <see cref="AudioManager"/>) à chaque clic sur le bouton.
    /// Attacher sur n'importe quel bouton.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSound : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(PlayClickSound);
        }
    }
}
