using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

public class SightlinesWindow : EditorWindow
{
    public MeshRenderer[] allMeshes = null;
    public Bounds bounds;

    private bool _foldedOut = false;
    private bool _activationFoldedOut = false;
    private string _cachedLayerName = string.Empty;

    public static readonly string SIGHTLINEIDX_ID = "SightlinesIdx";
    public static readonly string SIGHTLINESPREBAKED_ID = "SightlinesPreBaked";

    [MenuItem("NoClip/Clear All Oops")] 
    public static void ClearAll()
    {
        if (EditorUtility.DisplayDialog("Are you sure?", "About to delete all the sightlines, are you sure?", "Yeap", "Ah shit no"))
        {
            SightlinesLayer[] layers = FindObjectsOfType<SightlinesLayer>();
            for (int i = layers.Length - 1; i >= 0; --i)
            {
                DestroyImmediate(layers[i]);
            }
            Debug.Log("Destroyed " + layers.Length + " layers");
        }
    }

    [MenuItem("NoClip/Prebake Sightlines")]
    public static void PrebakeSightlines()
    {
        var manager = Camera.main.GetComponent<SightlinesManager>();

    }

    public static SightlinesWindow Init(MeshRenderer[] _filters, Bounds _bounds)
    {
        SightlinesWindow window = EditorWindow.GetWindow<SightlinesWindow>();
        window.Show();

        window.allMeshes = _filters;
        window.bounds = _bounds;
        //window._cachedLayerName = string.Empty;

        return window;
    }

