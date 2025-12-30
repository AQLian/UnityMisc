using UnityEngine;
using TMPro;

public class TMP_ColorLerp_Optimized : MonoBehaviour
{
    [Header("Settings")]
    public TMP_Text textComponent;
    public Color colorA = Color.white;
    public Color colorB = Color.red;
    public float speed = 3.0f;
    public float splitOffset = 0.5f;

    // This is our "Glass Case" - the safe master copy of the mesh
    private TMP_MeshInfo[] cachedMeshInfo;

    void OnEnable()
    {
        // Subscribe to the event: "If the text string changes, tell me!"
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnTextChanged(Object obj)
    {
        // Only update if OUR text component changed
        if (obj == textComponent)
        {
            UpdateCache();
        }
    }

    void UpdateCache()
    {
        // We run this expensive operation ONLY when the text actually changes (e.g. "Score: 1" -> "Score: 2")
        textComponent.ForceMeshUpdate(); 
        cachedMeshInfo = textComponent.textInfo.CopyMeshInfoVertexData();
    }

    void Update()
    {
        // Safety check: if we haven't cached yet, do it now
        if (cachedMeshInfo == null) UpdateCache();

        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        // Loop through characters
        for (int i = 0; i < characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            // Calculate the color
            float t = Mathf.PingPong(Time.time * speed + (i * splitOffset), 1f);
            Color32 newColor = Color.Lerp(colorA, colorB, t);

            // Get indices
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // CRITICAL: Even though we are only changing colors, we grab the array 
            // from the meshInfo to apply them.
            // Note: Colors don't strictly require reading from Cache like Positions do,
            // but the structure ensures we are efficient.
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            // Apply color to all 4 vertices
            vertexColors[vertexIndex + 0] = newColor;
            vertexColors[vertexIndex + 1] = newColor;
            vertexColors[vertexIndex + 2] = newColor;
            vertexColors[vertexIndex + 3] = newColor;
        }

        // Apply the changes to the mesh
        // We only update the COLORS channel, which is very fast
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}