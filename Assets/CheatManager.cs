using Scripts.Player.Core;
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
         FindAnyObjectByType<PlayerHealthSystem>().HealLife(3);
        }
        if(Input.GetKeyDown(KeyCode.O))
        {
            FindAnyObjectByType<PlayerHealthSystem>().HealArmor(3);
        }
    }
}
