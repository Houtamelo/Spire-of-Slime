using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unused.GUI_PRO_Kit___Fantasy_RPG.Scripts
{
    public class PanelControlFantasyRPG : MonoBehaviour
    {
        private int page = 0;
        private bool isReady = false;
        [SerializeField] private List<GameObject> panels = new();
        private TextMeshProUGUI textTitle;
        [SerializeField] private Transform panelTransform;
        [SerializeField] private Button buttonPrev;
        [SerializeField] private Button buttonNext;

        private void Start()
        {
            textTitle = transform.GetComponentInChildren<TextMeshProUGUI>();
            buttonPrev.onClick.AddListener(Click_Prev);
            buttonNext.onClick.AddListener(Click_Next);

            foreach (Transform t in panelTransform)
            {
                panels.Add(t.gameObject);
                t.gameObject.SetActive(false);
            }

            panels[index: page].SetActive(true);
            isReady = true;

            CheckControl();
        }

        private void Update()
        {
            if (panels.Count <= 0 || !isReady) return;

            if (Input.GetKeyDown(key: KeyCode.LeftArrow))
                Click_Prev();
            else if (Input.GetKeyDown(key: KeyCode.RightArrow))
                Click_Next();
        }

        //Click_Prev
        public void Click_Prev()
        {
            if (page <= 0 || !isReady) return;

            panels[index: page].SetActive(false);
            panels[index: page -= 1].SetActive(true);
            textTitle.text = panels[index: page].name;
            CheckControl();
        }

        //Click_Next
        public void Click_Next()
        {
            if (page >= panels.Count - 1) return;

            panels[index: page].SetActive(false);
            panels[index: page += 1].SetActive(true);
            CheckControl();
        }

        private void SetArrowActive()
        {
            buttonPrev.gameObject.SetActive(page > 0);
            buttonNext.gameObject.SetActive(page < panels.Count - 1);
        }

        //SetTitle, SetArrow Active
        private void CheckControl()
        {
            textTitle.text = panels[index: page].name.Replace(oldValue: "_", newValue: " ");
            SetArrowActive();
        }
    }
}
