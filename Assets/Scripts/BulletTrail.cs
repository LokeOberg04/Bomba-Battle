using System.Collections;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{

    public float duration = 0.5f;
    LineRenderer lineRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void startTrail(Vector3 start, Vector3 end)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        StartCoroutine(fadeOut());
    }

    public IEnumerator fadeOut()
    {
        float time = 0f;
        while (duration > time)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time/duration);
            Color color = new Color(1,1,1,alpha);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            yield return null;
        }
        Destroy(gameObject);
    }

}
