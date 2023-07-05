using System.Collections;
using Core.Game_Manager.Scripts;
using Core.Utils.Async;
using Core.Visual_Novel.Scripts;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Test
{
    public class TestManager : MonoBehaviour
    {
        [SerializeField]
        private Button firstCombat, firstWorldMap;

        private void Awake()
        {
            firstCombat.onClick.AddListener( () => new CoroutineWrapper(ToFirstCombatRoutine(), nameof(ToFirstCombatRoutine), this, autoStart: true));
            firstCombat.onClick.AddListener(() =>
                                            {
                                                firstCombat.interactable = false;
                                                firstWorldMap.interactable = false;
                                            });
            firstCombat.GetComponentInChildren<TMP_Text>().text = "To First Combat";
            
            firstWorldMap.onClick.AddListener( () => new CoroutineWrapper(ToFirstWorldMapRoutine(), nameof(ToFirstWorldMapRoutine), this, autoStart: true));
            firstWorldMap.onClick.AddListener(() =>
                                              {
                                                  firstCombat.interactable = false;
                                                  firstWorldMap.interactable = false;
                                              });
            
            firstWorldMap.GetComponentInChildren<TMP_Text>().text = "To First World Map";
        }

        private IEnumerator ToFirstCombatRoutine()
        {
            Save.StartNewGame("test");
            GameManager gameManager = GameManager.Instance.Value;
            yield return gameManager.FadePanel.FadeUp().WaitForCompletion();
            if (gameManager.UnloadScene(SceneRef.MainMenu).TrySome(out CoroutineWrapper unloadOperation))
                yield return unloadOperation;

            YarnCommandHandler.StartCombat("combat_ch01_tutorial","Ch01_Won-First-Fight", "Ch01_Rescued_by_Mistress_Tender");
        }
        
        private IEnumerator ToFirstWorldMapRoutine()
        {
            Save.StartNewGame("test");
            GameManager gameManager = GameManager.Instance.Value;
            yield return gameManager.FadePanel.FadeUp().WaitForCompletion();
            if (gameManager.UnloadScene(SceneRef.MainMenu).TrySome(out CoroutineWrapper unloadOperation))
                yield return unloadOperation;

            Save.Current.SetVariable("Finished_Ch01_Awakening", true);
            Save.Current.SetVariable("Finished_Ch01_First_Fight", true);
            Save.Current.SetVariable("Finished_Ch01_Rescued_by_Mistress_Tender", true);
            Save.Current.SetVariable("Finished_Ch01_Ethel_Nema_discuss_dragonkin_quest", true);
            Save.Current.SetVariable("Finished_Ch01_Leaving_chapel_towards_dragonkin", true);
            YarnCommandHandler.AwardLevel();
            
            YarnCommandHandler.StartWorldMap();
        }
    }
}