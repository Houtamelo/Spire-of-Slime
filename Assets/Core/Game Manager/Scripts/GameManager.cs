using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Audio.Scripts;
using Core.Audio.Scripts.MusicControllers;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.WinningCondition;
using Core.Local_Map.Scripts;
using Core.Local_Map.Scripts.PathCreating;
using Core.Main_Database;
using Core.Main_Database.Local_Map;
using Core.Main_Database.World_Map;
using Core.Main_Menu.Scripts;
using Core.Pause_Menu.Scripts;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using Core.World_Map.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Game_Manager.Scripts
{
    public sealed class GameManager : Singleton<GameManager>
    {
        public static readonly SceneRef[] CrucialScenes =
        {
            SceneRef.GameManager,
            SceneRef.PauseMenu,
            SceneRef.Audio,
            SceneRef.VisualNovel,
            SceneRef.ScreenButtons,
            SceneRef.CharacterPanel
        };

        private readonly IReadOnlyDictionary<SceneRef, IReadOnlyCollection<SceneRef>> _mutuallyExclusiveScenes = new Dictionary<SceneRef, IReadOnlyCollection<SceneRef>>
        {
            [SceneRef.MainMenu] = new[] { SceneRef.LocalMap, SceneRef.WorldMap, SceneRef.Combat, SceneRef.WorldMap },
            [SceneRef.LocalMap] = new[] { SceneRef.MainMenu, SceneRef.LocalMap },
            [SceneRef.WorldMap] = new[] { SceneRef.MainMenu, SceneRef.WorldMap },
            [SceneRef.Combat] = new[] { SceneRef.MainMenu, SceneRef.Combat },
            [SceneRef.PauseMenu] = Array.Empty<SceneRef>(),
            [SceneRef.VisualNovel] = Array.Empty<SceneRef>(),
            [SceneRef.StartScreen] = Array.Empty<SceneRef>(),
            [SceneRef.GameManager] = Array.Empty<SceneRef>(),
            [SceneRef.Audio] = Array.Empty<SceneRef>(),
            [SceneRef.ScreenButtons] = Array.Empty<SceneRef>(),
            [SceneRef.CharacterPanel] = Array.Empty<SceneRef>()
        };

        public delegate void RootEnabled(SceneRef scene);
        public static event RootEnabled OnRootEnabled;
        public static void NotifyRootEnabled(SceneRef scene) => OnRootEnabled?.Invoke(scene);

        public delegate void RootDisabled(SceneRef scene);
        public static event RootDisabled OnRootDisabled;
        public static void NotifyRootDisabled(SceneRef scene) => OnRootDisabled?.Invoke(scene);

        [OdinSerialize, Required]
        private readonly DatabaseManager _databaseManager;

        [OdinSerialize, Required]
        private readonly PathDatabase _pathDatabase;

        [SerializeField, Required]
        private GameObject urgentWarningObject;

        [SerializeField, Required]
        private TMP_Text urgentWarningText;

        [SerializeField, Required]
        private FadePanel fadePanel;
        public FadePanel FadePanel => fadePanel;

        private CoroutineWrapper _majorCoroutine;
        private readonly List<CoroutineWrapper> _minorCoroutines = new();
        
        protected override void Awake()
        {
            _databaseManager.SetReference();
            base.Awake();
        }

        public void ShowUrgentWarning(string text)
        {
            urgentWarningObject.SetActive(true);
            urgentWarningText.text = text;
        }

        public void InitializeFromStartScreen()
        {
            StartCoroutine(InitializeFromStartScreenRoutine());
        }

        private IEnumerator InitializeFromStartScreenRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            if (UnloadScene(SceneRef.StartScreen).TrySome(out CoroutineWrapper unloadingOperation))
                yield return unloadingOperation;

            for (int i = 0; i < CrucialScenes.Length; i++)
                LoadScene(CrucialScenes[i]);
            
            LoadScene(SceneRef.MainMenu);
            yield return fadePanel.FadeDown().WaitForCompletion();
        }
        
#if UNITY_EDITOR
        public void LoadScenesForTesting()
        {
            for (int i = 0; i < CrucialScenes.Length; i++)
                LoadScene(CrucialScenes[i]);
        }
#endif
        
        private void LoadScene([NotNull] SceneRef sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName.Name);
            if (scene.IsValid() && scene.isLoaded)
            {
                sceneName.SetObjectsActive(true);
                return;
            }

            SceneManager.LoadScene(sceneName.Name, LoadSceneMode.Additive);
            GC.Collect();
        }

        public Option<CoroutineWrapper> UnloadScene([NotNull] SceneRef sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName.Name);
            if (!scene.IsValid() || !scene.isLoaded)
                return Option<CoroutineWrapper>.None;

            AsyncOperation handle = SceneManager.UnloadSceneAsync(scene: scene);
            CoroutineWrapper routine = new(WaitForHandle(handle: handle), routineName: nameof(UnloadScene));
            RegisterMinorRoutine(wrapper: routine);
            return Option<CoroutineWrapper>.Some(value: routine);
        }

        private static IEnumerator WaitForHandle([NotNull] AsyncOperation handle)
        {
            while (!handle.isDone)
                yield return null;
        }
        
        [NotNull]
        public CoroutineWrapper WorldMapToLocalMap(WorldPath worldPath, in FullPathInfo fullPathInfo, LocationEnum origin, LocationEnum destination)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(WorldMapToLocalMapRoutine(worldPath: worldPath, fullPathInfo: fullPathInfo, origin: origin, destination: destination), routineName: nameof(WorldMapToLocalMapRoutine));
            return _majorCoroutine;
        }

        private IEnumerator WorldMapToLocalMapRoutine(WorldPath worldPath, FullPathInfo fullPathInfo, LocationEnum origin, LocationEnum destination)
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.SetController(worldPath.MusicController);

            SceneRef.WorldMap.SetObjectsActive(false);
            if (Save.AssertInstance(out Save save))
                save.SetNemaExhaustion(newValue: 0);
            
            yield return UnLoadMutuallyExclusiveScenes(SceneRef.LocalMap);

            LoadScene(SceneRef.LocalMap);
            while (LocalMapManager.Instance.IsNone)
                yield return null;

            PathBetweenNodesBlueprint[] nodes = new PathBetweenNodesBlueprint[worldPath.nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                bool isLast = i == nodes.Length - 1;
                nodes[i] = worldPath.nodes[i].GetBluePrint(deterministic: false, new OneWay(origin, destination), isLast);
            }

            LocalMapManager.Instance.Value.GenerateMap(source: worldPath, nodes, fullPathInfo, origin, destination);

            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager))
                saveFilesManager.WriteCurrentSessionToDisk(log: false);

            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        private CoroutineWrapper UnLoadMutuallyExclusiveScenes(SceneRef origin)
        {
            CoroutineWrapper routine = new(UnLoadMutuallyExclusiveScenesRoutine(origin: origin), routineName: nameof(UnLoadMutuallyExclusiveScenesRoutine));
            RegisterMinorRoutine(wrapper: routine);
            return routine;
        }

        private IEnumerator UnLoadMutuallyExclusiveScenesRoutine(SceneRef origin)
        {
            foreach (SceneRef scene in _mutuallyExclusiveScenes[key: origin])
            {
                Option<CoroutineWrapper> unloadScene = UnloadScene(sceneName: scene);
                if (unloadScene.IsSome)
                    yield return unloadScene.Value;
            }
        }

        [NotNull]
        public CoroutineWrapper LocalMapToCombat(CombatSetupInfo setupInfo, CombatTracker tracker, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(LocalMapToCombatRoutine(setupInfo, tracker, winningConditionGenerator, backgroundKey), routineName: nameof(LocalMapToCombatRoutine));
            return _majorCoroutine;
        }

        private IEnumerator LocalMapToCombatRoutine(CombatSetupInfo setupInfo, CombatTracker tracker, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey)
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.NotifyEvent(MusicEvent.Combat);

            yield return UnLoadMutuallyExclusiveScenes(SceneRef.Combat);
            SceneRef.LocalMap.SetObjectsActive(false);
            LoadScene(SceneRef.Combat);
            while (CombatManager.Instance.IsNone)
                yield return null;

            CombatManager.Instance.Value.SetupCombatFromBeginning(setupInfo, tracker, winningConditionGenerator, backgroundKey);
            yield return fadePanel.FadeDown().WaitForCompletion();
        }
        
        [NotNull]
        public CoroutineWrapper PlayerWonCombatOnLocalMap()
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(PlayerWonCombatOnLocalMapRoutine(), routineName: nameof(PlayerWonCombatOnLocalMapRoutine));
            return _majorCoroutine;
        }

        private IEnumerator PlayerWonCombatOnLocalMapRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            Option<CoroutineWrapper> unloadOperation = UnloadScene(SceneRef.Combat);
            if (unloadOperation.IsSome)
                yield return unloadOperation.Value;

            LoadScene(SceneRef.LocalMap);
            while (LocalMapManager.Instance.IsNone)
                yield return null;
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper PlayerLostCombatOnLocalMap()
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(PlayerLostCombatOnLocalMapRoutine(), routineName: nameof(PlayerLostCombatOnLocalMapRoutine));
            return _majorCoroutine;
        }
        
        private IEnumerator PlayerLostCombatOnLocalMapRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();

            Option<CoroutineWrapper> unloadOperation = UnloadScene(SceneRef.Combat);
            if (unloadOperation.IsSome)
                yield return unloadOperation.Value;

            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null, this is not supposed to happen.");
                ToMainMenuExclusive();
                yield break;
            }

            _majorCoroutine = null; // forfeit ownership of the major routine

            yield return LocalMapToWorldMap(save.Location);
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper VisualNovelToCombat(CombatSetupInfo combatSetupInfo, CombatTracker combatFlag, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey, MusicController musicController)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(VisualNovelToCombatRoutine(combatSetupInfo, combatFlag, winningConditionGenerator, backgroundKey, musicController), routineName: nameof(VisualNovelToCombatRoutine));
            return _majorCoroutine;
        }
        
        private IEnumerator VisualNovelToCombatRoutine(CombatSetupInfo combatSetupInfo, CombatTracker combatFlag, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey, MusicController musicController)
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.SetController(musicController);

            yield return UnLoadMutuallyExclusiveScenes(SceneRef.Combat);
            SceneRef.VisualNovel.SetObjectsActive(false);
            LoadScene(SceneRef.Combat);
            while (CombatManager.Instance.IsNone)
                yield return null;
            
            CombatManager.Instance.Value.SetupCombatFromBeginning(combatSetupInfo, combatFlag, winningConditionGenerator, backgroundKey);
            yield return fadePanel.FadeDown().WaitForCompletion();
        }
        
        [NotNull]
        public CoroutineWrapper VisualNovelToWorldmap()
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(VisualNovelToWorldmapRoutine(), routineName: nameof(VisualNovelToWorldmapRoutine));
            return _majorCoroutine;
        }

        private IEnumerator VisualNovelToWorldmapRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            yield return UnLoadMutuallyExclusiveScenes(SceneRef.WorldMap);
            SceneRef.VisualNovel.SetObjectsActive(false);
            LoadScene(SceneRef.WorldMap);
            while (WorldMapManager.Instance.IsNone)
                yield return null;

            SavePoint.RecordWorldMapPure();
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper LocalMapToWorldMap(LocationEnum location)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(LocalMapToWorldMapRoutine(location: location), routineName: nameof(LocalMapToWorldMapRoutine));
            return _majorCoroutine;
        }

        private IEnumerator LocalMapToWorldMapRoutine(LocationEnum location)
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.LocalMapEnds();
            
            if (UnloadScene(SceneRef.LocalMap).TrySome(out CoroutineWrapper unloadLocalMap))
                yield return unloadLocalMap;

            yield return UnLoadMutuallyExclusiveScenes(SceneRef.WorldMap);

            if (Save.AssertInstance(out Save save) == false)
            {
                ToMainMenuExclusive();
                yield break;
            }

            save.SetLocation(location);
            if (WorldScenesDatabase.GetWorldScene(location).TrySome(out WorldYarnScene worldScene) && DialogueController.AssertInstance(out DialogueController dialogueController))
            {
                SceneRef.WorldMap.SetObjectsActive(false);
                SavePoint.RecordWorldMapEvent(worldScene.SceneName);
                // yarn scene is responsible for fading down
                dialogueController.Play(worldScene.SceneName);
                yield break;
            }
            
            LoadScene(SceneRef.WorldMap);
            while (WorldMapManager.Instance.IsNone)
                yield return null;
            
            SavePoint.RecordWorldMapPure();

            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper NewGameFromMainMenu(string saveName)
        {
            Save.StartNewGame(name: saveName);
            return new CoroutineWrapper(NewGameFromMainMenuRoutine(), nameof(NewGameFromMainMenuRoutine), this, autoStart: true);
        }

        private IEnumerator NewGameFromMainMenuRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            yield return new WaitForSecondsRealtime(4); // wait until the "new game sfx is done playing"
            if (UnloadScene(SceneRef.MainMenu).TrySome(out CoroutineWrapper unloadOperation))
                yield return unloadOperation;
            
            SavePoint.RecordWorldMapEvent("Ch01_Awakening");

            if (DialogueController.AssertInstance(out DialogueController dialogueController))
                dialogueController.Play("Ch01_Awakening");
            
            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager))
                saveFilesManager.WriteCurrentSessionToDisk(log: true);
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper LoadSave(SaveRecord record)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(LoadSaveRoutine(record), routineName: nameof(LoadSaveRoutine));
            return _majorCoroutine;
        }

        private IEnumerator LoadSaveRoutine([NotNull] SaveRecord record)
        {
            Result<Save> save = Save.FromRecord(record);
            if (save.IsErr)
            {
                Debug.LogError(save.Reason);
                yield break;
            }
            
            Save.Handler.SetValue(save.Value);
            if (record == null)
                yield break;
            
            yield return fadePanel.FadeUp().WaitForCompletion();
            
            if (UnloadScene(SceneRef.MainMenu).TrySome(out CoroutineWrapper unloadOperation))
                yield return unloadOperation;

            SavePoint.Base savePoint = record.SavePoint;
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.SetController(savePoint.MusicKey);

            switch (savePoint)
            {
                case SavePoint.CombatInLocalMap combatInLocalMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.LocalMap);
                    LoadScene(SceneRef.LocalMap);
                    while (LocalMapManager.Instance.IsNone)
                        yield return null;

                    LocalMapManager.Instance.Value.GenerateMap(combatInLocalMap.LocalMapData);
                    SceneRef.LocalMap.SetObjectsActive(false);
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.Combat);
                    LoadScene(SceneRef.Combat);
                    
                    while (CombatManager.Instance.IsNone)
                        yield return null;
                    
                    CombatManager.Instance.Value.SetupCombatFromSave(combatInLocalMap.CombatData, combatInLocalMap.OnFinish);
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Combat);
                    
                    break;
                }
                case SavePoint.CombatInWorldMap combatInWorldMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.WorldMap);
                    LoadScene(SceneRef.WorldMap);
                    
                    while (WorldMapManager.Instance.IsNone)
                        yield return null;
                    
                    SceneRef.WorldMap.SetObjectsActive(false);
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.Combat);
                    
                    LoadScene(SceneRef.Combat);
                    
                    while (CombatManager.Instance.IsNone)
                        yield return null;
                    
                    CombatManager.Instance.Value.SetupCombatFromSave(combatInWorldMap.CombatData, combatInWorldMap.OnFinish);
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Combat);
                    
                    break;
                }
                case SavePoint.LocalMap localMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.LocalMap);
                    
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Exploration);
                    
                    LoadScene(SceneRef.LocalMap);
                    while (LocalMapManager.Instance.IsNone)
                        yield return null;

                    LocalMapManager.Instance.Value.GenerateMap(localMap.LocalMapData);
                    break;
                }
                case SavePoint.EventInLocalMap eventInLocalMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.LocalMap);
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.VisualNovel);
                    LoadScene(SceneRef.LocalMap);
                    LoadScene(SceneRef.VisualNovel);
                    while (LocalMapManager.Instance.IsNone || DialogueController.Instance.IsNone)
                        yield return null;
                    
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Exploration);
                    
                    LocalMapManager.Instance.Value.GenerateMap(eventInLocalMap.LocalMapData);
                    DialogueController.Instance.Value.Play(eventInLocalMap.Scene);
                    break;
                }
                case SavePoint.WorldMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.WorldMap);
                    LoadScene(SceneRef.WorldMap);
                    while (WorldMapManager.Instance.IsNone)
                        yield return null;
                    
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Exploration);
                    
                    break;
                }
                case SavePoint.EventInWorldMap eventInWorldMap:
                {
                    yield return UnLoadMutuallyExclusiveScenes(SceneRef.WorldMap);
                    LoadScene(SceneRef.WorldMap);
                    while (WorldMapManager.Instance.IsNone)
                        yield return null;

                    while (DialogueController.Instance.IsNone)
                        yield return null;
                    
                    if (MusicManager.AssertInstance(out musicManager))
                        musicManager.NotifyEvent(MusicEvent.Exploration);
                    
                    DialogueController.Instance.Value.Play(eventInWorldMap.Scene);
                    break;
                }
                default:                                          
                    throw new ArgumentOutOfRangeException(nameof(savePoint), message: "Invalid SavePoint type: " + (savePoint != null ? savePoint.GetType() : "null"));
            }
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        public Coroutine PauseMenuToMainMenu() => StartCoroutine(PauseMenuToMainMenuRoutine());

        private IEnumerator PauseMenuToMainMenuRoutine()
        {
            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager))
                saveFilesManager.WriteCurrentSessionToDisk(log: true);
            
            Save.Deactivate();
            
            Scene scene = SceneManager.GetSceneByName(SceneRef.MainMenu);
            if (scene.IsValid() && scene.isLoaded && scene.GetRootGameObjects().Any(obj => obj.activeInHierarchy))
            {
                if (PauseMenuManager.AssertInstance(out PauseMenuManager pauseMenuManager))
                    pauseMenuManager.Close();
                
                yield break;
            }

            if (DialogueController.AssertInstance(out DialogueController dialogueController))
                dialogueController.Stop();
            
            yield return fadePanel.FadeUp().WaitForCompletion();

            yield return UnLoadMutuallyExclusiveScenes(SceneRef.MainMenu);
            LoadScene(SceneRef.MainMenu);
            while (MainMenuManager.Instance.IsNone)
                yield return null;
            
            if (PauseMenuManager.AssertInstance(out PauseMenuManager pauseMenu))
                pauseMenu.Close();

            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        private void StopMajorRoutine()
        {
            if (_majorCoroutine is { Running: true })
            {
                Debug.Log(message: $"Major load coroutine is running while trying to start another one, previous: {_majorCoroutine.RoutineName}", context: _majorCoroutine.Context);
                Debug.Log(_majorCoroutine.StackTraceAtCreation);
                _majorCoroutine.ForceFinish();
            }
        }

        private void RegisterMinorRoutine([NotNull] CoroutineWrapper wrapper)
        {
            _minorCoroutines.Add(wrapper);
            wrapper.Finished += MinorRoutineFinished;
        }

        private void MinorRoutineFinished(bool _, CoroutineWrapper routine)
        {
            _minorCoroutines.Remove(routine);
        }

        private void StopAllMinorRoutines()
        {
            foreach (CoroutineWrapper coroutine in _minorCoroutines)
                coroutine.ForceFinish();
        }

        [NotNull]
        public CoroutineWrapper VisualNovelToLocalMap()
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(VisualNovelToLocalMapRoutine(), routineName: nameof(VisualNovelToLocalMapRoutine));
            return _majorCoroutine;
        }

        private IEnumerator VisualNovelToLocalMapRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();

            SceneRef.VisualNovel.SetObjectsActive(false);
            if (LocalMapManager.Instance.IsSome)
            {
                SceneRef.LocalMap.SetObjectsActive(true);
                yield return fadePanel.FadeDown().WaitForCompletion();
                yield break;
            }
            
            Debug.LogWarning("Trying to go to LocalMap from Visual Novel, but Local Map Scene is not loaded, returning to main menu...", this);
            yield return UnLoadMutuallyExclusiveScenes(SceneRef.MainMenu);
            LoadScene(SceneRef.MainMenu);
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper ToMainMenuExclusive()
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(ToMainMenuExclusiveRoutine(), routineName: nameof(ToMainMenuExclusiveRoutine));
            return _majorCoroutine;
        }
        
        private IEnumerator ToMainMenuExclusiveRoutine()
        {
            yield return fadePanel.FadeUp().WaitForCompletion();
            yield return UnLoadMutuallyExclusiveScenes(SceneRef.MainMenu);
            LoadScene(SceneRef.MainMenu);
            while (MainMenuManager.Instance.IsNone)
                yield return null;
            
            yield return fadePanel.FadeDown().WaitForCompletion();
        }

        [NotNull]
        public CoroutineWrapper CombatToVisualNovel(string visualNovelScene)
        {
            StopMajorRoutine();
            StopAllMinorRoutines();
            _majorCoroutine = new CoroutineWrapper(CombatToVisualNovelRoutine(sceneName: visualNovelScene), routineName: nameof(CombatToVisualNovelRoutine));
            return _majorCoroutine;
        }
        
        private IEnumerator CombatToVisualNovelRoutine(string sceneName)
        {
            yield return fadePanel.FadeUp().WaitForCompletion();

            Option<CoroutineWrapper> combat = UnloadScene(SceneRef.Combat);
            if (combat.IsSome)
                yield return combat.Value;
            
            LoadScene(SceneRef.VisualNovel);
            while (DialogueController.Instance.IsNone)
                yield return null;

            SavePoint.RecordSceneStartAnonymously(sceneName);

            SceneRef.VisualNovel.SetObjectsActive(true);
            DialogueController.Instance.Value.Play(sceneName);
            yield return fadePanel.FadeDown().WaitForCompletion();
        }
    }
}