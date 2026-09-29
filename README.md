# Wave Object Distributor

Wave Object Distributor is a Unity component that distributes a GameObject's direct children across the local X axis and offsets them along the local Y axis with a sine wave. It can be used as a live runtime component and as an Edit Mode layout/preview tool.

## Requirements

- Unity 6000.3.15f1 verified
- No external packages required

## Installation

Copy these files into a Unity project:

```text
Runtime/WaveObjectDistributor.cs
Editor/WaveObjectDistributorEditor.cs
```

The editor script must remain inside an `Editor` folder. The runtime component is build-safe and can remain in player builds.

## Usage

1. Add `WaveObjectDistributor` to a parent GameObject.
2. Add the objects you want to distribute as direct children of that GameObject.
3. Adjust the distribution and wave settings in the Inspector.
4. Use `Refresh Children`, `Apply Layout Now`, or `Randomize Seed` when you want to manually refresh/apply the current setup.

The component is marked `[ExecuteAlways]`, so it applies layout during edit-time validation and lifecycle callbacks. In Play Mode, it applies the wave layout every frame.

## Behavior

- Distribution axis: local X
- Wave displacement axis: local Y
- Wave type: sine
- Affected objects: direct children only
- Ordering: sibling order determines distribution order and per-child variation identity
- Position ownership: the component overwrites affected child local positions while applied
- Rotation and scale are not modified
- Zero children: no-op
- One child: placed at the midpoint (`t = 0.5`)
- Negative `waveSpeed` is valid and reverses wave direction

## Edit Mode And Runtime

Edit Mode layout updates happen through `[ExecuteAlways]`, `OnValidate`, `OnEnable`, `Reset`, and manual inspector actions. Continuous Edit Mode animation is optional through `animateInEditMode`.

Runtime animation is always active in Play Mode. There is no separate runtime animation toggle.

## Deterministic Variation

When `randomizeMotion` is enabled, per-child phase, speed, amplitude, and height variation are calculated deterministically from `randomSeed`, child index, and fixed hash salts.

`Randomize Seed` generates a new seed without using Unity's global random state. The resulting layout remains deterministic for that seed and current child order.

## Known Limitations

- Direct children only; descendants are not traversed recursively.
- No baseline capture or restore system is included.
- Disabling or removing the component does not restore prior child positions.
- Active/inactive filtering depends on `includeInactiveChildren`.
- Reordering children changes their distribution order and deterministic variation identity.

## Validation

The finalized implementation was validated with:

```text
WAVE_OBJECT_DISTRIBUTOR_VALIDATION_SUMMARY total=12 passed=12 failed=0
WAVE_OBJECT_DISTRIBUTOR_VALIDATION_PASS
```
