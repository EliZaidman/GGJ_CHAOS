using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuSelectionScreen : MonoBehaviour
{
    public GameObject[] enable;
    public GameObject[] disable;
    int index = 0;
    public float rotationSpeed = 3;
    //Dictionary<PlayerInput, int>

    public void PlayerJoinedHandler(PlayerInput input)
    {
        Debug.Log("Player " + index + " joined");
        enable[index].SetActive(true);
        disable[index].SetActive(false);
        index++;
        InputAction action = input.currentActionMap.actions.FirstOrDefault(ia => ia.name == "Move");
        StartCoroutine(ReadInputRoutine(action, enable[index - 1]));

        if (index == 4)
        {
            SceneManager.LoadScene(1);
        }
    }

    private IEnumerator ReadInputRoutine(InputAction action, GameObject gameObject)
    {
        float rotation = 0;
        float vel = 0;
        while (true)
        {
            var v2 = action.ReadValue<Vector2>();
            gameObject.transform.Rotate(0, -rotation * Time.deltaTime, 0);
            rotation += v2.x * rotationSpeed;
            rotation = UnityEngine.Mathf.SmoothDamp(rotation, 0, ref vel, 0.75f);
            if (rotation > 720) rotation = 720;
            if (rotation <= -720) rotation = -720;
            yield return null;
        }
    }

    public void PlayerLeftHandler(PlayerInput input)
    {
        Debug.Log("Player " + index + " left");
        enable[index].SetActive(true);
        disable[index].SetActive(false);
        index--;
    }
}
