using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Misc
{
    public class HyperlinkOpener : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Camera uiCamera;
        [SerializeField] private TMP_Text tmp;
        [SerializeField, ShowIf(nameof(playAudio))] private AudioSource audioSource;
        [SerializeField] private bool playAudio;

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmp, new Vector3(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0), uiCamera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = tmp.textInfo.linkInfo[linkIndex];
                Application.OpenURL(linkInfo.GetLinkID());
                
                if (playAudio) 
                    audioSource.Play();
            }
        }
    }
}