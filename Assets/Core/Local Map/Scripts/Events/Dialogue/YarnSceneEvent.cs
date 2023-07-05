using System.Collections;
using Core.Local_Map.Scripts.Enums;
using Core.Main_Database.Visual_Novel;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Local_Map.Scripts.Events.Dialogue
{
    [CreateAssetMenu(fileName = "event_yarn-scene_", menuName = "Database/Local Map/Events/Yarn Scene Event")]
    public class YarnSceneEvent : ScriptableLocalMapEvent
    {
        [SerializeField]
        private string sceneName;

        [SerializeField]
        private VariableRequirement[] requirements = new VariableRequirement[0];

        [SerializeField]
        private bool specifyPriority;

        [SerializeField, ShowIf(nameof(specifyPriority))]
        private int priority;

        public int GetPriority => specifyPriority ? priority : -1;

        [SerializeField]
        private bool specifyPath;

        [field: SerializeField, ShowIf(nameof(specifyPath))]
        public bool IsOneWayPath { get; private set; }

        [SerializeField, ShowIf(nameof(specifyPath)), LabelText(@"$LabelOne"), ValidateInput(nameof(IsOneDifferentThanTwo))]
        private LocationEnum one;

        [SerializeField, ShowIf(nameof(specifyPath)), LabelText(@"$LabelTwo"), ValidateInput(nameof(IsOneDifferentThanTwo))]
        private LocationEnum two;

        [UsedImplicitly]
        private string LabelOne => IsOneWayPath ? "Origin" : "End/Start Location";

        [UsedImplicitly]
        private string LabelTwo => IsOneWayPath ? "Destination" : "End/Start Location";
        
        private bool IsOneDifferentThanTwo() => one != two;

        public override IconType GetIconType(in Option<float> multiplier) => IconType.Unknown;
        public override bool AllowSaving => false;

        /// <summary>
        /// Returns priority of this event if it's available
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Option<int> IsAvailable(in OneWay path)
        {
            Save save = Save.Current;
            if (save == null)
                return Option<int>.None;

            if (specifyPath && ((IsOneWayPath && (path.Origin != one || path.Destination != two)) || path != new BothWays(one, two)))
                return Option<int>.None;

            foreach (VariableRequirement requirement in requirements)
                if (!requirement.Validate(save))
                    return Option<int>.None;

            return GetPriority;
        }
        
        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) => new(DialogueRoutine(), nameof(DialogueRoutine), this, autoStart: true);

        private IEnumerator DialogueRoutine()
        {
            if (DialogueController.AssertInstance(out DialogueController dialogueController) == false)
                yield break;

            if (dialogueController.SceneExists(sceneName) == false)
            {
                Debug.LogWarning($"Scene doesn't exist for dialogue event:{sceneName}");
                yield break;
            }
            
            SavePoint.RecordLocalMapEventStart(sceneName);

            yield return dialogueController.Play(sceneName);
            // what happens after the dialogue is handled by the dialogue file
        }
    }
}