using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// メインツールタブ
/// </summary>
public class FbxImporterTab : EditorWindow
{
    private static string[] _metaFilePaths;
    private static Vector2 scrollPosition = Vector2.zero;

    //MainToolsの変数たち
    private string _defaultDragAndDropZoneMessage = "Drag & Drop here";
    private string _readMetaString;

    public void Layout()
    {
        if (_metaFilePaths == null)
        {
            _metaFilePaths = new string[] { _defaultDragAndDropZoneMessage };
        }

        //TabDesctiprion
        EditorGUILayout.LabelField("アニメーションFBXファイルのインポートツール");

        GUILayout.Space(10);

        //Draw line
        using (new BackgroundColorScope(Color.white))
        {
            GUILayout.Box("", GUILayout.Width(this.position.width * 10), GUILayout.Height(1));
        }

        GUI.backgroundColor = Color.red;

        ///Reselection button
        if (GUILayout.Button("Reset",
            GUILayout.MinWidth(MCCSettings.BUTTON_MIN_SIZE.x), GUILayout.MinHeight(MCCSettings.BUTTON_MIN_SIZE.y / 2),
            GUILayout.MaxWidth(MCCSettings.BUTTON_MAX_SIZE.x), GUILayout.MaxHeight(MCCSettings.BUTTON_MAX_SIZE.y / 2)))
        {
            //Debug.Log("Reset file pathes");
            _metaFilePaths = new string[] { _defaultDragAndDropZoneMessage };
        }

        ////Draw line
        //using (new BackgroundColorScope(Color.gray))
        //{
        //    GUILayout.Box("", GUILayout.Width(this.position.width * 10), GUILayout.Height(1));
        //}

        using (new BackgroundColorScope(Color.white))
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.textColor = Color.black;
            boxStyle.fontSize = 12;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (_metaFilePaths[0] == _defaultDragAndDropZoneMessage)
            {
                GUILayout.Label(_defaultDragAndDropZoneMessage);
            }
            else
            {
                for (int i = 0; i < _metaFilePaths.Length; i++)
                {
                    GUILayout.Label(i + " : " + Path.GetFileName(_metaFilePaths[i])); //show file name
                }
            }

            EditorGUILayout.EndScrollView();

            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    //if (!scrollArea.Contains(evt.mousePosition))
                    //    break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    DragAndDrop.activeControlID = id;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            var p = AssetDatabase.GetAssetPath(draggedObject);
                            //Debug.Log("Select fbx file:" + p);

                            if ((Path.GetExtension(p) == ".fbx"))
                            {
                                if (_metaFilePaths[0] == _defaultDragAndDropZoneMessage)
                                {
                                    _metaFilePaths[0] = p;
                                }
                                else
                                {
                                    if (Array.IndexOf(_metaFilePaths, p) == -1)
                                    {
                                        Array.Resize(ref _metaFilePaths, _metaFilePaths.Length + 1);
                                        _metaFilePaths[_metaFilePaths.Length - 1] = p;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("Dragged file is not fbx file !");
                            }
                        }
                        DragAndDrop.activeControlID = 0;
                    }
                    Event.current.Use();

                    break;
            }
        }

        //Draw line
        using (new BackgroundColorScope(Color.gray))
        {
            GUILayout.Box("", GUILayout.Width(this.position.width * 10), GUILayout.Height(1));
        }

        GUILayout.Space(20);

        ///Apply button
        EditorGUILayout.BeginHorizontal();
        {
            //GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("選択されたfbxをセットアップする");

                using (new BackgroundColorScope(Color.yellow))
                {
                    if (GUILayout.Button("Apply",
                            GUILayout.MinWidth(MCCSettings.BUTTON_MIN_SIZE.x), GUILayout.MinHeight(MCCSettings.BUTTON_MIN_SIZE.y),
                            GUILayout.MaxWidth(MCCSettings.BUTTON_MAX_SIZE.x), GUILayout.MaxHeight(MCCSettings.BUTTON_MAX_SIZE.y)))
                    {
                        if (_metaFilePaths[0] != _defaultDragAndDropZoneMessage)
                        {
                            SetuupFbx(_metaFilePaths);
                        }
                        else
                        {
                            Debug.LogError("Not selected fbx files");
                        }
                    }
                }
            }
            //GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        //Draw line
        using (new BackgroundColorScope(Color.gray))
        {
            GUILayout.Box("", GUILayout.Width(this.position.width * 10), GUILayout.Height(1));
        }

        GUILayout.Space(10);

        using (new BackgroundColorScope(Color.white))
        {
            //FbxImporterで適用後にanimationClipを取り出すかどうか
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("FbxImportと同時にClipを取り出す");

                bool isDuplicateAnimClip = PlayerPrefs.GetInt("nmxi.movieCreateCompanion.isDuplicateAnimClipWhenFbxImport", 1) == 1 ? true : false;
                isDuplicateAnimClip = EditorGUILayout.Toggle(isDuplicateAnimClip);
                int value = isDuplicateAnimClip ? 1 : 0;
                PlayerPrefs.SetInt("nmxi.movieCreateCompanion.isDuplicateAnimClipWhenFbxImport", value);
                PlayerPrefs.Save();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            //取り出したAnimaitonClipのRootTransform設定をするかどうか
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("AnimClipを取り出した後 最適化");

                bool isAutoAnimClipRootTransformSettings = PlayerPrefs.GetInt("nmxi.movieCreateCompanion.AutoAnimClipRootTransformSettings", 1) == 1 ? true : false;
                isAutoAnimClipRootTransformSettings = EditorGUILayout.Toggle(isAutoAnimClipRootTransformSettings);
                int value = isAutoAnimClipRootTransformSettings ? 1 : 0;
                PlayerPrefs.SetInt("nmxi.movieCreateCompanion.AutoAnimClipRootTransformSettings", value);
                PlayerPrefs.Save();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            //AnimationClipをFbxから取り出した後Fbxを削除するかどうか
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("AnimClipを取り出した後Fbxを削除");

                bool isAutoDeleteFbxAfterDuplicated = PlayerPrefs.GetInt("nmxi.movieCreateCompanion.AutoDeleteFbxAfterDuplicated", 1) == 1 ? true : false;
                isAutoDeleteFbxAfterDuplicated = EditorGUILayout.Toggle(isAutoDeleteFbxAfterDuplicated);
                int value = isAutoDeleteFbxAfterDuplicated ? 1 : 0;
                PlayerPrefs.SetInt("nmxi.movieCreateCompanion.AutoDeleteFbxAfterDuplicated", value);
                PlayerPrefs.Save();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// FbxImporterのメイン機能部分
    /// </summary>
    /// <param name="fbxFilePath"></param>
    private void SetuupFbx(string[] fbxFilePaths)
    {
        foreach (var fbxFilePath in fbxFilePaths)
        {
            string targetFilePath = fbxFilePath + ".meta";

            FileInfo fi = new FileInfo(targetFilePath);
            bool flag = false;
            using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8))
            {
                while (sr.EndOfStream == false)
                {
                    string line = sr.ReadLine();
                    if (line.IndexOf("  animationType: 2") != -1)
                    {
                        line = "  animationType: 3";
                        flag = true;
                    }
                    _readMetaString += line + "\r\n";
                }
            }

            if (flag)
            {
                File.Delete(targetFilePath);
                File.AppendAllText(targetFilePath, _readMetaString);

                AssetDatabase.Refresh();
            }

            _readMetaString = string.Empty;
            fi = new FileInfo(targetFilePath);

            try
            {
                using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8))
                {
                    while (sr.EndOfStream == false)
                    {
                        string line = sr.ReadLine();
                        if (line.IndexOf("      rotation: {x:") != -1)
                        {
                            line = "      rotation: {x: 0, y: -0, z: -0, w: 1}";
                        }

                        _readMetaString += line + "\r\n";
                    }
                }

                File.Delete(targetFilePath);
                File.AppendAllText(targetFilePath, _readMetaString);
                _readMetaString = string.Empty;
            }
            catch (Exception)
            {
                Debug.LogError("Faild to open meta file");
            }
        }

        AssetDatabase.Refresh();

        Debug.Log("fbx meta file was updated");

        //自動でAnimationClipのRootTransform設定を行う
        if (PlayerPrefs.GetInt("nmxi.movieCreateCompanion.AutoAnimClipRootTransformSettings", 1) == 1 ? true : false)
        {
            foreach (var fbxFilePath in fbxFilePaths)
            {
                var importer = AssetImporter.GetAtPath(fbxFilePath);
                SetAnimationImporterSettings(importer as ModelImporter);
                AssetDatabase.ImportAsset(fbxFilePath);
            }
            Selection.activeObject = null;

            AssetDatabase.Refresh();
        }

        if (PlayerPrefs.GetInt("nmxi.movieCreateCompanion.isDuplicateAnimClipWhenFbxImport", 1) == 1 ? true : false)
        {
            DuplicateAnimClipFromFbx(fbxFilePaths);
        }
    }

    /// <summary>
    /// 自動でAnimationClipのRootTransform設定を行う部分
    /// </summary>
    /// <param name="importer"></param>
    private void SetAnimationImporterSettings(ModelImporter importer)
    {
        var clips = importer.clipAnimations;

        if (clips.Length == 0) clips = importer.defaultClipAnimations;

        foreach (var clip in clips)
        {
            clip.lockRootRotation = true;
            clip.lockRootPositionXZ = true;
            clip.lockRootHeightY = true;
        }

        importer.clipAnimations = clips;
    }

    /// <summary>
    /// 指定されたfbxファイルからAnimationClipを複製する
    /// 現在複数のAnimationClipを含むFbxには対応していない
    /// </summary>
    /// <param name="fbxFilePaths"></param>
    private void DuplicateAnimClipFromFbx(string[] fbxFilePaths)
    {
        //Debug.Log("AnimationClipを複製する");

        foreach (var path in fbxFilePaths)
        {
            // AnimationClipを持つFBXのパス
            string fbxPath = path;

            // AnimationClipの出力先
            string exportPath = string.Empty;

            exportPath = System.IO.Path.GetDirectoryName(path) + "/" + System.IO.Path.GetFileNameWithoutExtension(path) + ".anim";

            //UUIDをユニークなものにするために一旦保存するAnimationClip名
            string tempExportedClip = "Assets/tempClip.anim";

            // AnimationClipの取得
            var animations = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var originalClip = System.Array.Find<Object>(animations, item =>
                  item is AnimationClip && item.name.Contains("__preview__") == false
            );

            Debug.Log(originalClip.name);

            // AnimationClipをコピーして出力(ユニークなuuid)
            var copyClip = Object.Instantiate(originalClip);
            AssetDatabase.CreateAsset(copyClip, tempExportedClip);

            // AnimationClipのコピー（固定化したuuid）
            File.Copy(tempExportedClip, exportPath, true);
            File.Delete(tempExportedClip);

            if(PlayerPrefs.GetInt("nmxi.movieCreateCompanion.AutoDeleteFbxAfterDuplicated", 1) == 1 ? true : false)
            {
                File.Delete(path);
            }
        }

        // AssetDatabaseリフレッシュ
        AssetDatabase.Refresh ();
    }
}