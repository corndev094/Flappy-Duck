using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShortCut : MonoBehaviour{

    void Update()
    {
        if (EditorApplication.isPlaying && Keyboard.current.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    [Shortcut("Custom/ToggleActive", KeyCode.D)]
    static void ToggleActive()
    {
        foreach (var obj in Selection.gameObjects)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }

    [Shortcut("Custom/ResetTransform", KeyCode.E, ShortcutModifiers.Control)]
    static void ResetTransform()
    {
        if (Selection.activeGameObject == null) return;
        Selection.activeGameObject.transform.position = Vector3.zero;
        Selection.activeGameObject.transform.rotation = Quaternion.identity;
        Selection.activeGameObject.transform.localScale = Vector3.one;
    }
}