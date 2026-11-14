using System.Collections.Generic;
using UnityEngine;

public class SpikeSurfaceGenerator : MonoBehaviour
{
    public enum AxisMode { AutoLongestAxis, LocalY, LocalX, LocalZ, WorldUp }
    public enum StartMode { Auto, TopSurface, OuterWall }

    [Header("References")]
    public MeshCollider target;
    public GameObject spikePrefab;

    [Header("Cylinder axis (check the green gizmo line)")]
    [Tooltip("Which direction is 'up' along the cylinder. Auto picks the longest axis of the mesh.")]
    public AxisMode axisMode = AxisMode.AutoLongestAxis;
    [Tooltip("Tick if the green gizmo shows the top at the wrong end of the cylinder.")]
    public bool flipAxis = false;
    [Tooltip("Tick if the cylinder moves/rotates while playing (slower: re-measures the mesh).")]
    public bool trackTargetMovement = false;

    [Header("Spawning")]
    public bool spawnOnStart = true;
    [Min(0f), Tooltip("Seconds between each spawned spike.")]
    public float spawnCooldown = 0.3f;
    [Min(1), Tooltip("No new spikes are spawned while this many are alive.")]
    public int maxActiveSpikes = 60;

    [Header("Random start on top")]
    [Tooltip("Auto: try the top surface first, and if the top is open/hollow, start on the outer wall just below the rim.")]
    public StartMode startMode = StartMode.Auto;
    [Range(0f, 1f), Tooltip("Inner limit of the random start area, as a fraction of the cylinder radius (0 = center).")]
    public float minStartRadiusFraction = 0f;
    [Range(0f, 1f), Tooltip("Outer limit of the random start area, as a fraction of the cylinder radius (1 = rim).")]
    public float maxStartRadiusFraction = 0.9f;
    [Range(0.01f, 1f), Tooltip("A top-surface hit must be within this fraction of the total height below the top, " +
                               "otherwise it is rejected (prevents starting inside a hollow model).")]
    public float topBandFraction = 0.25f;
    [Range(0f, 0.5f), Tooltip("OuterWall start: how far below the top (fraction of height) the spikes start.")]
    public float wallStartDepthFraction = 0.05f;

    [Header("Camera preference")]
    [Tooltip("Favor spawning on the side of the cylinder that faces the camera.")]
    public bool favorCamera = true;
    [Range(0f, 1f), Tooltip("Chance that a spike spawns on the camera-facing side. 0 = fully random all around, 1 = always facing the camera. " +
                            "The remaining spikes still spawn randomly all around.")]
    public float cameraBias = 0.7f;
    [Range(5f, 180f), Tooltip("Half-width in degrees of the camera-facing area (60 = a 120 degree wedge centered on the camera).")]
    public float facingSpread = 60f;

    [Header("Movement")]
    [Min(0f), Tooltip("Slide speed range in units per second. Each spike picks a random value.")]
    public Vector2 speedRange = new Vector2(0.4f, 0.8f);
    [Min(0.01f), Tooltip("Length of each surface-following step. Smaller = follows tiny bumps better (a bit more CPU).")]
    public float walkStep = 0.08f;
    [Min(0f), Tooltip("How quickly the spike turns to match the surface (0 = snap instantly).")]
    public float rotationSmoothing = 12f;
    [Tooltip("After spawning, a spike never spins left/right on its own: it only tilts to stay stuck to the surface. " +
             "Its heading (including the random Y offset) is decided once at spawn.")]
    public bool lockSpinAfterSpawn = true;
    [Tooltip("Despawn a spike when it reaches the bottom of the cylinder or the path ends.")]
    public bool despawnWhenPathEnds = true;
    [Tooltip("Stop a spike when it gets this close to the bottom of the cylinder.")]
    public float stopAboveBottom = 0.05f;

