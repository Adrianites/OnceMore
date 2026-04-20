using UnityEngine;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class EndGameCommand : MonoBehaviour
{
    [YarnCommand("EndGame")]
    public static void EndGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
