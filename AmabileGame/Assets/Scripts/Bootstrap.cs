using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string firstLevelName = "Nivel1";

    void Start()
    {
        // Cargar Nivel1 junto a Core
        SceneManager.LoadScene(firstLevelName, LoadSceneMode.Additive);
    }
}