    [Header("Despawn")]
    public bool despawnWhenOffScreen = true;
    [Tooltip("Camera used for the off-screen test. Empty = Camera.main.")]
    public Camera viewCamera;
    [Tooltip("Only despawn once the spike has been on screen at least once (so spikes that start off screen aren't removed instantly).")]
    public bool onlyAfterSeen = true;
    [Range(0f, 0.5f), Tooltip("Extra border around the screen, in viewport units, before a spike counts as off screen.")]
    public float offScreenMargin = 0.05f;
    [Min(0f), Tooltip("Seconds a spike must stay off screen before it is removed.")]
    public float offScreenDelay = 0.25f;
    [Min(0f), Tooltip("Safety net: remove any spike older than this many seconds (0 = never).")]
    public float maxLifetime = 40f;

    [Header("Spike look")]
    [Tooltip("Random scale multiplier range per spike.")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    [Min(0f), Tooltip("Seconds a spike takes to grow from scale 0 to its full size after spawning (0 = appears at full size).")]
    public float growDuration = 0.6f;
    [Tooltip("Growth shape: X = time through the growth (0 to 1), Y = size (0 to 1). Pull the curve above 1 for an overshoot/pop effect.")]
    public AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Pushes each spike outward along the surface normal. Raise it if the spike sinks into the model.")]
    public float surfaceOffset = 0f;
    public bool alignToSurfaceNormal = true;
    [Tooltip("Disables Rigidbodies/Colliders on spawned spikes so they can't be pushed into the model by physics.")]
    public bool disableSpikePhysics = true;

    [Header("Spike rotation")]
    [Tooltip("The spike prefab's own rotation is kept: the direction it visually points in the prefab " +
             "is the direction that gets aligned to the surface.")]
    public bool honorPrefabRotation = true;
    [Tooltip("The spike's tip direction in the prefab's local space (Y = (0,1,0) for a normal cone).")]
    public Vector3 tipAxis = Vector3.up;
    [Tooltip("Each spike gets a fixed random rotation offset in [-value, +value] degrees per axis. " +
             "Measured around the surface normal: Y spins the spike around the normal, X/Z tilt it away from it.")]
    public Vector3 maxRotationOffset = new Vector3(15f, 45f, 15f);

    [Header("Debug")]
    [Tooltip("Draws a green line along the surface normal at every spawn point (Scene view, 10 s). " +
             "If these lines point INTO the model, the mesh normals / collider are the problem.")]
    public bool debugSpawnNormals = false;

    const string ContainerName = "Spikes";

    class Walker
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector3 lastDir;
    }

    class Spike
    {
        public Transform t;
        // Current waypoint on the surface and the next one; the spike glides between them.
        public Vector3 pos, normal, dir;
        public Vector3 nextPos, nextNormal, nextDir;
        public float progress;
        public float speed;
        public Vector3 randomEuler;
        public Vector3 tiltNormal;   // direction the spike's tip currently follows (smoothed)
        public Vector3 baseScale;    // full-size scale of this spike
        public bool seen;
        public bool stopped;
        public float age;
        public float offTime;
    }

    readonly List<Spike> spikes = new List<Spike>();
    Transform container;
    bool spawning;
    float spawnTimer;

    // Cylinder description, computed from the real mesh vertices.
    Vector3 up;            // axis direction (top = +up)
    Vector3 refPerp;       // two directions perpendicular to the axis
    Vector3 refPerp2;
    Vector3 planeCenter;   // axis position (perpendicular part only); axis point at height h = planeCenter + up * h
    float topH;
    float bottomH;
    float radius;

    // Cache so gizmos / Update don't recompute needlessly.
    bool statsValid;
    Matrix4x4 cachedMatrix;
    Mesh cachedMesh;
    AxisMode cachedAxis;
    bool cachedFlip;

    bool warnedNoStart;
    bool warnedNoCamera;
    bool diagnosed;

    // ---------------------------------------------------------------- lifecycle

    void Start()
    {
        if (spawnOnStart) StartSpawning();
    }

