using System;
using System.Text;
using Core.Audio.Scripts;
using Core.Combat.Scripts.WinningCondition;
using Core.Game_Manager.Scripts;
using Core.Visual_Novel.Scripts;
using UnityEngine;
using Utils.Patterns;
using Utils.Extensions;

namespace Core.Combat.Scripts
{
    public record CombatTracker(CombatTracker.FinishRecord OnFinish, bool IsDone = false, bool ValidEnd = false, CombatStatus Result = CombatStatus.LeftSideWon)
    {
        public bool IsDone { get; private set; } = IsDone;
        public bool ValidEnd { get; private set; } = ValidEnd;
        public bool InvalidEnd => ValidEnd == false;
        public CombatStatus Result { get; private set; } = Result;

        public void SetDone(CombatStatus status, bool valid)
        {
            ValidEnd = valid;
            Result = status;
            IsDone = true;
            
            if (ValidEnd)
                OnFinish.NotifyFinished(state: this);
            else
                Debug.LogWarning("Combat ended with InvalidEnd, OnFinish will not be called.");
        }
        
        public void AboutToFinish(CombatStatus status)
        {
            if (IsDone)
                throw new InvalidOperationException("Combat is already done.");
            
            OnFinish.AboutToFinish(status);
        }

        public abstract record FinishRecord : IDeepCloneable<FinishRecord>
        {
            /// <summary> Usually called at the experience screen </summary>
            public abstract void AboutToFinish(CombatStatus result);
            
            public abstract void NotifyFinished(CombatTracker state);
            public abstract FinishRecord DeepClone();
            public abstract bool IsDataValid(StringBuilder errors);
        }

        public record StandardLocalMap : FinishRecord
        {
            public override void AboutToFinish(CombatStatus result)
            {
                if (result is CombatStatus.LeftSideWon && MusicManager.AssertInstance(out MusicManager musicManager))
                    musicManager.NotifyEvent(MusicEvent.Exploration);
            }

            public override void NotifyFinished(CombatTracker state)
            {
                if (GameManager.AssertInstance(out GameManager gameManager) == false)
                    return;
                
                switch(state.Result)
                {
                    case CombatStatus.LeftSideWon:  gameManager.PlayerWonCombatOnLocalMap(); break;
                    case CombatStatus.RightSideWon: gameManager.PlayerLostCombatOnLocalMap(); break;
                    default:                        throw new ArgumentOutOfRangeException(nameof(state.Result), message: $"Unhandled combat status: {state.Result}");
                }
            }

            public override FinishRecord DeepClone() => new StandardLocalMap();
            public override bool IsDataValid(StringBuilder errors) => true;
        }

        public record PlayScene(string OnWin, string OnLoss) : FinishRecord
        {
            public override void AboutToFinish(CombatStatus result)
            {
                if (result is CombatStatus.LeftSideWon && MusicManager.AssertInstance(out MusicManager musicManager))
                    musicManager.NotifyEvent(MusicEvent.Exploration);
            }

            public override void NotifyFinished(CombatTracker state)
            {
                if (GameManager.AssertInstance(out GameManager gameManager))
                    gameManager.CombatToVisualNovel(state.Result == CombatStatus.LeftSideWon ? OnWin : OnLoss);
            }

            public override FinishRecord DeepClone() => new PlayScene(OnWin, OnLoss);

            public override bool IsDataValid(StringBuilder errors)
            {
                if (YarnDatabase.SceneExists(OnWin) == false)
                {
                    errors.AppendLine("Invalid ", nameof(PlayScene), " OnWin scene: ", OnWin, " does not exist in database.");
                    return false;
                }
                
                if (YarnDatabase.SceneExists(OnLoss) == false)
                {
                    errors.AppendLine("Invalid ", nameof(PlayScene), " OnLoss scene: ", OnLoss, " does not exist in database.");
                    return false;
                }
                
                return true;
            }
        }
    }
}