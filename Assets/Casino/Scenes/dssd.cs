using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class dssd : MonoBehaviour
{
    [SerializeField] private bool isVisible= true;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isVisible)
            {
                Screen.lockCursor = false;
                //Cursor.visible = false;
                //Cursor.lockState = CursorLockMode.Locked;
                isVisible = false;
            }
            else
            {
                Screen.lockCursor = true;

                //Cursor.visible = true;
                //Cursor.lockState = CursorLockMode.None;
                isVisible = true;
            }
        }
    }

void Awake()
    {
        Screen.lockCursor = true;

        //Cursor.visible = false;
}

}