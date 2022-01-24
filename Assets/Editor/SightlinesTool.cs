using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Assign Sightlines")]
public class SightlinesTool : EditorTool
{
    List<MeshRenderer> allFilters = new List<MeshRenderer>();
    Bounds bounds;

    [MenuItem("NoClip/Enable Sightlines Chunking Tool", validate = true)]
    public static bool CanEnableSightlines()
    {
        Debug.Log("Seleciton.count:" + Selection.count);
        return Selection.count > 0;
    }
    [MenuItem("NoClip/Enable Sightlines Chunking Tool")]
    public static void EnableSightlines()
    {
        ToolManager.SetActiveTool<SightlinesTool>();
    }

    void ActiveToolChange()
    {
        if (!ToolManager.IsActiveTool(this))
        {
            return;
        }

        bool initialized = false;
        allFilters.Clear();
        foreach (Transform xform in Selection.transforms)
        {
            MeshRenderer[] renderer = xform.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderer.Length; ++i)
            {
                Transform transform = renderer[i].transform;
                MeshFilter filter = renderer[i].GetComponent<MeshFilter>();
                Vector3[] verts = filter.sharedMesh.vertices;
                if (!initialized)
                {
                    bounds = new Bounds(transform.TransformPoint(verts[0]), Vector3.zero);
                    initialized = true;
                }
                foreach (Vector3 vert in verts)
                {
                    bounds.Encapsulate(transform.TransformPoint(vert));
                }
            }
            allFilters.AddRange(renderer);
        }

        // padding
        bounds.Expand(0.5f);
        SightlinesWindow.Init(allFilters.ToArray(), bounds);
    }

    private void OnEnable()
    {
        ToolManager.activeToolChanged += ActiveToolChange;
        Selection.selectionChanged += ActiveToolChange;
    }
    private void OnDisable()
    {
        ToolManager.activeToolChanged -= ActiveToolChange;
        Selection.selectionChanged -= ActiveToolChange;
    }

    public override void OnToolGUI(EditorWindow window)
    {
        Event ev = Event.current;

        if (ev.type == EventType.Repaint)
        {
            var prevZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            {
                Handles.color = Color.red;
                Handles.DrawWireCube(bounds.center, bounds.size);
            }
            Handles.zTest = prevZTest;
        }
    }
}
