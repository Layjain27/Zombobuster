using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public void RestartGame()
    {

        SceneManager.LoadScene("Zombofun (NIS)");
        Time.timeScale = 1f;
    }

  

    public void QuitToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
 
}