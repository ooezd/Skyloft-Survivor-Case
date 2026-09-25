using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Produces separate mobile texture/material assets; original image files and importers stay intact.
public static class EnemyTextureOptimizer
{
    private const string SourceFolder = "Assets/Art Source/Original/enemy.fbm/";
    private const string SourceModel = "Assets/Art Source/Original/enemy.fbx";
    private const string OutputFolder = "Assets/Game/Art/Characters/Optimized";
    private const string Prefab = "Assets/Game/Prefabs/Enemy.prefab";
    private static readonly string[] Textures =
    {
        "Ch30_1001_Diffuse.png", "Ch30_1002_Diffuse.png",
        "Ch30_1001_Normal.png", "Ch30_1002_Normal.png"
    };

    [MenuItem("Skyloft/Optimization/Create Separate Enemy Mobile Textures")]
    public static void CreateVariants()
    {
        string folder = OutputFolder + "/Textures";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(OutputFolder, "Textures");

        foreach (string name in Textures)
        {
            string path = folder + "/" + name;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null &&
                !AssetDatabase.CopyAsset(SourceFolder + name, path))
                throw new InvalidOperationException("Could not copy texture " + name);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Copied texture importer missing: " + path);
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        string materialFolder = OutputFolder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(OutputFolder, "Materials");
        var sources = AssetDatabase.LoadAllAssetsAtPath(SourceModel).OfType<Material>().ToArray();
        Material MakeMaterial(string sourceName, string uvSet)
        {
            var original = sources.Single(m => m.name == sourceName);
            string path = materialFolder + "/" + sourceName + " Mobile.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(original) { name = sourceName + " Mobile" };
                AssetDatabase.CreateAsset(material, path);
            }
            else EditorUtility.CopySerialized(original, material);
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Ch30_" + uvSet + "_Diffuse.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Ch30_" + uvSet + "_Normal.png");
            material.SetTexture("_BaseMap", diffuse);
            material.SetTexture("_MainTex", diffuse);
            material.SetTexture("_BumpMap", normal);
            EditorUtility.SetDirty(material);
            return material;
        }

        var mobile1002 = MakeMaterial("Ch30_Body1", "1002");
        var mobile1001 = MakeMaterial("Ch30_Body", "1001");
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.sharedMaterials = new[] { mobile1002, mobile1001 };
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("Created separate mobile enemy textures/materials; Android max 1024");
    }
}
