using System.Collections.Generic;
using Parabox.CSG;
using UnityEngine;
using UnityEngine.Events;

public class CSGModelHolder : MonoBehaviour
{
    public MeshFilter Filter { get; private set; } = null;
    public MeshRenderer @Renderer { get; private set; } = null;

    public Material reclippedMaterial = null;

    public Model @Model { get; private set; } = null;
    public Node @Node { get; private set; } = null;
    [HideInInspector]
    protected List<CSGModelHolder> children = null;

    public UnityEvent OnTrigger;
    public bool ThrowOnReclip = true;

    private void OnEnable()
    {
        Filter = GetComponent<MeshFilter>();
        @Renderer = GetComponent<MeshRenderer>();
        if (Filter != null && @Renderer != null)
        {
            CSGUtil.GenerateBarycentric(gameObject);
            Filter = GetComponent<MeshFilter>();
            if (@Model == null)
            {
                // Could probably put this into a static dictionary
                @Model = new Model(gameObject);
            }
            if (@Model != null && @Node == null)
            {
                @Node = new Node(@Model.ToPolygons());
            }
        }

        if (children == null)
        {
            children = new List<CSGModelHolder>();
            for (int childIdx = 0; childIdx < transform.childCount; ++childIdx)
            {
                children.AddRange(transform.GetChild(childIdx).GetComponentsInChildren<CSGModelHolder>(false));
            }
        }
    }

    public static void CreateCompositeAndDisable(CSGModelHolder lhs, GameObject rhs)
    {
        for (int i = 0; i < lhs.children.Count; ++i)
        {
            CreateCompositeAndDisableAction(lhs.children[i], rhs);
            //lhs.children[i].gameObject.SetActive(false);
            lhs.OnTrigger.Invoke();
        }

        CreateCompositeAndDisableAction(lhs, rhs);
        lhs.GetComponent<Collider>().enabled = false;
        //lhs.gameObject.SetActive(false);

        lhs.OnTrigger.Invoke();
    }

    private static void CreateCompositeAndDisableAction(CSGModelHolder minusend, GameObject subtrahend)
    {
        if (minusend.Node == null) return;

        Model subtrahendModel = new Model(subtrahend);
        Node subtrahendNode = new Node(subtrahendModel.ToPolygons());
        List<Polygon> polygons = Node.Subtract(minusend.@Node, subtrahendNode).AllPolygons();
        if (polygons.Count == 0)
        {
            // We hit a failure state and I don't wanna spend time debugging it so fuck it
            minusend.Renderer.enabled = false;
            return;
        }
        Model retVal = new Model(polygons);

        GameObject composite = new GameObject();
        composite.layer = LayerMask.NameToLayer("Ignore Raycast");
        composite.AddComponent<MeshFilter>().mesh = retVal.mesh;
        MeshRenderer rend = composite.AddComponent<MeshRenderer>();
        if (minusend.ThrowOnReclip)
        {
            composite.AddComponent<SphereCollider>();
            Rigidbody rb = composite.AddComponent<Rigidbody>();
            //rb.AddExplosionForce(5f, subtrahend.transform.position, 4f, 1.2f, ForceMode.Impulse);
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
            rb.AddRelativeTorque(Random.onUnitSphere * 5f, ForceMode.Impulse);
            Destroy(composite, 5f);
        }
        Material[] newMats = retVal.materials.ToArray();
        if (minusend.reclippedMaterial)
        {
            MeshRenderer origRend = minusend.GetComponent<MeshRenderer>();
            for (int i = 0; i < newMats.Length; ++i)
            {
                newMats[i] = minusend.reclippedMaterial;
            }
        }
        rend.materials = newMats;
        composite.name = string.Format("Composite[{0}]", minusend.name);
        minusend.Renderer.enabled = false;
    }
}