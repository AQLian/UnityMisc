using UnityEngine;
using TMPro;

public class TMP_Bouncer_Optimized : MonoBehaviour
{
    public TMP_Text textComponent;
    public float bounceHeight = 10f;
    public float bounceSpeed = 5f;

    private TMP_MeshInfo[] cachedMeshInfo; // We store the original mesh here

    void OnEnable()
    {
        // Listen for text changes so we can update our cache
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    // Only triggered when the actual string changes
    void OnTextChanged(Object obj)
    {
        if (obj == textComponent) UpdateCache();
    }

    void UpdateCache()
    {
        textComponent.ForceMeshUpdate(); // We only force update ONCE when text changes
        cachedMeshInfo = textComponent.textInfo.CopyMeshInfoVertexData();
    }

    void Update()
    {
        // Safety check
        if (cachedMeshInfo == null) UpdateCache();

        var textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        for (int i = 0; i < characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // BEST PRACTICE: Read from the CACHED (original) positions
            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

            // Calculate offset
            float bounce = Mathf.Sin(Time.time * bounceSpeed + i) * bounceHeight;
            Vector3 offset = new Vector3(0, bounce, 0);

            // Write to the WORKING textInfo
            // We set the position explicitly: Original Position + Offset
            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] + offset;
            destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] + offset;
            destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] + offset;
            destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] + offset;
        }

        // Apply changes
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}