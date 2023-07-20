using System.Text;
using Core.Audio.Scripts;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts;
using Core.Utils.Extensions;
using Core.Visual_Novel.Scripts;
using JetBrains.Annotations;

namespace Core.Save_Management.SaveObjects
{
    public static class SavePoint
    {
        public abstract record Base(CleanString MusicKey);

        public record WorldMap(CleanString MusicKey) : Base(MusicKey);
        public record EventInWorldMap(CleanString MusicKey, string Scene) : Base(MusicKey); // custom one that should only be created manually
        public record LocalMap(CleanString MusicKey, LocalMapRecord LocalMapData) : Base(MusicKey);
        public record EventInLocalMap(CleanString MusicKey, LocalMapRecord LocalMapData, string Scene) : Base(MusicKey); // custom one that should only be created manually
        public record CombatInLocalMap(CleanString MusicKey, LocalMapRecord LocalMapData, CombatRecord CombatData, CombatTracker.FinishRecord OnFinish) : Base(MusicKey);
        public record CombatInWorldMap(CleanString MusicKey, CombatRecord CombatData, CombatTracker.FinishRecord OnFinish) : Base(MusicKey);

        public static void TryGenerateFromCurrentSession()
        {
            if (Save.AssertInstance(out Save save) == false)
                return;
            
            if (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning)
                return;

            CleanString musicKey = GetCurrentMusicKey();
            if (SceneRef.Combat.IsLoadedAndActive())
            {
                if (CombatManager.Instance.TrySome(out CombatManager combatManager) == false || combatManager.Running == false || combatManager.Tracker.IsNone)
                    return;

                CombatRecord combatData = combatManager.GenerateRecord();
                CombatTracker.FinishRecord onFinish = combatManager.Tracker.Value.OnFinish.DeepClone();
                if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager))
                {
                    LocalMapRecord localMapData = localMapManager.GenerateRecord();
                    save.ForceRecordManually(new CombatInLocalMap(musicKey, localMapData, combatData, onFinish));
                    return;
                }

                save.ForceRecordManually(new CombatInWorldMap(musicKey, combatData, onFinish));
                return;
            }
            
