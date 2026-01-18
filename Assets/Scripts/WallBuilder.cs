using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

public class WallBuilder : MonoBehaviour
{

    [Range(1.0f, 100.0f)]
    public int segments = 10;

    [Range(0.5f, 5.0f)]
    public float width = 10;

    [Range(1.0f, 25.0f)]
    public float radius = 10;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void makeCircle()
    {
        while(transform.childCount > 0)
        {
            if(Application.isPlaying)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 360;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            Vector3 pos = new Vector3(x, transform.position.y, y);
            GameObject newWall = GameObject.CreatePrimitive(PrimitiveType.Plane);
            newWall.transform.position = pos;
            newWall.transform.LookAt(transform.position);
            newWall.name = "wall";
            newWall.transform.parent = transform;
            newWall.transform.Rotate(new Vector3(90, 0, 0));
            newWall.transform.localScale = new Vector3(width, 1, 1);
        }
    }



}

#if UNITY_EDITOR

[CustomEditor(typeof(WallBuilder))]
public class WBEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WallBuilder builder = (WallBuilder)target;

        if(GUILayout.Button("Build Walls"))
        {
            builder.makeCircle();
        }
    }
}

#endif