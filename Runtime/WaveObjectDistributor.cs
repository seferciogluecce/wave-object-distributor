using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class WaveObjectDistributor : MonoBehaviour
{
    [Header("Children")]
    [SerializeField, Tooltip("Include inactive direct children in the wave layout.")]
    private bool includeInactiveChildren = true;

    [SerializeField, Tooltip("Refresh the cached direct-child list when relevant hierarchy or active-state changes are detected.")]
    private bool autoRefreshChildList = true;

    [Header("Distribution")]
    [SerializeField, Tooltip("Minimum local X position for the first distributed child.")]
    private float xMin = -5f;

    [SerializeField, Tooltip("Maximum local X position for the last distributed child.")]
    private float xMax = 5f;

    [SerializeField, Tooltip("Minimum local Y value used by the sine wave.")]
    private float yMin = -1f;

    [SerializeField, Tooltip("Maximum local Y value used by the sine wave.")]
    private float yMax = 1f;

    [SerializeField, Tooltip("Local Z position to use when Preserve Child Z is disabled.")]
    private float zOffset;

    [SerializeField, Tooltip("Keep each affected child's current local Z value instead of overwriting it.")]
    private bool preserveChildZ = true;

    [Header("Wave")]
    [SerializeField, Min(0f), Tooltip("Number of sine-wave cycles across the distributed children.")]
    private float waveCycles = 1.25f;

    [SerializeField, Tooltip("Wave animation speed. Negative values reverse the wave direction.")]
    private float waveSpeed = 1f;

    [SerializeField, Tooltip("Phase offset applied to the entire wave.")]
    private float phaseOffset;

    [SerializeField, Tooltip("Animate the wave continuously in Edit Mode. Runtime animation always runs in Play Mode.")]
    private bool animateInEditMode;

    [Header("Randomized Motion")]
    [SerializeField, Tooltip("Apply deterministic per-child phase, speed, amplitude, and height variation from the seed.")]
    private bool randomizeMotion = true;

    [SerializeField, Tooltip("Seed used for deterministic per-child wave variation.")]
    private int randomSeed = 12345;

    [SerializeField, Range(0f, 1f), Tooltip("Maximum deterministic per-child phase variation.")]
    private float phaseRandomness = 0.75f;

    [SerializeField, Range(0f, 1f), Tooltip("Maximum deterministic per-child speed variation.")]
    private float speedRandomness = 0.2f;

    [SerializeField, Range(0f, 1f), Tooltip("Maximum deterministic per-child amplitude variation.")]
    private float amplitudeRandomness = 0.2f;

    [SerializeField, Range(0f, 1f), Tooltip("Maximum deterministic per-child vertical offset variation.")]
    private float heightOffsetRandomness = 0.08f;

    private readonly List<Transform> children = new List<Transform>();
    private int cachedChildCount = -1;
    private int cachedChildSignature;

    private void Reset()
    {
        RefreshChildren();
        ApplyLayout(0f);
    }

    private void OnEnable()
    {
        RefreshChildren();
        ApplyLayout(GetWaveTime());
    }

    private void OnValidate()
    {
        if (xMax < xMin)
        {
            (xMin, xMax) = (xMax, xMin);
        }

        if (yMax < yMin)
        {
            (yMin, yMax) = (yMax, yMin);
        }

        waveCycles = Mathf.Max(0f, waveCycles);
        RefreshChildren();
        ApplyLayout(GetWaveTime());
    }

    private void Update()
    {
        bool childCacheChanged = autoRefreshChildList && HasChildCacheChanged();
        if (childCacheChanged)
        {
            RefreshChildren();
        }

        if (!Application.isPlaying && !animateInEditMode && !childCacheChanged)
        {
            return;
        }

        ApplyLayout(GetWaveTime());
    }

    [ContextMenu("Refresh Children")]
    public void RefreshChildren()
    {
        children.Clear();
        cachedChildCount = transform.childCount;
        cachedChildSignature = CalculateChildSignature();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && (includeInactiveChildren || child.gameObject.activeSelf))
            {
                children.Add(child);
            }
        }
    }

    [ContextMenu("Apply Layout Now")]
    public void ApplyLayoutNow()
    {
        RefreshChildren();
        ApplyLayout(GetWaveTime());
    }

    [ContextMenu("Randomize Seed")]
    public void RandomizeSeed()
    {
        randomSeed = GenerateRandomSeed();
        ApplyLayout(GetWaveTime());
    }

    public void GetCachedChildren(List<Transform> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        results.AddRange(children);
    }

    private bool HasChildCacheChanged()
    {
        return cachedChildCount != transform.childCount || cachedChildSignature != CalculateChildSignature();
    }

    private int CalculateChildSignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + transform.childCount;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                hash = hash * 31 + (child != null ? child.GetInstanceID() : 0);

                if (!includeInactiveChildren && child != null)
                {
                    hash = hash * 31 + (child.gameObject.activeSelf ? 1 : 0);
                }
            }

            return hash;
        }
    }

    private void ApplyLayout(float time)
    {
        int count = children.Count;
        if (count == 0)
        {
            return;
        }

        float yCenter = (yMin + yMax) * 0.5f;
        float yAmplitude = (yMax - yMin) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Transform child = children[i];
            if (child == null)
            {
                continue;
            }

            float t = count == 1 ? 0.5f : i / (count - 1f);
            float x = Mathf.Lerp(xMin, xMax, t);
            float childPhase = 0f;
            float childSpeedScale = 1f;
            float childAmplitudeScale = 1f;
            float childHeightOffset = 0f;

            if (randomizeMotion)
            {
                childPhase = SignedHash01(randomSeed, i, 17) * phaseRandomness;
                childSpeedScale += SignedHash01(randomSeed, i, 29) * speedRandomness;
                childAmplitudeScale += SignedHash01(randomSeed, i, 43) * amplitudeRandomness;
                childHeightOffset = SignedHash01(randomSeed, i, 61) * heightOffsetRandomness * yAmplitude;
            }

            float phaseTime = time * waveSpeed * Mathf.Max(0f, childSpeedScale) + phaseOffset + childPhase;
            float wave = Mathf.Sin((t * waveCycles + phaseTime) * Mathf.PI * 2f);
            float y = yCenter + childHeightOffset + wave * yAmplitude * Mathf.Max(0f, childAmplitudeScale);
            float z = preserveChildZ ? child.localPosition.z : zOffset;

            child.localPosition = new Vector3(x, y, z);
        }
    }

    private float GetWaveTime()
    {
        return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
    }

    private static int GenerateRandomSeed()
    {
        return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
    }

    private static float SignedHash01(int seed, int index, int salt)
    {
        return Hash01(seed, index, salt) * 2f - 1f;
    }

    private static float Hash01(int seed, int index, int salt)
    {
        unchecked
        {
            uint hash = (uint)seed;
            hash ^= (uint)(index + 1) * 0x9E3779B9u;
            hash ^= (uint)(salt + 1) * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }
}
