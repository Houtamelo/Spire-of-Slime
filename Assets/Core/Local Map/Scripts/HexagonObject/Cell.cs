using System;
using System.Collections.Generic;
using System.Text;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.Enums;
using Core.Local_Map.Scripts.Events;
using Core.Main_Database.Local_Map;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Utils.Patterns;
using Random = UnityEngine.Random;


namespace Core.Local_Map.Scripts.HexagonObject
{
    public class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IEquatable<Cell>
    {
        [SerializeField, Required]
        private RectTransform rectTransform;

        [SerializeField, Required]
        private SpriteRenderer mainImage;

        [SerializeField, Required]
        private SpriteRenderer eventIcon;

        [SerializeField, Required]
        private Image mouseOverImage;

        [SerializeField]
        private float size;
        public float Size => size;

        [SerializeField, Required]
        private Sprite mouseOverSprite, mouseOverWalkableSprite;

        [SerializeField]
        private float normalIntensity = 1f, exploredIntensity = 0.5f;

        [SerializeField]
        private Vector3 activeLightSourceScale;

        [SerializeField, Required]
        private Light2D lightSource;

        [SerializeField, Required]
        private Transform lightSourceTransform;

        private StateMachine _stateMachine;
        private StateMachine GetStateMachine => _stateMachine ??= new(owner: this);

        public TileInfo TileInfo { get; private set; }
        private bool _visualsSet;

        public Axial position;
        
        private readonly Dictionary<Direction, Cell> _neighbors = new(capacity: 6);
        public IReadOnlyDictionary<Direction, Cell> Neighbors => _neighbors;

        private bool _isObstacle;
        private bool _visible;
        private bool Invisible => _visible == false;

        public Option<(ILocalMapEvent mapEvent, float multiplier)> AssignedEvent { get; private set; }
        public bool HasEvent => AssignedEvent.IsSome;
        
        private void SetSize(float value)
        {
            size = value;
            rectTransform.localScale = Vector3.one * size;
        }

        public bool ForcedObstacle { get; private set; }
        public void SetForcedObstacle(bool value) => ForcedObstacle = value;

        public bool IsObstacle
        {
            get => _isObstacle || ForcedObstacle;
            private set => _isObstacle = value;
        }

        public bool TrySetObstacle(bool value)
        {
            if (ForcedObstacle)
                return false;

            IsObstacle = value;
            return true;
        }

        public bool AlreadyExplored { get; private set; }

        public void SetAlreadyExplored(bool value)
        {
            AlreadyExplored = value;
            lightSource.intensity = value ? exploredIntensity : normalIntensity;
            GetStateMachine.CheckState();
        }

        public void Initialize() => _stateMachine ??= new(owner: this);

        public bool IsNeighbor(Cell cell) => _neighbors.ContainsValue(cell);
        
        public bool SetEvent(ILocalMapEvent mapEvent, float multiplier, bool overrideExisting)
        {
            if (mapEvent == null)
            {
                SetAboveIcon(IconType.None);
                AssignedEvent = Option<(ILocalMapEvent, float)>.None;
                return false;
            }
            
            if (AssignedEvent.IsSome && overrideExisting == false)
                return false;

            SetAboveIcon(mapEvent.GetIconType(multiplier));
            AssignedEvent = Option<(ILocalMapEvent, float)>.Some((mapEvent, multiplier));
            return true;
        }

        private void SetAboveIcon(IconType iconType)
        {
            if (iconType == IconType.None)
            {
                eventIcon.gameObject.SetActive(false);
                return;
            }
            
            eventIcon.gameObject.SetActive(true);
            if (LocalMapManager.AssertInstance(out LocalMapManager localMapManager))
                eventIcon.sprite = localMapManager.GetIcon(iconType);
        }

        public void SetPosition(Axial pos)
        {
            position = pos;
            RefreshPositionAndSize(origin: Vector3.zero);
        }

        public void SetVisuals(TileInfo info, Sprite sprite, bool overrideCurrent = false)
        {
            if (!overrideCurrent && _visualsSet && Random.value < 0.5f)
                return;
            
            mainImage.sprite = sprite;
            TileInfo = info;
            _visualsSet = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsObstacle || Invisible)
                return;
            
            mouseOverImage.gameObject.SetActive(true);
            mouseOverImage.sprite = AlreadyExplored ? mouseOverSprite : mouseOverWalkableSprite;
            if (LocalMapManager.AssertInstance(out LocalMapManager localMapManager))
                localMapManager.CellPointerEnter(cell: this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsObstacle || Invisible)
                return;
            
            mouseOverImage.gameObject.SetActive(false);
            if (LocalMapManager.AssertInstance(out LocalMapManager localMapManager))
                localMapManager.CellPointerExit(cell: this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_visible && LocalMapManager.AssertInstance(out LocalMapManager localMapManager))
                localMapManager.CellPointerClick(cell: this);
        }

        public void RefreshPositionAndSize(Vector3 origin)
        {
            transform.position = origin + position.ToWorldCoordinates(Size);
            SetSize(Size);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            GetStateMachine.CheckState();
        }

        public void CheckState() => GetStateMachine.CheckState();

