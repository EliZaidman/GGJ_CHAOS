using System;
using UnityEngine;

public class PlayHitSound : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
          //  SoundManager.PlayAt(SoundId.HitWhooh, other.transform.position);
        print("ben is a cool guy overall");
     //   SoundManager.PlayAt(SoundId., hitPos); 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)){
            print("f11");
            SoundManager.PlayAt(SoundId.HitWhooh, transform.position);}
    }
}
