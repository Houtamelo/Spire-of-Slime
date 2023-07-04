using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Managers;
using Core.Local_Map.Scripts.Events;
using Core.Main_Characters.Ethel.Combat;
using Core.Visual_Novel.Scripts;
using Data.Main_Characters.Nema;
using Utils.Math;

namespace Save_Management
{
    public static class SaveUtils
    {
        private static readonly HashSet<CleanString> TrueBooleansOnNewGame 
            = new()
              {
                  EthelSkills.Clash,
                  EthelSkills.Sever,
                  EthelSkills.Jolt,
                  EthelSkills.Safeguard,
                  NemaSkills.Calm.key,
                  NemaSkills.Gawky.key,
              };

        public static IReadOnlyCollection<CleanString> GetTrueBooleansOnNewGame() => TrueBooleansOnNewGame;

        public static bool CanSaveCurrentToDisk()
        {
            if (Save.Current == null)
                return false;
            
            if (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning)
                return false;
            
            if (LocalMapEventHandler.Instance.TrySome(out LocalMapEventHandler localMapEventHandler) && !localMapEventHandler.CurrentEventAllowsSaving)
                return false;
            
            if (CombatManager.Instance.TrySome(out CombatManager combatManager) && !combatManager.Running) // means combat hasn't been setup yet
                return false;

            return true;
        }
    }
}