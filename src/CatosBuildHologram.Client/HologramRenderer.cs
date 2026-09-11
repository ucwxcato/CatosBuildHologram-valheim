using System;
using System.Collections.Generic;
using CatosBuildHologram.Shared.Contracts;
using UnityEngine;

namespace CatosBuildHologram.Client
{
    internal sealed class HologramRenderer
    {
        private readonly Dictionary<string, HologramView> _views
            = new Dictionary<string, HologramView>(StringComparer.Ordinal);

        internal int Count => _views.Count;

        internal void Sync(IList<BlueprintRecord> records)
        {
            var activeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.BlueprintId)
                    || record.BuildState == BuildState.Built
                    || record.BuildState == BuildState.Cancelled)
                {
                    continue;
                }

                activeIds.Add(record.BlueprintId);
                if (!_views.TryGetValue(record.BlueprintId, out var view) || !IsAlive(view.Root))
                {
                    var source = FindPiecePrefab(record.PieceTypeId);
                    if (source == null)
                    {
                        continue;
                    }

                    view = CreateView(record, source);
                    if (view == null)
                    {
                        continue;
                    }
                    _views[record.BlueprintId] = view;
                }

                UpdateView(view, record);
            }

            var staleIds = new List<string>();
            foreach (var pair in _views)
            {
                if (!activeIds.Contains(pair.Key))
                {
                    staleIds.Add(pair.Key);
                }
            }
            foreach (var staleId in staleIds)
            {
                DestroyView(staleId);
            }
        }

        internal void ShowLocal(BlueprintRecord record, Piece source)
        {
            if (record == null || source == null || string.IsNullOrWhiteSpace(record.BlueprintId))
            {
                return;
            }

            if (_views.TryGetValue(record.BlueprintId, out var oldView))
            {
                DestroyRoot(oldView.Root);
            }

            var view = CreateView(record, source);
            if (view != null)
            {
                _views[record.BlueprintId] = view;
                UpdateView(view, record);
            }
        }

        internal void Clear()
        {
            foreach (var view in _views.Values)
            {
                DestroyRoot(view.Root);
            }
            _views.Clear();
        }

        private static HologramView CreateView(BlueprintRecord record, Piece source)
        {
            try
            {
                var root = CreateVisualOnlyClone(source.transform);
                if (root == null)
                {
                    return null;
                }

                root.name = "CatosBuildHologram_Local_" + record.BlueprintId;
                root.hideFlags = HideFlags.DontSave;
                return new HologramView(root);
            }
            catch
            {
                return null;
            }
        }

        private static GameObject CreateVisualOnlyClone(Transform sourceRoot)
        {
            if (sourceRoot == null)
            {
                return null;
            }

            var root = new GameObject("CatosBuildHologram_VisualRoot");
            var transformMap = new Dictionary<Transform, Transform>();
            transformMap[sourceRoot] = root.transform;
            CopyTransformTree(sourceRoot, root.transform, transformMap);
            CopyRendererTree(sourceRoot, transformMap);
            return root;
        }

        private static void CopyTransformTree(Transform source, Transform target,
            Dictionary<Transform, Transform> transformMap)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;

            for (var index = 0; index < source.childCount; index++)
            {
                var sourceChild = source.GetChild(index);
                var targetChild = new GameObject(sourceChild.name).transform;
                targetChild.SetParent(target, false);
                transformMap[sourceChild] = targetChild;
                CopyTransformTree(sourceChild, targetChild, transformMap);
            }
        }

        private static void CopyRendererTree(Transform sourceRoot,
            Dictionary<Transform, Transform> transformMap)
        {
            foreach (var sourceTransform in transformMap.Keys)
            {
                var targetTransform = transformMap[sourceTransform];
                var meshFilter = sourceTransform.GetComponent<MeshFilter>();
                var meshRenderer = sourceTransform.GetComponent<MeshRenderer>();
                if (meshFilter != null && meshRenderer != null)
                {
                    var targetFilter = targetTransform.gameObject.AddComponent<MeshFilter>();
                    targetFilter.sharedMesh = meshFilter.sharedMesh;
                    var targetRenderer = targetTransform.gameObject.AddComponent<MeshRenderer>();
                    CopyRendererSettings(meshRenderer, targetRenderer);
                }

                var skinnedRenderer = sourceTransform.GetComponent<SkinnedMeshRenderer>();
                if (skinnedRenderer != null)
                {
                    var targetRenderer = targetTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
                    targetRenderer.sharedMesh = skinnedRenderer.sharedMesh;
                    targetRenderer.localBounds = skinnedRenderer.localBounds;
                    targetRenderer.updateWhenOffscreen = skinnedRenderer.updateWhenOffscreen;
                    targetRenderer.rootBone = MapTransform(skinnedRenderer.rootBone, transformMap);
                    var bones = skinnedRenderer.bones;
                    var mappedBones = new Transform[bones.Length];
                    for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        mappedBones[boneIndex] = MapTransform(bones[boneIndex], transformMap);
                    }
                    targetRenderer.bones = mappedBones;
                    CopyRendererSettings(skinnedRenderer, targetRenderer);
                }
            }
        }

        private static Transform MapTransform(Transform source,
            Dictionary<Transform, Transform> transformMap)
        {
            if (source == null || !transformMap.TryGetValue(source, out var mapped))
            {
                return null;
            }
            return mapped;
        }

        private static void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.sharedMaterials = source.sharedMaterials;
            target.enabled = source.enabled;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
        }

        private static void UpdateView(HologramView view, BlueprintRecord record)
        {
            if (!IsAlive(view.Root))
            {
                return;
            }

            var transform = view.Root.transform;
            transform.position = new Vector3(record.Transform.PositionX, record.Transform.PositionY, record.Transform.PositionZ);
            transform.rotation = new Quaternion(record.Transform.RotationX, record.Transform.RotationY,
                record.Transform.RotationZ, record.Transform.RotationW);
            view.SetColor(GetColor(record.SupportState));
        }

        private static Piece FindPiecePrefab(string pieceTypeId)
        {
            if (string.IsNullOrWhiteSpace(pieceTypeId) || ObjectDB.instance == null)
            {
                return null;
            }

            var pieces = ObjectDB.instance.GetAllBuildPieces(false);
            if (pieces == null)
            {
                return null;
            }
            foreach (var piece in pieces)
            {
                if (piece != null && string.Equals(piece.name, pieceTypeId, StringComparison.Ordinal))
                {
                    return piece;
                }
            }
            return null;
        }

        private void DestroyView(string blueprintId)
        {
            if (_views.TryGetValue(blueprintId, out var view))
            {
                DestroyRoot(view.Root);
                _views.Remove(blueprintId);
            }
        }

        private static void DestroyRoot(GameObject root)
        {
            if (IsAlive(root))
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static bool IsAlive(UnityEngine.Object value)
        {
            return value != null;
        }

        private static Color GetColor(SupportState state)
        {
            switch (state)
            {
                case SupportState.Supported:
                    return new Color(0.15f, 1f, 0.2f, 0.45f);
                case SupportState.Unsupported:
                    return new Color(1f, 0.1f, 0.1f, 0.45f);
                case SupportState.Blocked:
                    return new Color(1f, 0.65f, 0.05f, 0.45f);
                case SupportState.Stale:
                case SupportState.Unknown:
                default:
                    return new Color(0.65f, 0.7f, 0.75f, 0.4f);
            }
        }

        private sealed class HologramView
        {
            internal HologramView(GameObject root)
            {
                Root = root;
                PrepareRoot();
            }

            internal GameObject Root { get; }

            internal void SetColor(Color color)
            {
                foreach (var renderer in Root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    try
                    {
                        var material = renderer.material;
                        if (material != null)
                        {
                            material.color = color;
                        }
                    }
                    catch
                    {
                        // One unsupported renderer must not break the view.
                    }
                }
            }

            private void PrepareRoot()
            {
                foreach (var behaviour in Root.GetComponentsInChildren<Behaviour>(true))
                {
                    if (behaviour != null)
                    {
                        behaviour.enabled = false;
                    }
                }
                foreach (var collider in Root.GetComponentsInChildren<Collider>(true))
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
                foreach (var rigidbody in Root.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (rigidbody != null)
                    {
                        rigidbody.isKinematic = true;
                        rigidbody.detectCollisions = false;
                    }
                }
            }
        }
    }
}
