using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;

// Generates separate mesh assets; source FBXs stay intact. Runtime wiring is a separate command.
public static class RadicalMeshVariants
{
    private const string Output = "Assets/Game/Art/Characters/Optimized/Radical";

    [MenuItem("Skyloft/Optimization/Generate Radical Model Variants")]
    public static void Generate()
    {
        EnsureFolder();
        GenerateModel("Assets/Game/Art/Characters/Enemy Humanoid.fbx", "Ch30", "Enemy", 0.075f, 0.03f, 0.015f);
        GenerateModel("Assets/Game/Art/Characters/Player Humanoid.fbx", "Soldier_body", "Player Body", 0.25f, 0.10f);
        GenerateModel("Assets/Game/Art/Characters/Player Humanoid.fbx", "Soldier_head", "Player Head", 0.25f, 0.10f);
        // Thin barrel geometry collapses below roughly 35% with this simplifier.
        GenerateModel("Assets/Art Source/Original/rifle.fbx", "body", "Rifle Body", 0.50f, 0.35f);
        GenerateModel("Assets/Art Source/Original/rifle.fbx", "mag", "Rifle Magazine", 0.50f, 0.35f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Skyloft/Optimization/Generate Enemy Radical 3")]
    public static void GenerateEnemyRadical3()
    {
        EnsureFolder();
        GenerateModel("Assets/Game/Art/Characters/Enemy Humanoid.fbx", "Ch30", "Enemy", 0.075f, 0.03f, 0.015f);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder()
    {
        const string parent = "Assets/Game/Art/Characters/Optimized";
        if (!AssetDatabase.IsValidFolder(parent))
            throw new InvalidOperationException("Optimized character folder is missing");
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder(parent, "Radical");
    }

    private static void GenerateModel(string path, string meshName, string label, float first, float second,
        float third = -1f)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Model importer missing: " + path);
        byte[] originalMeta = File.ReadAllBytes(path + ".meta");
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        try
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
                .SingleOrDefault(mesh => mesh.name == meshName);
            if (source == null) throw new InvalidOperationException("Mesh missing: " + path + "/" + meshName);
            int originalTriangles = source.triangles.Length / 3;
            var level1 = SaveSimplified(source, first, label + " Radical 1.asset");
            var level2 = SaveSimplified(source, second, label + " Radical 2.asset");
            int tris1 = level1.triangles.Length / 3;
            int tris2 = level2.triangles.Length / 3;
            if (tris1 >= originalTriangles || tris2 >= tris1)
                throw new InvalidOperationException("Variants are not progressively simpler: " + label);
            if (third > 0f)
            {
                var level3 = SaveSimplified(source, third, label + " Radical 3.asset");
                int tris3 = level3.triangles.Length / 3;
                if (tris3 >= tris2) throw new InvalidOperationException("Third variant is not simpler: " + label);
                Debug.Log(label + ": " + originalTriangles + " -> " + tris1 + " -> " + tris2 +
                    " -> " + tris3 + " triangles");
            }
            else Debug.Log(label + ": " + originalTriangles + " -> " + tris1 + " -> " + tris2 + " triangles");
        }
        finally
        {
            if (!wasReadable)
            {
                // SaveAndReimport can rewrite unrelated humanoid mapping fields. Restore exact source metadata.
                File.WriteAllBytes(path + ".meta", originalMeta);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static Mesh SaveSimplified(Mesh source, float quality, string filename)
    {
        var simplifier = new MeshSimplifier(source);
        simplifier.SimplifyMesh(quality);
        var reduced = simplifier.ToMesh();
        reduced.name = filename.Replace(".asset", "");
        if (reduced.vertexCount == 0 || reduced.subMeshCount != source.subMeshCount ||
            reduced.uv.Length != reduced.vertexCount ||
            reduced.boneWeights.Length != source.boneWeights.Length * reduced.vertexCount / source.vertexCount ||
            reduced.bindposes.Length != source.bindposes.Length)
            throw new InvalidOperationException("Variant lost UV, skinning, or material sections: " + filename);

        string path = Output + "/" + filename;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(reduced, path);
            return reduced;
        }
        EditorUtility.CopySerialized(reduced, existing);
        UnityEngine.Object.DestroyImmediate(reduced);
        EditorUtility.SetDirty(existing);
        return existing;
    }
}
