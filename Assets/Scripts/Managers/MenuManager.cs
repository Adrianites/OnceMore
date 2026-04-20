using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void ClickStart()
    {
        SceneManager.LoadScene("Scene");
    }

        public void ClickQuit()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #elif UNITY_WEBGL
                Debug.Log("Game Over! Thanks for playing.");
            #elif UNITY_STANDALONE
                Application.Quit();
            #endif
        }
}
