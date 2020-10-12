using UnityEditor;

public class MovieCreateCompanion : EditorWindow
{
    static MovieCreateCompanion MoiveCreateCompanionWindow; //複数タブ表示不可能用

    [MenuItem("MovieCreateCompanion/MainTool")]
    static void Open()
    {
        if(MoiveCreateCompanionWindow == null)
        {
            MoiveCreateCompanionWindow = CreateInstance<MovieCreateCompanion>();
        }

        MoiveCreateCompanionWindow.minSize = MCCSettings.MIN_WINDOW_SIZE;
        MoiveCreateCompanionWindow.maxSize = MCCSettings.MAX_WINDOW_SIZE;

        MoiveCreateCompanionWindow.Show();
    }

    void OnGUI()
    {
        var fbxImporterTab = new FbxImporterTab();
        fbxImporterTab.Layout();
    }
}