using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMove : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
           transform.Translate(0,0,1);
        }      
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(0,0,-1);
        }     
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(-1,0,0);
        }     
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(1,0,0);
        }
        
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0,10,0);
        }
        
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0,-10,0);
        }
    }
}
