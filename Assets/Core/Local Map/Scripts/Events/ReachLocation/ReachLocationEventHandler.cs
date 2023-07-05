using System.Collections;
using Core.Game_Manager.Scripts;
using Core.Utils.Async;
using Core.World_Map.Scripts;

namespace Core.Local_Map.Scripts.Events.ReachLocation
{
    public static class ReachLocationEventHandler
    {
        public static CoroutineWrapper HandleReachLocation(LocationEnum location)
        {
            return new CoroutineWrapper(SetLocationAndContinue(location), nameof(SetLocationAndContinue), context: null, autoStart: true);
        }
        
        private static IEnumerator SetLocationAndContinue(LocationEnum location)
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
                yield return gameManager.LocalMapToWorldMap(location);
        }
    }
}