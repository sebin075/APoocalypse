using System.Collections;
using UnityEngine;

public class VRScreenFade : MonoBehaviour
{
    private Material fadeMaterial;
    private float alpha = 0.0f;

    private void Awake()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            fadeMaterial = new Material(shader);
            fadeMaterial.color = Color.white; // ★ 하얀색으로 설정
        }
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        alpha = endAlpha;
    }

    private void OnPostRender()
    {
        if (fadeMaterial == null || alpha <= 0f) return;

        GL.PushMatrix();
        fadeMaterial.SetPass(0);
        GL.LoadOrtho();

        GL.Begin(GL.QUADS);
        GL.Color(new Color(1, 1, 1, alpha)); // ★ 하얀색 알파 치트
        GL.Vertex3(0, 0, 0);
        GL.Vertex3(1, 0, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(0, 1, 0);
        GL.End();

        GL.PopMatrix();
    }
}