using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectClickHandler : MonoBehaviour
{
    public Camera myCamera;

    // In the Unity Inspector, you can set the name of the scene you want to load.
    public string sceneNameToLoad;

    void Update()
    {
        // Check if the left mouse button was clicked
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray myRay = myCamera.ScreenPointToRay(mousePosition);
            RaycastHit raycastHit;

            // Perform the raycast
            if (Physics.Raycast(myRay, out raycastHit))
            {
                // Check the name of the object that was hit
                switch (raycastHit.transform.name)
                {
                    case "Jugar-Box":
                        // If the "Jugar-Box" is clicked, load the specified scene
                        Debug.Log("Jugar-Box clicked - Loading scene: " + sceneNameToLoad);
                        SceneManager.LoadScene(sceneNameToLoad);
                        break;

                    case "Salir-Box":
                        // If the "Salir-Box" is clicked, exit the game
                        Debug.Log("Salir-Box clicked - Quitting application");
                        Application.Quit();

                        // The following line is for stopping play mode in the Unity Editor,
                        // as Application.Quit() does not work in the editor.
                        #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                        #endif
                        break;
                }
            }
        }
    }
}