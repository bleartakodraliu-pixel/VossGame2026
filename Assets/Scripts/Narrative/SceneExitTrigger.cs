using UnityEngine;
using Voss.Core;
using Voss.Player;

namespace Voss.Narrative
{
    /// <summary>Loads the next scene when the player crosses it, provided the required flag is set.</summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitTrigger : MonoBehaviour
    {
        [SerializeField] private string targetScene = SceneNames.Theatre;
        [SerializeField] private GameFlag requiredFlag = GameFlag.FinalExitUnlocked;

        private bool fired;

        public void Configure(string scene, GameFlag flag)
        {
            targetScene = scene;
            requiredFlag = flag;
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!GameFlags.Check(gm.State, requiredFlag)) return;
            fired = true;
            gm.Transitions.LoadScene(targetScene, 0.8f);
        }
    }
}
