using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveObjectDistributor))]
public sealed class WaveObjectDistributorEditor : Editor
{
    private readonly List<Transform> childBuffer = new List<Transform>();

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField(
            "Distribute child objects along an axis with optional wave and randomized motion.",
            EditorStyles.wordWrappedMiniLabel);

        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Refresh Children", "Rebuild the cached direct-child list.")))
            {
                foreach (Object targetObject in targets)
                {
                    if (targetObject is WaveObjectDistributor distributor)
                    {
                        Undo.RecordObject(distributor, "Refresh Wave Objects");
                        distributor.RefreshChildren();
                        EditorUtility.SetDirty(distributor);
                    }
                }
            }

            if (GUILayout.Button(new GUIContent("Apply Layout", "Refresh children and immediately apply the wave layout.")))
            {
                foreach (Object targetObject in targets)
                {
                    if (targetObject is WaveObjectDistributor distributor)
                    {
                        distributor.RefreshChildren();
                        distributor.GetCachedChildren(childBuffer);

                        foreach (Transform child in childBuffer)
                        {
                            if (child != null)
                            {
                                Undo.RecordObject(child, "Apply Wave Layout");
                            }
                        }

                        distributor.ApplyLayoutNow();
                        EditorUtility.SetDirty(distributor);
                    }
                }
            }
        }

        if (GUILayout.Button(new GUIContent("Randomize Seed", "Generate a new seed for deterministic per-child wave variation.")))
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is WaveObjectDistributor distributor)
                {
                    Undo.RecordObject(distributor, "Randomize Wave Seed");
                    distributor.RefreshChildren();
                    distributor.GetCachedChildren(childBuffer);

                    foreach (Transform child in childBuffer)
                    {
                        if (child != null)
                        {
                            Undo.RecordObject(child, "Randomize Wave Seed");
                        }
                    }

                    distributor.RandomizeSeed();
                    EditorUtility.SetDirty(distributor);
                }
            }
        }
    }
}
