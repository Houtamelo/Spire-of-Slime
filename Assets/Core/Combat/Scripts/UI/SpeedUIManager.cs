using Core.Combat.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Combat.Scripts.UI
{
    public class SpeedUIManager : MonoBehaviour
    {
        private const string SpeedPlayerPref = "combat_speed";
        public const float Speed1X = 0.15f, Speed2X = Speed1X * 2f, Speed3X = Speed1X * 3f;

        [SerializeField]
        private Button pauseButton,
                       speed1XButton, 
                       speed2XButton,
                       speed3XButton;
        
        [SerializeField]
        private Image pauseIndicator,
                      speed1XIndicator,
                      speed2XIndicator,
                      speed3XIndicator;   

        [SerializeField]
        private CombatManager combatManager;
        
        private void Start()
        {
            pauseButton.onClick.AddListener(OnPauseButtonClick);
            speed1XButton.onClick.AddListener(OnSpeed1XButtonClick);
            speed2XButton.onClick.AddListener(OnSpeed2XButtonClick);
            speed3XButton.onClick.AddListener(OnSpeed3XButtonClick);
            
            int speed = PlayerPrefs.GetInt(SpeedPlayerPref, defaultValue: 0);
            switch (speed)
            {
                case 0: combatManager.SpeedHandler.SetValue(Speed1X); break;
                case 1: combatManager.SpeedHandler.SetValue(Speed2X); break;
                case 2: combatManager.SpeedHandler.SetValue(Speed3X); break;
            }
            
            combatManager.SpeedHandler.Event += SpeedHandlerChanged;
            SpeedHandlerChanged(combatManager.SpeedHandler.Value);

            combatManager.PauseHandler.Changed += PausedChanged;
            PausedChanged(combatManager.PauseHandler.Value);
        }

        private void OnDestroy()
        {
            if (combatManager == null)
                return;
            
            combatManager.PauseHandler.Changed -= PausedChanged;
            combatManager.SpeedHandler.Event -= SpeedHandlerChanged;
        }

        private void SpeedHandlerChanged(float value)
        {
            if (value >= Speed3X)
            {
                speed1XIndicator.enabled = false;
                speed2XIndicator.enabled = false;
                speed3XIndicator.enabled = true;
                PlayerPrefs.SetInt(SpeedPlayerPref, 2);
            }
            else if (value >= Speed2X)
            {
                speed1XIndicator.enabled = false;
                speed2XIndicator.enabled = true;
                speed3XIndicator.enabled = false;
                PlayerPrefs.SetInt(SpeedPlayerPref, 1);
            }
            else
            {
                speed1XIndicator.enabled = true;
                speed2XIndicator.enabled = false;
                speed3XIndicator.enabled = false;
                PlayerPrefs.SetInt(SpeedPlayerPref, 0);
            }
        }
        
        private void PausedChanged(bool value) => pauseIndicator.enabled = value;

        private void OnSpeed1XButtonClick() => combatManager.SpeedHandler.SetValue(Speed1X);
        private void OnSpeed2XButtonClick() => combatManager.SpeedHandler.SetValue(Speed2X);
        private void OnSpeed3XButtonClick() => combatManager.SpeedHandler.SetValue(Speed3X);
        private void OnPauseButtonClick() => combatManager.TogglePause();
    }
}