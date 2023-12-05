using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movementprototype : MonoBehaviour
{
    public float speed = 10.0f;
    int vlieg;

    // Update is called once per frame
    void Update()
    {
        float translationz = Input.GetAxis("Vertical") * speed;
        float translationx = Input.GetAxis("Horizontal") * speed;
        float translationy = vlieg * speed;
        translationz *= Time.deltaTime;
        translationx *= Time.deltaTime;
        translationy *= Time.deltaTime;

        transform.Translate(translationx, translationy, translationz);
        if (Input.GetKey(KeyCode.Space))
        {
            vlieg = 1;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            vlieg = -1;
        }
        else
        {
            vlieg = 0;
        }
    }
}
