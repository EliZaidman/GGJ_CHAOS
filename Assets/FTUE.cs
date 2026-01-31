using System;
using TMPro;
using UnityEngine;

public class FTUE : MonoBehaviour
{
    [SerializeField] TextMeshPro FTUE3DText;
    [SerializeField] GameObject FTUEToast;
    bool PlayFtue = false;
    public static FTUE Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        
    }

    public void ToggleFtue(bool active)
    {
        print(active + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa " + FTUE3DText.text);
        FTUE3DText.gameObject.SetActive(active);
        FTUEToast.gameObject.SetActive(active);
    }
  
}