    private void OnGUI()
    {
        SightlinesManager manager = Camera.main.GetComponent<SightlinesManager>();
        if (manager == null)
        {
            EditorGUILayout.LabelField("Camera is missing Sightlines Manager");
            return;
        }
        bool usingSightlines = ToolManager.activeToolType == typeof(SightlinesTool);
        if (!usingSightlines)
        {
            allMeshes = null;
            bounds.extents = Vector3.zero;
        } 

        string chunksInfo = string.Format("Number of meshes: {0}", (allMeshes != null ? allMeshes.Length.ToString() : "NULL"));
        EditorGUILayout.LabelField(chunksInfo, new GUIStyle("BoldLabel"));
        GUIStyle childrenStyle = new GUIStyle("miniLabel");
        _foldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(_foldedOut, "Meshes");
        if (_foldedOut && allMeshes != null)
        {
            for (int i = 0; i < allMeshes.Length; ++i)
            {
                GUILayout.Label(allMeshes[i].name, childrenStyle);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bounds: " + (allMeshes != null ? bounds.ToString() : "NULL"), new GUIStyle("BoldLabel"));

        EditorGUILayout.Space(10);
        List<string> displayedLayers = manager.sightlineKeys.ToList();
        displayedLayers.Insert(0, "New Layer...");

        int idx = EditorPrefs.GetInt(SIGHTLINEIDX_ID);
        if (idx >= displayedLayers.Count) idx = 0;
        idx = EditorGUILayout.Popup("Current Layer:", idx, displayedLayers.ToArray(), new GUIStyle("miniPopup"));
        if (idx == 0)
        {
            _cachedLayerName = EditorGUILayout.TextField(_cachedLayerName);
        }
        else
        {
            _cachedLayerName = displayedLayers[idx];

            #region Select Button
            if (GUILayout.Button("Select"))
            {
                List<int> instanceIDs = new List<int>();
                SightlinesLayer[] layers = FindObjectsOfType<SightlinesLayer>();
                for (int i = 0; i < layers.Length; ++i)
                {
                    if (layers[i].layerNames.Contains(_cachedLayerName))
                    {
                        instanceIDs.Add(layers[i].gameObject.GetInstanceID());
                    }
                }
                Selection.instanceIDs = instanceIDs.ToArray();
                ShowNotification(new GUIContent(instanceIDs.Count.ToString() + " meshes selected"));
                ToolManager.SetActiveTool<SightlinesTool>();
            }
            #endregion

            #region Activation Layers
            var entry = manager.EDITOR_GetSightLine(_cachedLayerName);

            _activationFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(_activationFoldedOut, "Activation Layers");
            if (_activationFoldedOut)
            {
                string newLayerName = "";
                newLayerName = EditorGUILayout.DelayedTextField(newLayerName);
                if (!string.IsNullOrEmpty(newLayerName))
                {
                    entry.activations.Add(newLayerName);
                }
                GUIStyle labelStyle = new GUIStyle("MiniLabel");
                GUIStyle buttonStyle = new GUIStyle("miniButtonRight");
                string markedForRemoval = string.Empty;
                for (int i = 0; i < entry.activations.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(entry.activations[i], labelStyle);
                        if (GUILayout.Button("-", buttonStyle))
                        {
                            markedForRemoval = entry.activations[i];
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (!string.IsNullOrEmpty(markedForRemoval))
                {
                    entry.activations.Remove(markedForRemoval);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion
        }
        EditorPrefs.SetInt(SIGHTLINEIDX_ID, idx);
        --idx; // have it actually map to the order in manager, with -1 == exception case

        if (usingSightlines)
        {
            EditorGUILayout.BeginHorizontal();
            #region Commit Button
            bool commit = GUILayout.Button("Commit");
            bool layerOnly = GUILayout.Button("Add to Layer");
            if (commit || layerOnly)
            {
                if (string.IsNullOrEmpty(_cachedLayerName))
                {
                    ShowNotification(new GUIContent("Unable to commit empty string as new layer"));
                    return;
                }
                if (idx <= -1)
                {
                    if (manager.sightlineKeys.Contains(_cachedLayerName))
                    {
                        Debug.LogError("Couldn't add Sightline Layer [" + _cachedLayerName + "], it already exists.");
                        return;
                    }
                    List<string> activations = new List<string>();
                    activations.Add(_cachedLayerName); // activate self, yeah?
                    manager.EDITOR_AddSightLine(_cachedLayerName, new SightlinesManager.SightlineDefines() { bounds = bounds, activations = activations });
                }
                else
                {
                    SightlinesManager.SightlineDefines entry = manager.EDITOR_GetSightLine(_cachedLayerName);
                    if (commit) entry.bounds = bounds;
                    if (!entry.activations.Contains(_cachedLayerName))
                    {
                        entry.activations.Add(_cachedLayerName);
                    }
                    manager.EDITOR_AddSightLine(_cachedLayerName, entry);
                }

                int count = 0;
                foreach (MeshRenderer filter in allMeshes)
                {
                    GameObject target = filter.gameObject;
                    SightlinesLayer layer;
                    if (!target.TryGetComponent(out layer))
                    {
                        layer = target.AddComponent<SightlinesLayer>();
                    }
                    if (!layer.layerNames.Contains(_cachedLayerName))
                    {
                        layer.layerNames.Add(_cachedLayerName);
                        ++count;
                    }
                }

                EditorPrefs.SetBool(SIGHTLINESPREBAKED_ID, false);
                ShowNotification(new GUIContent(count + " meshes committed to layer " + _cachedLayerName));
                manager.SightlinesPrebaked = false;
                _cachedLayerName = string.Empty;
            }
            #endregion

            #region Remove Button
            if (GUILayout.Button("Remove"))
            {
                if (string.IsNullOrEmpty(_cachedLayerName) || idx <= -1)
                {
                    ShowNotification(new GUIContent("Unable to remove meshes; no layer declared"));
                    return;
                }

                foreach (MeshRenderer mesh in allMeshes)
                {
                    SightlinesLayer sll = mesh.GetComponent<SightlinesLayer>();
                    Debug.Assert(sll.layerNames.Contains(_cachedLayerName));
                    sll.layerNames.Remove(_cachedLayerName);
                }

                ShowNotification(new GUIContent("Removed " + allMeshes.Length + " meshes from " + _cachedLayerName));
            }
            #endregion
            EditorGUILayout.EndHorizontal();
        }
    }
}
