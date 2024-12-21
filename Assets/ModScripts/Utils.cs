using System;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace KoiKoi
{
    public static class Utils
    {
        public static IEnumerator CallbackCoroutine(IEnumerator routine, Action callback)
        {
            yield return routine;
            callback();
        }

        public static IEnumerator SynchronizedCoroutines(MonoBehaviour obj, params IEnumerator[] coroutines)
        {
            var finished = new bool[coroutines.Length];
            for (int i = 0; i < coroutines.Length; i++)
            {
                int x = i;
                obj.StartCoroutine(CallbackCoroutine(coroutines[i], () => finished[x] = true));
            }
            yield return new WaitUntil(() => finished.All(x => x));
        }
    }
}