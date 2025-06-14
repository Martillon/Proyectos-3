using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TMG_EditorTools
{

    public class CustomDebugLogColor : MonoBehaviour
    {
        private void Awake()
        {
            // Example usage
            string msg1 = "test log";
            string test1 = DebugC.FormatMessage(msg1, Color.green);
            Debug.Log(test1);


            string msg2 = "test warning";
            string test2 = DebugC.FormatMessage(msg2, Color.yellow, 20, false, true);
            Debug.Log(test2);


            string msg3 = "test error";
            string test3 = DebugC.FormatMessage(msg3, Color.red, 18, true, false);
            Debug.Log(test3);

        }
    }

}