        public void LoadData(Record record)
        {
            Option<TileInfo> tileInfo = TileInfoDatabase.GetTileInfo(record.TileInfoKey);
            if (tileInfo.IsSome)
            {
                Option<Sprite> spriteOption = tileInfo.Value.GetSpriteByIndex(record.SpriteIndex);
                if (spriteOption.IsSome)
                {
                    SetVisuals(tileInfo.Value, spriteOption.Value, overrideCurrent: true);
                }
                else
                {
                    Debug.LogWarning($"TileInfo {tileInfo.Value.Key} has no sprite at index {record.SpriteIndex.ToString()}, using random one instead.");
                    SetVisuals(tileInfo.Value, tileInfo.Value.GetRandomSprite(), overrideCurrent: true);
                }
            }
            else
            {
                Debug.LogWarning($"Missing tile info for key {record.TileInfoKey}", this);
            }

            Axial pos = new(record.Q, record.R);
            SetPosition(pos);
            _isObstacle = record.IsObstacle;
            ForcedObstacle = record.ForcedObstacle;
            SetAlreadyExplored(record.WasExplored);

            if (record.HasEvent == false)
                return;
            
            Option<ILocalMapEvent> localMapEvent = MapEventDatabase.GetEvent(record.EventData.Key);
            if (localMapEvent.IsNone)
            {
                string message = $"Map event {record.EventData.Key} not found in database.\n";
                Debug.LogWarning(message, this);
                return;
            }
                
            SetEvent(localMapEvent.Value, record.EventData.Multiplier, overrideExisting: true);
        }

        public Record GenerateRecord() => Record.FromCell(cell: this);
        
        [Button] private void RefreshSize() => RefreshPositionAndSize(origin: Vector3.zero);
        [Button] private void DebugPosition() => Debug.Log(transform.position.ToString());
        [Button] public void ClearEvent() => SetEvent(null, multiplier: 0f, overrideExisting: true);
        
        public record Record(CleanString TileInfoKey, int SpriteIndex, int Q, int R, bool ForcedObstacle, bool IsObstacle, bool WasExplored, bool HasEvent, EventRecord EventData)
        {
            public static Record FromCell(Cell cell)
            {
                bool hasEvent = cell.AssignedEvent.TrySome(out (ILocalMapEvent mapEvent, float multiplier) assignedEvent);
                EventRecord eventData = hasEvent ? new EventRecord(assignedEvent.mapEvent.Key, assignedEvent.multiplier) : null;
                
                return new Record(cell.TileInfo.Key, cell.TileInfo.GetSpriteIndex(cell.mainImage.sprite), cell.position.q, cell.position.r, cell.ForcedObstacle, cell._isObstacle, cell.AlreadyExplored, hasEvent, eventData);
            }

            public bool IsDataValid(StringBuilder errors)
            {
                if (TileInfoKey.IsNullOrEmpty())
                {
                    errors.AppendLine("Invalid ", nameof(Cell.Record), " data. ", nameof(TileInfoKey), " is null or empty.");
                    return false;
                }

                if (TileInfoDatabase.GetTileInfo(TileInfoKey).IsNone)
                {
                    errors.AppendLine("Invalid ", nameof(Cell.Record), " data. ", nameof(TileInfoKey), " key: ", TileInfoKey.ToString(), " was not found in database.");
                    return false;
                }

                if (HasEvent == false)
                    return true;

                if (EventData == null)
                {
                    errors.AppendLine("Invalid ", nameof(Cell.Record), " data. ", nameof(HasEvent), " is true but ", nameof(EventData), " is null.");
                    return false;
                }

                if (EventData.Key.IsNullOrEmpty())
                {
                    errors.AppendLine("Invalid ", nameof(Cell.Record), " data. ", nameof(HasEvent), " is true but ", nameof(EventData), "'s key is null or empty.");
                    return false;
                }

                if (MapEventDatabase.GetEvent(EventData.Key).IsNone)
                {
                    errors.AppendLine("Invalid ", nameof(Cell.Record), " data. ", nameof(EventData), " key: ", EventData.Key.ToString(), " was not found in database.");
                    return false;
                }
                
                return true;
            }
        }
        
        private enum State
        {
            Hidden,
            Visible,
        }
        
        private class StateMachine
        {
            private readonly Cell _owner;
            private State _current;
            private Tween _tween;
            
            public StateMachine(Cell owner)
            {
                _owner = owner;
                _current = EvaluateState();

                owner.lightSourceTransform.localScale = _current switch
                {
                    State.Hidden  => Vector3.zero,
                    State.Visible => owner.activeLightSourceScale,
                    _             => throw new ArgumentOutOfRangeException()
                };
            }

            private State EvaluateState()
            {
                if (_owner.Invisible || _owner.IsObstacle)
                    return State.Hidden;

                return State.Visible;
            }

            public void CheckState()
            {
                State newState = EvaluateState();
                if (newState == _current)
                    return;

                OnExit(_current);
                OnEnter(newState);
                _current = newState;
            }

            private void OnExit(State state)
            {
                _tween.CompleteIfActive();
                switch (state)
                {
                    case State.Hidden:  _tween = _owner.lightSourceTransform.DOScale(_owner.activeLightSourceScale, duration: 1f); break;
                    case State.Visible: break;
                    default:            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }

            private void OnEnter(State state)
            {
                switch (state)
                {
                    case State.Hidden:  _tween = _owner.lightSourceTransform.DOScale(Vector3.zero, duration: 1f); break;
                    case State.Visible: _owner.lightSourceTransform.DOScale(_owner.activeLightSourceScale, duration: 1f); break;
                    default:            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        public void FindNeighbors(IReadOnlyDictionary<Axial, Cell> map)
        {
            foreach ((Direction direction, Axial axial) in position.GetClosestNeighbors())
                if (map.TryGetValue(axial, out Cell neighbor))
                    _neighbors[direction] = neighbor;
        }

        public bool Equals(Cell other) => other == this;
    }
}