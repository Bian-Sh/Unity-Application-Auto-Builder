using UnityEditor;
using UnityEngine;

public class ProgressBarWindow : EditorWindow
{
    private static ProgressBarWindow window;
    private static float progress;
    private static string info;
    private static new string title;
    public static void ShowProgressBar(string title, string info, float progress)
    {
        if (window == null)
        {
            window = CreateInstance<ProgressBarWindow>();
            window.titleContent = new GUIContent(title);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 100);
            window.minSize = window.maxSize = new Vector2(600, 80);
            window.ShowModalUtility(); // Show as a modal utility window, which is always on top
        }

        ProgressBarWindow.info = info;
        ProgressBarWindow.title = title;
        ProgressBarWindow.progress = progress;
        window?.Repaint();
    }

    public static void ClearProgressBar()
    {
        window = GetWindow<ProgressBarWindow>();
        if (window != null)
        {
            window.Close();
            window = null;
        }
    }

    private void OnGUI()
    {
        window.titleContent.text = title;
        EditorGUI.ProgressBar(new Rect(3, 20, position.width - 6, 20), progress, $"{progress * 100:F1}%");
        EditorGUI.LabelField(new Rect(3, 50, position.width - 6, 20), info);
    }
}
