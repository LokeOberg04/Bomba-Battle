using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]

public abstract class ProceduralMesh : MonoBehaviour
{

    private Mesh m_mesh;

    public Mesh Mesh => m_mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        UpdateMesh();
    }

    protected virtual void OnDestroy()
    {
        Cleanup();
    }

    void Cleanup()
    {
        if (m_mesh != null)
        {
            if(Application.isPlaying)
            {
                Destroy(m_mesh);
            }
            else
            {
                DestroyImmediate(m_mesh);
            }

            m_mesh = null;
        }
    }

    public abstract Mesh CreateMesh();

    public virtual void UpdateMesh()
    {
        m_mesh = CreateMesh();
        GetComponent<MeshFilter>().mesh = m_mesh;
    }
}