            if (SceneRef.LocalMap.IsLoadedAndActive())
            {
                if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager) == false || localMapManager.RunningEvent)
                    return;

                LocalMapRecord localMapData = localMapManager.GenerateRecord();
                save.ForceRecordManually(new LocalMap(musicKey, localMapData));
                return;
            }

            save.ForceRecordManually(new WorldMap(musicKey));
        }

        public static void RecordLocalMap([NotNull] LocalMapManager manager)
        {
            if (manager.RunningEvent || 
                Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) || 
                SceneRef.Combat.IsLoadedAndActive())
                return;
            
            LocalMapRecord localMapData = manager.GenerateRecord();
            LocalMap record = new(GetCurrentMusicKey(), localMapData);
            save.ForceRecordManually(record);
        }

        public static void RecordLocalMapEventStart(string sceneName)
        {
            if (Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) || 
                SceneRef.Combat.IsLoadedAndActive())
                return;

            if (SceneRef.LocalMap.IsLoadedAndActive() == false)
                return;
            
            if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager) == false)
                return;

            LocalMapRecord localMapData = localMapManager.GenerateRecord();
            EventInLocalMap record = new(GetCurrentMusicKey(), localMapData, sceneName);
            save.ForceRecordManually(record);
        }

        public static void RecordWorldMapPure()
        {
            if (Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) || 
                SceneRef.Combat.IsLoadedAndActive() || 
                SceneRef.LocalMap.IsLoadedAndActive())
                return;
            
            WorldMap record = new(GetCurrentMusicKey());
            save.ForceRecordManually(record);
        }

        public static void RecordWorldMapEvent(string scene)
        {
            if (Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) || 
                SceneRef.Combat.IsLoadedAndActive() || 
                SceneRef.LocalMap.IsLoadedAndActive())
                return;
            
            EventInWorldMap record = new(GetCurrentMusicKey(), scene);
            save.ForceRecordManually(record);
        }

        public static void RecordSceneStartAnonymously(string sceneName)
        {
            if (Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) ||
                SceneRef.Combat.IsLoadedAndActive())
                return;
            
            if (SceneRef.LocalMap.IsLoadedAndActive())
            {
                if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager) == false || localMapManager.RunningEvent)
                    return;

                LocalMapRecord localMapData = localMapManager.GenerateRecord();
                save.ForceRecordManually(new EventInLocalMap(GetCurrentMusicKey(), localMapData, sceneName));
                return;
            }

            save.ForceRecordManually(new EventInWorldMap(GetCurrentMusicKey(), sceneName));
        }

        public static void RecordCombatStart()
        {
            if (Save.AssertInstance(out Save save) == false ||
                (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) ||
                SceneRef.Combat.IsLoadedAndActive() == false ||
                CombatManager.Instance.TrySome(out CombatManager combatManager) == false ||
                combatManager.Running == false ||
                combatManager.Tracker.IsNone)
                return;
            
            CombatRecord combatData = combatManager.GenerateRecord();
            CombatTracker.FinishRecord onFinish = combatManager.Tracker.Value.OnFinish.DeepClone();
            if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager))
            {
                LocalMapRecord localMapData = localMapManager.GenerateRecord();
                save.ForceRecordManually(new CombatInLocalMap(GetCurrentMusicKey(), localMapData, combatData, onFinish));
            }
            else
            {
                save.ForceRecordManually(new CombatInWorldMap(GetCurrentMusicKey(), combatData, onFinish));
            }
        }

        public static bool IsDataValid([CanBeNull] this Base savePoint, StringBuilder errors)
        {
            switch (savePoint)
            {
                case CombatInLocalMap combatInLocalMap:
                {
                    if (combatInLocalMap.LocalMapData == null)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatInLocalMap), ". ", nameof(combatInLocalMap.LocalMapData), " is null.");
                        return false;
                    }

                    if (combatInLocalMap.LocalMapData.IsDataValid(errors) == false)
                        return false;

                    if (combatInLocalMap.CombatData == null)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatInLocalMap), ". ", nameof(combatInLocalMap.CombatData), " is null.");
                        return false;
                    }
                    
                    if (combatInLocalMap.CombatData.IsDataValid(errors) == false)
                        return false;

                    if (combatInLocalMap.OnFinish == null)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatInLocalMap), ". ", nameof(combatInLocalMap.OnFinish), " is null.");
                        return false;
                    }
                    
                    if (combatInLocalMap.OnFinish.IsDataValid(errors) == false)
                        return false;

                    return true;
                }
                case CombatInWorldMap combatInWorldMap:
                {
                    if (combatInWorldMap.CombatData == null)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatInWorldMap), ". ", nameof(combatInWorldMap.CombatData), " is null.");
                        return false;
                    }
                    
                    if (combatInWorldMap.CombatData.IsDataValid(errors) == false)
                        return false;

                    if (combatInWorldMap.OnFinish == null)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatInWorldMap), ". ", nameof(combatInWorldMap.OnFinish), " is null.");
                        return false;
                    }
                    
                    if (combatInWorldMap.OnFinish.IsDataValid(errors) == false)
                        return false;

                    return true;
                }
                case EventInLocalMap eventInLocalMap:
                {
                    if (eventInLocalMap.LocalMapData == null)
                    {
                        errors.AppendLine("Invalid ", nameof(EventInLocalMap), ". ", nameof(eventInLocalMap.LocalMapData), " is null.");
                        return false;
                    }
                    
                    if (eventInLocalMap.LocalMapData.IsDataValid(errors) == false)
                        return false;

                    if (YarnDatabase.SceneExists(eventInLocalMap.Scene) == false)
                    {
                        errors.AppendLine("Invalid ", nameof(EventInLocalMap), ". ", nameof(eventInLocalMap.Scene), ": ", eventInLocalMap.Scene, " does not exist in database.");
                        return false;
                    }
                    
                    return true;
                }
                case EventInWorldMap eventInWorldMap:
                {
                    if (YarnDatabase.SceneExists(eventInWorldMap.Scene) == false)
                    {
                        errors.AppendLine("Invalid ", nameof(EventInWorldMap), ". ", nameof(eventInWorldMap.Scene), ": ", eventInWorldMap.Scene, " does not exist in database.");
                        return false;
                    }
                    
                    return true;
                }
                case LocalMap localMap:
                {
                    if (localMap.LocalMapData == null)
                    {
                        errors.AppendLine("Invalid ", nameof(LocalMap), ". ", nameof(localMap.LocalMapData), " is null.");
                        return false;
                    }
                    
                    if (localMap.LocalMapData.IsDataValid(errors) == false)
                        return false;
                    
                    return true;
                }
                case WorldMap worldMap:
                    return true;
                default:
                {
                    errors.AppendLine("Invalid ", nameof(Base), ". ", nameof(savePoint), " is of unexpected type. Actual type: ", savePoint == null ? "null" : savePoint.GetType().Name);
                    return false;
                }
            }
        }

        private static CleanString GetCurrentMusicKey()
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                return musicManager.CurrentMusicKey;

            return string.Empty;
        }
    }
}