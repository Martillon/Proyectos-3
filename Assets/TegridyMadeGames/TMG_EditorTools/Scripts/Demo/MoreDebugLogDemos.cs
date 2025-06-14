using System.Collections;
using System.Collections.Generic;
using TMG_EditorTools;
using UnityEngine;

namespace TMG_EditorTools
{

    public class MoreDebugLogDemos : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            string msg1 = "Documents Example";
            string test1 = DebugC.FormatMessage(msg1, Color.blue, 13, true, true);
            Debug.Log(test1);
            // Loop 5 times
            for (int i = 0; i < 10; i++)
            {
                string msg2 = "test spam message";
                string test2 = DebugC.FormatMessage(msg2, Color.black, 12, true, true);
                Debug.Log(test2);
                // Add your code here to execute for each iteration
            }
            StartCoroutine(ExampleCoroutine());
        }
        IEnumerator ExampleCoroutine()
        {
            yield return new WaitForSeconds(2);

            string msg3 = "Delay test";
            string test3 = DebugC.FormatMessage(msg3, Color.grey, 13, true, true);
            Debug.Log(test3);
        }
    }

}