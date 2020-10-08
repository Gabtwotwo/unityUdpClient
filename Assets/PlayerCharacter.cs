using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Drawing;

public class PlayerCharacter : MonoBehaviour
{
 
    public NetworkMan.Player playerRef;
    public Vector3 direction;

    void Update()
    {
       direction.x = Input.GetAxis("Horizontal");
       direction.y = Input.GetAxis("Vertical");

       transform.position += direction;

        GetComponent<Renderer>().material.color = new UnityEngine.Color(playerRef.color.R, playerRef.color.G, playerRef.color.B);
    }


}