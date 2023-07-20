using System.Collections.Generic;
using System.Linq;
using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts;
using Core.Main_Database.Local_Map;
using Core.Main_Database.World_Map;
using Core.Utils.Collections.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.World_Map.Scripts
{
    public sealed class WorldMapManager : Singleton<WorldMapManager>
    {
        public static bool LOG;
        
        private const float CameraLerpDuration = 0.5f;

        [OdinSerialize, Required, SceneObjectsOnly]
        private readonly Transform _sceneRoot;

        [OdinSerialize, Required, SceneObjectsOnly]
        private readonly Transform _virtualCameraTransform;

        [OdinSerialize, Required, SceneObjectsOnly]
        private Transform _playerIcon;

        [OdinSerialize, Required, SceneObjectsOnly]
        private AudioSource _locationConfirmedAudioSource;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource badInputAudioSource;

        private Dictionary<LocationEnum, LocationButton> _buttons;
        private Dictionary<BothWays, DottedLine> _dottedLines;

        private readonly HashSet<LocationEnum> _currentLocations = new();
        private readonly HashSet<LocationEnum> _previousLocations = new();
        private readonly Dictionary<OneWay, WorldPath> _availablePaths = new();

        protected override void Awake()
        {
            base.Awake();
            Save.LocationChanged += CheckPlayerLocation;
            DottedLine[] dottedLines = _sceneRoot.GetComponentsInChildren<DottedLine>(includeInactive: true);
            _dottedLines = dottedLines.ToDictionary(keySelector: x => x.Way, elementSelector: x => x);
            
            LocationButton[] buttons = _sceneRoot.GetComponentsInChildren<LocationButton>(includeInactive: true);
            _buttons = buttons.ToDictionary(keySelector: x => x.Location, elementSelector: x => x);
        }

        private void Start()
        {
            if (Save.Current != null)
                CheckPlayerLocation(location: Save.Current.Location);
            else
                Debug.LogWarning("Current save is null on world map", this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Save.LocationChanged -= CheckPlayerLocation;
        }

        private void OnEnable()
        {
            CheckAvailableLocations();
        }
        
        private void CheckPlayerLocation(LocationEnum location)
        {
            Vector3 worldPos = _buttons[location].transform.position;
            worldPos.z = _virtualCameraTransform.position.z;
            _virtualCameraTransform.DOKill();
            _virtualCameraTransform.transform.DOMove(endValue: worldPos, duration: CameraLerpDuration).SetSpeedBased(isSpeedBased: false).SetTarget(target: _virtualCameraTransform);

            _playerIcon.DOKill();
            worldPos.z = 0;
            _playerIcon.transform.DOMove(endValue: worldPos, duration: CameraLerpDuration).SetSpeedBased(isSpeedBased: false).SetTarget(target: _playerIcon);
        }

        private void CheckAvailableLocations()
        {
            if (Save.Current == null)
                return;
            
            Save save = Save.Current;
            LocationEnum origin = save.Location;
            _previousLocations.Clear();
            _previousLocations.Add(_currentLocations);
            _currentLocations.Clear();
            _availablePaths.Clear();
            Dictionary<LocationEnum, WorldPath> availablePaths = WorldPathDatabase.GetAvailablePathsFrom(origin);
            foreach ((LocationEnum destination, WorldPath path) in availablePaths)
                _availablePaths.Add(new OneWay(origin, destination), path);

            _currentLocations.Add(availablePaths.Keys);
            _currentLocations.Add(origin);
            _dottedLines.Values.DoForEach(line => line.gameObject.SetActive(false));

            foreach (OneWay way in _availablePaths.Keys)
            {
                DottedLine line = _dottedLines[way];
                LocationButton start = _buttons[origin];
                LocationButton end = _buttons[way.Destination];
                line.Initialize(start, end);
            }

            _buttons.Values.DoForEach(button => button.SetActive(false));
            foreach (LocationEnum location in _currentLocations)
                _buttons[location].SetActive(true);
        }

        private Option<FullPathInfo> GeneratePathInfo(in BothWays bothWays)
        {
            Option<PathInfo> pathInfo = PathDatabase.GetPathInfo(bothWays);
            if (pathInfo.IsNone)
                return Option<FullPathInfo>.None;
            
            LocationButton start = _buttons[bothWays.One];
            LocationButton end = _buttons[bothWays.Two];
            float signedAngle =  -1 * Vector2.SignedAngle(from: end.transform.position - start.transform.position, to: Vector2.right);
            float polarAngle = signedAngle < 0 ? Mathf.Abs(signedAngle) + 180 : signedAngle;
            Option<TileInfo> startTileInfo = TileInfoDatabase.GetWorldLocationTileInfo(bothWays.One);
            if (startTileInfo.IsNone)
                Debug.LogWarning($"Unable to find tile info for {Enum<LocationEnum>.ToString(bothWays.One)}", this);
            
            FullPathInfo fullPathInfo = new(pathInfo: pathInfo.Value, polarEndAngle: polarAngle, startCellInfo: startTileInfo.SomeOrDefault());
            return fullPathInfo;
        }

        public bool LocationButtonClicked(LocationEnum buttonLocation)
        {
            if (GameManager.AssertInstance(out GameManager gameManager) == false || Save.AssertInstance(out Save save) == false)
                return false;
            
            LocationEnum saveLocation = save.Location;
            if (saveLocation == buttonLocation)
            {
                badInputAudioSource.Play();
                return false;
            }
            
            if (_availablePaths.TryGetValue(new OneWay(saveLocation, buttonLocation), out WorldPath path) == false)
                return false;

            if (LOG)
                Debug.Log($"Generating local map for path: {path.name}", context: path);

            Option<FullPathInfo> fullPathInfo = GeneratePathInfo(new BothWays(saveLocation, buttonLocation));
            if (fullPathInfo.IsNone)
            {
                Debug.LogWarning($"Unable to generate path info for {Enum<LocationEnum>.ToString(saveLocation)} to {Enum<LocationEnum>.ToString(buttonLocation)}", context: this);
                return false;
            }

            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.EnteringLocalMap.Play();

            gameManager.WorldMapToLocalMap(path, fullPathInfo.Value, origin: saveLocation, destination: buttonLocation);
            return true;
        }
        
        public void LocationButtonPointerEnter([NotNull] LocationButton locationButton)
        {
            LocationEnum saveLocation = Save.Current.Location;
            LocationEnum buttonLocation = locationButton.Location;
            if (saveLocation == buttonLocation)
                return;
            
            if (_availablePaths.TryGetValue(new OneWay(saveLocation, buttonLocation), out _) == false)
                return;

            foreach (DottedLine dottedLine in _dottedLines.Values)
            {
                if (dottedLine.StartLocationButton == null || dottedLine.EndLocationButton == null)
                    continue;

                LocationEnum endLocation = dottedLine.EndLocationButton.Location;
                LocationEnum startLocation = dottedLine.StartLocationButton.Location;
                if ((startLocation == saveLocation && endLocation == buttonLocation) || (startLocation == buttonLocation && endLocation == saveLocation))
                {
                    dottedLine.HighLight();
                    break;
                }
            }
        }
        
        public void LocationButtonPointerExit([NotNull] LocationButton locationButton)
        {
            LocationEnum saveLocation = Save.Current.Location;
            LocationEnum buttonLocation = locationButton.Location;
            if (saveLocation == buttonLocation)
                return;
            
            if (!_availablePaths.TryGetValue(new OneWay(saveLocation, buttonLocation), out _))
                return;

            foreach (DottedLine dottedLine in _dottedLines.Values)
            {
                if (dottedLine.StartLocationButton == null || dottedLine.EndLocationButton == null)
                    continue;

                LocationEnum endLocation = dottedLine.EndLocationButton.Location;
                LocationEnum startLocation = dottedLine.StartLocationButton.Location;
                if ((startLocation == saveLocation && endLocation == buttonLocation) || (startLocation == buttonLocation && endLocation == saveLocation))
                {
                    dottedLine.LowLight();
                    break;
                }
            }
        }

        public void DottedLineClicked([NotNull] DottedLine dottedLine)
        {
            LocationEnum startLocation = dottedLine.StartLocationButton.Location;
            LocationEnum endLocation = dottedLine.EndLocationButton.Location;
            if (LocationButtonClicked(startLocation) == false)
                LocationButtonClicked(endLocation);
        }

        public void DottedLinePointerExit([NotNull] DottedLine dottedLine)
        {
            dottedLine.EndLocationButton.LowLight();
            dottedLine.StartLocationButton.LowLight();
        }

        public void DottedLinePointerEnter([NotNull] DottedLine dottedLine)
        {
            Save save = Save.Current;
            if (dottedLine.StartLocationButton.Location != save.Location && dottedLine.EndLocationButton.Location != save.Location)
                return;
            
            if (_availablePaths.TryGetValue(new OneWay(save.Location, dottedLine.StartLocationButton.Location), out _))
                dottedLine.StartLocationButton.HighLight();
            else if (_availablePaths.TryGetValue(new OneWay(save.Location, dottedLine.EndLocationButton.Location), out _))
                dottedLine.EndLocationButton.HighLight();
        }
    }
}