using Core.World_Map.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public class RestEventBackground : MonoBehaviour
    {
        [SerializeField, Required]
        private GameObject ethelNormal, ethelLustful;

        [SerializeField, Required]
        private GameObject nemaNormal, nemaLustful;

        [SerializeField, Required]
        private Transform ethelPos, nemaPos;

        [SerializeField]
        private BothWays location;
        public BothWays Location => location;
        
        public Vector3 EthelWorldPosition => ethelPos.position;
        public Vector3 NemaWorldPosition => nemaPos.position;
        
        //todo! needs ambience

        private void OnEnable()
        {
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Rest event background enabled but no save is active.", this);
                return;
            }

            if (save.EthelStats.Lust >= 100)
            {
                ethelLustful.gameObject.SetActive(true);
                ethelNormal.gameObject.SetActive(false);
            }
            else
            {
                ethelLustful.gameObject.SetActive(false);
                ethelNormal.gameObject.SetActive(true);
            }
            
            if (save.NemaStats.Lust >= 100)
            {
                nemaLustful.gameObject.SetActive(true);
                nemaNormal.gameObject.SetActive(false);
            }
            else
            {
                nemaLustful.gameObject.SetActive(false);
                nemaNormal.gameObject.SetActive(true);
            }
        }
    }
}