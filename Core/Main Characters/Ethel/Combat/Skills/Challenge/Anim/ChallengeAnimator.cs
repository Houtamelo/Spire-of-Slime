using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Action;
using Core.Game_Manager.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Extensions;
using Utils.Objects;

namespace Core.Main_Characters.Ethel.Combat.Skills.Challenge.Anim
{
    public class ChallengeAnimator : MonoBehaviour
    {
        [SerializeField, Required]
        private ChallengeParticleFx particlePrefab;
        
        [SerializeField, Required]
        private CustomAudioSource[] challengeSounds;
        
        private readonly List<ChallengeParticleFx> _particles = new();

        private void Awake()
        {
            Scene scene = SceneManager.GetSceneByName(SceneRef.Combat);
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                for (int i = 0; i < 4; i++)
                    CreateParticle();
                
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject obj in roots)
            {
                if (obj.name == "Root")
                {
                    for (int i = 0; i < 4; i++)
                    {
                        ChallengeParticleFx particle = CreateParticle();
                        particle.transform.SetParent(obj.transform);
                    }

                    return;
                }
            }

            Debug.LogWarning("Could not find combat scene root to spawn Challenge particles, this should only happen in test scenes");
            for (int i = 0; i < 4; i++)
                CreateParticle();
        }
        
        public void AnimateChallenge(CasterContext casterContext)
        {
            if (challengeSounds.HasElements())
                challengeSounds.GetRandom().Play();
            
            ActionResult[] results = casterContext.Results;
            for (int i = _particles.Count; i < results.Length; i++) 
                CreateParticle();

            int particleIndex = 0;
            for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                ref ActionResult result = ref results[resultIndex];
                if (result.Missed || result.Caster == result.Target ||
                    result.Target.Display.AssertSome(out CharacterDisplay display) == false ||
                    display.GetBounds().TrySome(out Bounds bounds) == false)
                {
                    continue;
                }
                
                ChallengeParticleFx particle = _particles[particleIndex];
                Vector3 worldPosition = bounds.center + new Vector3(0, bounds.extents.y * 0.8f);
                particle.Animate(worldPosition);
                particleIndex++;
            }
            
            for (int i = particleIndex; i < _particles.Count; i++)
                _particles[i].Stop();
        }

        private ChallengeParticleFx CreateParticle()
        {
            ChallengeParticleFx particle = particlePrefab.InstantiateWithFixedLocalScale(transform);
            _particles.Add(particle);
            return particle;
        }
    }
}