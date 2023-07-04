using System;

namespace Core.Game_Manager.Scripts
{
    public static class SceneExtensions
    {
        public static SceneRef ToName(this SceneEnum sceneEnum)
        {
            return sceneEnum switch
            {
                SceneEnum.MainMenu       => SceneRef.MainMenu,
                SceneEnum.LocalMap       => SceneRef.LocalMap,
                SceneEnum.WorldMap       => SceneRef.WorldMap,
                SceneEnum.Combat         => SceneRef.Combat,
                SceneEnum.PauseMenu      => SceneRef.PauseMenu,
                SceneEnum.VisualNovel    => SceneRef.VisualNovel,
                SceneEnum.StartScreen    => SceneRef.StartScreen,
                SceneEnum.GameManager    => SceneRef.GameManager,
                SceneEnum.Audio          => SceneRef.Audio,
                SceneEnum.ScreenButtons  => SceneRef.ScreenButtons,
                SceneEnum.CharacterPanel => SceneRef.CharacterPanel,
                _                        => throw new ArgumentOutOfRangeException(nameof(sceneEnum), sceneEnum, null)
            };
        }
        
        public static int GetCameraPriority(this SceneEnum sceneEnum)
        {
            return sceneEnum switch
            {
                SceneEnum.MainMenu       => 16,
                SceneEnum.LocalMap       => 9,
                SceneEnum.WorldMap       => 8,
                SceneEnum.Combat         => 10,
                SceneEnum.PauseMenu      => 18,
                SceneEnum.VisualNovel    => 12,
                SceneEnum.StartScreen    => 19,
                SceneEnum.GameManager    => 20,
                SceneEnum.Audio          => -1,
                SceneEnum.ScreenButtons  => 13,
                SceneEnum.CharacterPanel => 14,
                _                        => throw new ArgumentOutOfRangeException(nameof(sceneEnum), sceneEnum, null)
            };
        }
    }
}