    [ContextMenu("Start Spawning")]
    public void StartSpawning()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("SpikeSurfaceGenerator: moving spikes only work in Play mode.", this);
            return;
        }
        if (target == null || spikePrefab == null)
        {
            Debug.LogWarning("SpikeSurfaceGenerator: assign a target MeshCollider and a spike prefab.", this);
            return;
        }
        if (!Prepare(true)) return;

        EnsureContainer();
        warnedNoStart = false;
        diagnosed = false;
        spawning = true;
        spawnTimer = 0f;
    }

    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        spawning = false;
    }

    [ContextMenu("Clear Spikes")]
    public void Clear()
    {
        for (int i = 0; i < spikes.Count; i++)
            if (spikes[i].t != null) Destroy(spikes[i].t.gameObject);
        spikes.Clear();
    }

    void EnsureContainer()
    {
        if (container != null) return;
        Transform old = transform.Find(ContainerName);
        if (old != null) { container = old; return; }
        container = new GameObject(ContainerName).transform;
        container.SetParent(transform, false);
    }

    void Update()
    {
        if (target == null || spikePrefab == null) return;
        if (trackTargetMovement) Prepare();
        if (!statsValid) return;

        float dt = Time.deltaTime;

        if (spawning)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnCooldown;
                if (spikes.Count < maxActiveSpikes) TrySpawn();
            }
        }

        UpdateSpikes(dt);
    }

    // ---------------------------------------------------------------- cylinder analysis

    Vector3 GetAxis(Transform tt, Mesh mesh)
    {
        switch (axisMode)
        {
            case AxisMode.LocalX: return tt.right;
            case AxisMode.LocalY: return tt.up;
            case AxisMode.LocalZ: return tt.forward;
            case AxisMode.WorldUp: return Vector3.up;
        }

        // Auto: the longest axis of the mesh (taking scale into account) is the cylinder axis.
        if (mesh == null) return tt.up;
        Vector3 size = mesh.bounds.size;
        Vector3 s = tt.lossyScale;
        float sx = size.x * Mathf.Abs(s.x);
        float sy = size.y * Mathf.Abs(s.y);
        float sz = size.z * Mathf.Abs(s.z);
        if (sy >= sx && sy >= sz) return tt.up;
        if (sx >= sz) return tt.right;
        return tt.forward;
    }

    bool Prepare(bool force = false)
    {
        if (target == null) return false;

        Transform tt = target.transform;
        Mesh mesh = target.sharedMesh;
        Matrix4x4 m = tt.localToWorldMatrix;

        if (!force && statsValid && m == cachedMatrix && mesh == cachedMesh &&
            cachedAxis == axisMode && cachedFlip == flipAxis)
            return true;

        up = GetAxis(tt, mesh).normalized;
        if (flipAxis) up = -up;

        refPerp = Vector3.Cross(up, Mathf.Abs(up.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        refPerp2 = Vector3.Cross(up, refPerp).normalized;

        if (mesh != null && mesh.isReadable && mesh.vertexCount > 0)
        {
            Vector3[] local = mesh.vertices;
            Vector3[] world = new Vector3[local.Length];

            float minH = float.MaxValue, maxH = float.MinValue;
            float minA = float.MaxValue, maxA = float.MinValue;
            float minB = float.MaxValue, maxB = float.MinValue;

            for (int i = 0; i < local.Length; i++)
            {
                Vector3 w = tt.TransformPoint(local[i]);
                world[i] = w;

                float h = Vector3.Dot(w, up);
                float a = Vector3.Dot(w, refPerp);
                float b = Vector3.Dot(w, refPerp2);
                if (h < minH) minH = h; if (h > maxH) maxH = h;
                if (a < minA) minA = a; if (a > maxA) maxA = a;
                if (b < minB) minB = b; if (b > maxB) maxB = b;
            }

            planeCenter = refPerp * ((minA + maxA) * 0.5f) + refPerp2 * ((minB + maxB) * 0.5f);
            topH = maxH;
            bottomH = minH;

            float maxSqr = 0f;
            for (int i = 0; i < world.Length; i++)
            {
                Vector3 w = world[i];
                Vector3 d = w - up * Vector3.Dot(w, up) - planeCenter;
                float sq = d.sqrMagnitude;
                if (sq > maxSqr) maxSqr = sq;
            }
            radius = Mathf.Sqrt(maxSqr);
        }
        else
        {
            // Mesh isn't readable: fall back to the collider's bounds (less accurate).
            Bounds bnd = target.bounds;
            Vector3 e = bnd.extents;
            float halfHeight = e.x * Mathf.Abs(up.x) + e.y * Mathf.Abs(up.y) + e.z * Mathf.Abs(up.z);
            float c = Vector3.Dot(bnd.center, up);
            topH = c + halfHeight;
            bottomH = c - halfHeight;
            planeCenter = bnd.center - up * c;

            float ra = e.x * Mathf.Abs(refPerp.x) + e.y * Mathf.Abs(refPerp.y) + e.z * Mathf.Abs(refPerp.z);
            float rb = e.x * Mathf.Abs(refPerp2.x) + e.y * Mathf.Abs(refPerp2.y) + e.z * Mathf.Abs(refPerp2.z);
            radius = Mathf.Min(ra, rb);
        }

        cachedMatrix = m;
        cachedMesh = mesh;
        cachedAxis = axisMode;
        cachedFlip = flipAxis;
        statsValid = true;
        return true;
    }

    // ---------------------------------------------------------------- spawning

    void TrySpawn()
    {
        if (!TryStartWalker(out Walker w)) return;

        DiagnoseOnce();
        if (debugSpawnNormals) Debug.DrawRay(w.pos, w.normal * 0.5f, Color.green, 10f);

        Vector3 m = new Vector3(Mathf.Abs(maxRotationOffset.x),
                                Mathf.Abs(maxRotationOffset.y),
                                Mathf.Abs(maxRotationOffset.z));

        var s = new Spike
        {
            pos = w.pos,
            normal = w.normal,
            dir = w.lastDir,
            speed = Random.Range(Mathf.Min(speedRange.x, speedRange.y), Mathf.Max(speedRange.x, speedRange.y)),
            randomEuler = new Vector3(Random.Range(-m.x, m.x), Random.Range(-m.y, m.y), Random.Range(-m.z, m.z))
        };

        // Work out where the spike heads first. If it can't go anywhere, don't spawn it.
        if (!Advance(s)) return;

        EnsureContainer();
        Quaternion rot = ComputeRotation(s.normal, s.randomEuler);
        GameObject go = Instantiate(spikePrefab, s.pos + s.normal * surfaceOffset, rot, container);

        float k = Random.Range(Mathf.Min(scaleRange.x, scaleRange.y), Mathf.Max(scaleRange.x, scaleRange.y));
        s.baseScale = spikePrefab.transform.localScale * k;
        go.transform.localScale = s.baseScale * GrowFactor(0f);

        if (disableSpikePhysics)
        {
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>())
            {
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            foreach (Collider col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        s.t = go.transform;
        s.tiltNormal = alignToSurfaceNormal ? s.normal : up;
        spikes.Add(s);
    }

    /// <summary>Computes the next surface waypoint from the spike's current one.</summary>
    bool Advance(Spike s)
    {
        Vector3 p = s.pos, n = s.normal, d = s.dir;
        if (!Walk(ref p, ref n, ref d, walkStep)) return false;
        s.nextPos = p;
        s.nextNormal = n;
        s.nextDir = d;
        return true;
    }

    void UpdateSpikes(float dt)
    {
        Camera cam = viewCamera != null ? viewCamera : Camera.main;
        if (despawnWhenOffScreen && cam == null && !warnedNoCamera)
        {
            warnedNoCamera = true;
            Debug.LogWarning("SpikeSurfaceGenerator: no camera found for off-screen despawn. " +
                             "Assign View Camera or tag a camera as MainCamera.", this);
        }

        for (int i = spikes.Count - 1; i >= 0; i--)
        {
            Spike s = spikes[i];
            if (s.t == null) { spikes.RemoveAt(i); continue; }

            s.age += dt;

            // --- move along the surface ---
            if (!s.stopped)
            {
                s.progress += s.speed * dt / walkStep;

                int guard = 0;
                while (s.progress >= 1f && guard++ < 10)
                {
                    s.pos = s.nextPos;
                    s.normal = s.nextNormal;
                    s.dir = s.nextDir;
                    s.progress -= 1f;

                    bool atBottom = Vector3.Dot(s.pos, up) <= bottomH + stopAboveBottom;
                    if (atBottom || !Advance(s))
                    {
                        s.stopped = true;
                        s.progress = 0f;
                        s.nextPos = s.pos;
                        s.nextNormal = s.normal;
                        break;
                    }
                }
                if (s.progress > 1f) s.progress = 1f;

                if (s.stopped && despawnWhenPathEnds)
                {
                    Remove(i);
                    continue;
                }
            }

            // --- place and orient ---
            float f = s.stopped ? 0f : s.progress;
            Vector3 p = Vector3.Lerp(s.pos, s.nextPos, f);
            Vector3 n = Vector3.Lerp(s.normal, s.nextNormal, f);
            n = n.sqrMagnitude > 1e-6f ? n.normalized : s.normal;

            s.t.position = p + n * surfaceOffset;
            s.t.localScale = s.baseScale * GrowFactor(s.age);

            if (lockSpinAfterSpawn)
            {
                // Only tilt: rotate by the smallest rotation that carries the old surface
                // direction to the new one. This adds no left/right spin, so the spike keeps
                // the heading it was spawned with.
                Vector3 want = alignToSurfaceNormal ? n : up;
                Vector3 newTilt = rotationSmoothing > 0f
                    ? Vector3.Slerp(s.tiltNormal, want, 1f - Mathf.Exp(-rotationSmoothing * dt))
                    : want;
                if (newTilt.sqrMagnitude > 1e-6f)
                {
                    newTilt.Normalize();
                    s.t.rotation = Quaternion.FromToRotation(s.tiltNormal, newTilt) * s.t.rotation;
                    s.tiltNormal = newTilt;
                }
            }
            else
            {
                Quaternion targetRot = ComputeRotation(n, s.randomEuler);
                s.t.rotation = rotationSmoothing > 0f
                    ? Quaternion.Slerp(s.t.rotation, targetRot, 1f - Mathf.Exp(-rotationSmoothing * dt))
                    : targetRot;
            }

            // --- despawn rules ---
            if (maxLifetime > 0f && s.age > maxLifetime)
            {
                Remove(i);
                continue;
            }

            if (despawnWhenOffScreen && cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(s.t.position);
                float mg = offScreenMargin;
                bool onScreen = vp.z > 0f && vp.x > -mg && vp.x < 1f + mg && vp.y > -mg && vp.y < 1f + mg;

                if (onScreen)
                {
                    s.seen = true;
                    s.offTime = 0f;
                }
                else if (s.seen || !onlyAfterSeen)
                {
                    s.offTime += dt;
                    if (s.offTime >= offScreenDelay)
                    {
                        Remove(i);
                        continue;
                    }
                }
            }
        }
    }

    /// <summary>Size multiplier (0 to 1) for a spike of the given age.</summary>
    float GrowFactor(float age)
    {
        if (growDuration <= 0f) return 1f;
        float t = Mathf.Clamp01(age / growDuration);
        float g = growCurve != null && growCurve.length > 0 ? growCurve.Evaluate(t) : t;
        return Mathf.Max(g, 0.0001f); // never exactly 0, which can upset renderers
    }

    void Remove(int i)
    {
        if (spikes[i].t != null) Destroy(spikes[i].t.gameObject);
        spikes.RemoveAt(i);
    }

    Quaternion ComputeRotation(Vector3 n, Vector3 randomEuler)
    {
        Vector3 axis = alignToSurfaceNormal ? n : up;

        // Prefab's own rotation is kept; its tip direction (tipAxis, in prefab space) is what gets aligned.
        Quaternion prefabRot = honorPrefabRotation ? spikePrefab.transform.rotation : Quaternion.identity;
        Vector3 localTip = tipAxis.sqrMagnitude > 1e-6f ? tipAxis.normalized : Vector3.up;
        Vector3 currentTip = prefabRot * localTip;
        Quaternion align = Quaternion.FromToRotation(currentTip, axis) * prefabRot;

        // Random offset measured in a frame whose Y axis is the surface normal.
        Quaternion frame = Quaternion.FromToRotation(Vector3.up, axis);
        Quaternion offset = frame * Quaternion.Euler(randomEuler) * Quaternion.Inverse(frame);

        return offset * align;
    }

    // ---------------------------------------------------------------- diagnostics

    void DiagnoseOnce()
    {
        if (diagnosed) return;
        diagnosed = true;

        if (target.convex)
            Debug.LogWarning("SpikeSurfaceGenerator: the MeshCollider is Convex, so it is only a simplified hull of the model. " +
                             "Untick Convex for accurate spawning.", this);

        Renderer r = target.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            float d = Vector3.Distance(r.bounds.center, target.bounds.center);
            if (d > 0.25f * Mathf.Max(0.01f, r.bounds.size.magnitude))
                Debug.LogWarning("SpikeSurfaceGenerator: the MeshCollider and the visible mesh are NOT in the same place " +
                                 "(their bounds centers differ). Spikes will appear where the collider is, not where the model looks like it is. " +
                                 "Assign the MeshCollider that sits on the visible model.", this);
        }
    }

    // ---------------------------------------------------------------- start points

    Vector3 RandomRadial()
    {
        // With probability cameraBias, pick a direction inside a wedge facing the camera.
        if (favorCamera && cameraBias > 0f && Random.value < cameraBias)
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - (planeCenter + up * topH);
                toCam = Vector3.ProjectOnPlane(toCam, up);

                // If the camera is almost straight above/below the axis there is no "facing side".
                if (toCam.sqrMagnitude > 1e-4f)
                {
                    toCam.Normalize();
                    float spread = Mathf.Abs(facingSpread);
                    return Quaternion.AngleAxis(Random.Range(-spread, spread), up) * toCam;
                }
            }
        }

        return Quaternion.AngleAxis(Random.Range(0f, 360f), up) * refPerp;
    }

    bool TryStartWalker(out Walker walker)
    {
        walker = null;

        if (startMode != StartMode.OuterWall && TryStartOnTop(out walker)) return true;
        if (startMode != StartMode.TopSurface && TryStartOnWall(out walker)) return true;

        if (!warnedNoStart)
        {
            warnedNoStart = true;
            Debug.LogWarning("SpikeSurfaceGenerator: could not find a start point on the target. Select this object " +
                             "and check the green gizmo line runs along the cylinder (change Axis Mode / Flip Axis if not). " +
                             "Also make sure the MeshCollider isn't Convex and the mesh normals face outward.", this);
        }
        return false;
    }

    /// <summary>Random point on the top surface (ray dropped from above).</summary>
    bool TryStartOnTop(out Walker walker)
    {
        walker = null;
        float lo = Mathf.Min(minStartRadiusFraction, maxStartRadiusFraction);
        float hi = Mathf.Max(minStartRadiusFraction, maxStartRadiusFraction);
        float height = topH - bottomH;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 radial = RandomRadial();

            // sqrt keeps the distribution uniform over the area instead of bunching at the center.
            float r = radius * Mathf.Sqrt(Random.Range(lo * lo, hi * hi));

            Vector3 origin = planeCenter + radial * r + up * (topH + 1f);
            if (!target.Raycast(new Ray(origin, -up), out RaycastHit hit, height + 2f))
                continue;

            // Reject hits that are not really on the top (e.g. the inside of a hollow model).
            float hitH = Vector3.Dot(hit.point, up);
            if (hitH < topH - height * topBandFraction) continue;
            if (Vector3.Dot(hit.normal, up) <= 0f) continue;

            walker = new Walker
            {
                pos = hit.point,
                normal = hit.normal,
                lastDir = radial // on the flat cap, head outward toward the rim, then down
            };
            return true;
        }
        return false;
    }

    /// <summary>Random point on the OUTER wall just below the top (ray shot inward from outside).</summary>
    bool TryStartOnWall(out Walker walker)
    {
        walker = null;
        float height = topH - bottomH;
        float depth = height * wallStartDepthFraction;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 radial = RandomRadial();
            Vector3 origin = planeCenter + up * (topH - depth) + radial * (radius + 1f);

            if (!target.Raycast(new Ray(origin, -radial), out RaycastHit hit, radius * 2f + 2f))
                continue;

            // Must be an outward-facing wall.
            if (Vector3.Dot(hit.normal, radial) <= 0.2f) continue;

            walker = new Walker
            {
                pos = hit.point,
                normal = hit.normal,
                lastDir = -up
            };
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- surface walking

    /// <summary>
    /// Moves one step along the surface in the "downhill" direction, then re-snaps to the mesh.
    /// Handles flat areas (uses the last direction), concave corners and convex edges.
    /// </summary>
    bool Walk(ref Vector3 pos, ref Vector3 normal, ref Vector3 lastDir, float dist)
    {
        // Downhill direction on the current surface. On a flat cap it's ~zero, so keep going the way we were heading.
        Vector3 dir = Vector3.ProjectOnPlane(-up, normal);
        if (dir.sqrMagnitude < 1e-4f)
            dir = Vector3.ProjectOnPlane(lastDir, normal);
        if (dir.sqrMagnitude < 1e-6f)
            return false;
        dir.Normalize();

        Vector3 next = pos + dir * dist;
        float h = Mathf.Max(dist, 0.1f);
        float maxJump = dist * 2.5f;
        RaycastHit hit;

        // A) Concave corner: the surface rises in front of us, so hit it head-on.
        float skin = Mathf.Max(0.01f, dist * 0.1f);
        if (target.Raycast(new Ray(pos + normal * skin, dir), out hit, dist * 1.5f))
            return Accept(hit, dir, maxJump, ref pos, ref normal, ref lastDir);

        // B) Normal case: drop a ray straight onto the surface from above the next point.
        if (target.Raycast(new Ray(next + normal * h, -normal), out hit, h * 2f))
            if (Accept(hit, dir, maxJump, ref pos, ref normal, ref lastDir))
                return true;

        // C) Convex edge: we walked off the edge, so shoot back and down to find the wall.
        Vector3 back = (-dir - normal).normalized;
        if (target.Raycast(new Ray(next, back), out hit, dist * 3f))
            if (Accept(hit, dir, maxJump, ref pos, ref normal, ref lastDir))
                return true;

        // D) Last resort: straight down from above.
        if (target.Raycast(new Ray(next + up * h, -up), out hit, h * 3f))
            return Accept(hit, dir, maxJump, ref pos, ref normal, ref lastDir);

        return false;
    }

    bool Accept(RaycastHit hit, Vector3 dir, float maxJump,
                ref Vector3 pos, ref Vector3 normal, ref Vector3 lastDir)
    {
        if (Vector3.Distance(hit.point, pos) > maxJump) return false;
        pos = hit.point;
        normal = hit.normal;
        lastDir = dir;
        return true;
    }

        
    void OnDrawGizmosSelected()
    {
        if (target == null || !Prepare()) return;

        Vector3 top = planeCenter + up * topH;
        Vector3 bottom = planeCenter + up * bottomH;
        float marker = Mathf.Max(0.02f, radius * 0.06f);

        // Axis: green line, red ball = TOP, blue ball = bottom.
        Gizmos.color = Color.green;
        Gizmos.DrawLine(bottom, top);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(top, marker);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(bottom, marker);

        // Random start area on top.
        float lo = Mathf.Min(minStartRadiusFraction, maxStartRadiusFraction);
        float hi = Mathf.Max(minStartRadiusFraction, maxStartRadiusFraction);
        Gizmos.color = Color.yellow;
        DrawRing(top, radius * hi);
        if (lo > 0f) DrawRing(top, radius * lo);
    }

    void DrawRing(Vector3 c, float r)
    {
        const int n = 48;
        Vector3 prev = c + refPerp * r;
        for (int i = 1; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 p = c + (refPerp * Mathf.Cos(a) + refPerp2 * Mathf.Sin(a)) * r;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}