using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;

// Rebuildable skinned LODs. The imported source mesh and skeleton stay untouched.
public static class EnemyArtOptimizer
{
    private const string Source = "Assets/Game/Art/Characters/Enemy Humanoid.fbx";
    private const string Prefab = "Assets/Game/Prefabs/Enemy.prefab";
    private const string Folder = "Assets/Game/Art/Characters/Optimized";

    [MenuItem("Skyloft/Optimization/Rebuild Enemy LODs")]
    public static void Rebuild()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(Source);
        if (importer == null) throw new InvalidOperationException("Enemy source importer missing");
        byte[] sourceMeta = File.ReadAllBytes(Source + ".meta");
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source)
                .GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            if (source == null || source.boneWeights.Length != source.vertexCount)
                throw new InvalidOperationException("Expected a readable, skinned enemy mesh");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Game/Art/Characters", "Optimized");

            Mesh medium = SaveSimplified(source, 0.35f, "Enemy LOD1.asset");
            Mesh low = SaveSimplified(source, 0.15f, "Enemy LOD2.asset");

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var original = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (original == null || original.sharedMesh == null)
                    throw new InvalidOperationException("Enemy prefab renderer missing");
                var parent = original.transform.parent;
                foreach (string name in new[] { "Enemy LOD1", "Enemy LOD2" })
                {
                    var old = parent.Find(name);
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }

                SkinnedMeshRenderer MakeRenderer(string name, Mesh mesh)
                {
                    var child = new GameObject(name);
                    child.transform.SetParent(parent, false);
                    var renderer = child.AddComponent<SkinnedMeshRenderer>();
                    EditorUtility.CopySerialized(original, renderer);
                    renderer.sharedMesh = mesh;
                    renderer.rootBone = original.rootBone;
                    renderer.bones = original.bones;
                    return renderer;
                }

                var midRenderer = MakeRenderer("Enemy LOD1", medium);
                var lowRenderer = MakeRenderer("Enemy LOD2", low);
                var group = parent.GetComponent<LODGroup>();
                if (group == null) group = parent.gameObject.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(new[]
                {
                    new LOD(0.11f, new Renderer[] { original }),
                    new LOD(0.055f, new Renderer[] { midRenderer }),
                    new LOD(0.01f, new Renderer[] { lowRenderer })
                });
                group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Enemy LODs built: {source.triangles.Length / 3} -> {medium.triangles.Length / 3} -> {low.triangles.Length / 3} triangles");
        }
        finally
        {
            if (!wasReadable)
            {
                // SaveAndReimport can rewrite unrelated FBX humanoid mapping fields.
                // Restore the exact original importer data after generating the assets.
                File.WriteAllBytes(Source + ".meta", sourceMeta);
                AssetDatabase.ImportAsset(Source, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static Mesh SaveSimplified(Mesh source, float quality, string name)
    {
        var simplifier = new MeshSimplifier(source);
        simplifier.SimplifyMesh(quality);
        var mesh = simplifier.ToMesh();
        mesh.name = name.Replace(".asset", "");
        if (mesh.vertexCount == 0 || mesh.boneWeights.Length != mesh.vertexCount ||
            mesh.bindposes.Length != source.bindposes.Length || mesh.subMeshCount != source.subMeshCount)
            throw new InvalidOperationException("Simplified mesh lost skinning or material sections");
        string path = Folder + "/" + name;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
            EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }
}
