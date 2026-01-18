using UnityEngine;

public class Triangle : ProceduralMesh
{
    public override Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;
        mesh.name = "Triangle";

        mesh.vertices = new Vector3[] { new Vector3(5,5,5),
                                        new Vector3(6,6,5),
                                        new Vector3(7,5,5)};

        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateBounds();

        return mesh;
    }
}
