using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Sample
{
    public class PlayTimelineView : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirectorPrefab;
        [SerializeField] private Button button;

        public void InstantiateAndPlay()
        {
            StartCoroutine(PlayCoroutine());
        }

        private IEnumerator PlayCoroutine()
        {
            button.gameObject.SetActive(false);
            var directionInstance = Instantiate(playableDirectorPrefab);

            yield return new WaitForSeconds((float)directionInstance.duration);

            Destroy(directionInstance.gameObject);
            button.gameObject.SetActive(true);
        }
    }
}
