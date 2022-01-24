using UnityEngine;

namespace Parabox.CSG
{
    public static class CSGUtil
    {
        /**
		 * Rebuild mesh with individual triangles, adding barycentric coordinates
		 * in the colors channel.  Not the most ideal wireframe implementation,
		 * but it works and didn't take an inordinate amount of time :)
		 */
        public static void GenerateBarycentric(GameObject go)
        {
            Mesh m = go.GetComponent<MeshFilter>().sharedMesh;

            if (m == null) return;

            int[] tris = m.triangles;
            int triangleCount = tris.Length;
            int submeshCount = m.subMeshCount;

            Vector3[] mesh_vertices = m.vertices;
            Vector3[] mesh_normals = m.normals;
            Vector2[] mesh_uv = m.uv;

            Vector3[] vertices = new Vector3[triangleCount];
            Vector3[] normals = new Vector3[triangleCount];
            Vector2[] uv = new Vector2[triangleCount];
            Color[] colors = new Color[triangleCount];

            for (int i = 0; i < triangleCount; i++)
            {
                vertices[i] = mesh_vertices[tris[i]];
                normals[i] = mesh_normals[tris[i]];
                uv[i] = mesh_uv[tris[i]];

                colors[i] = i % 3 == 0 ? new Color(1, 0, 0, 0) : (i % 3) == 1 ? new Color(0, 1, 0, 0) : new Color(0, 0, 1, 0);

                tris[i] = i;
            }

            Mesh wireframeMesh = new Mesh();

            wireframeMesh.Clear();
            wireframeMesh.vertices = vertices;
            wireframeMesh.triangles = tris;
            wireframeMesh.normals = normals;
            wireframeMesh.colors = colors;
            wireframeMesh.uv = uv;
            wireframeMesh.subMeshCount = submeshCount;
            for (int i = 0; i < m.subMeshCount; ++i)
            {
                var desc = m.GetSubMesh(i);
                wireframeMesh.SetSubMesh(i, desc);
            }

            wireframeMesh.name = m.name + " (Composite)";

            go.GetComponent<MeshFilter>().sharedMesh = wireframeMesh;
        }
    }

}
