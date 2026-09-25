using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only comparison scene. It never changes Game.unity or the runtime model assignments.
public static class ModelReviewSceneBuilder
{
    private const string ReviewScene = "Assets/Game/Scenes/ModelOptimizationReview.unity";
    private const string Radical = "Assets/Game/Art/Characters/Optimized/Radical/";
    private const string Output = "Documentation/Benchmark/Optimization/ModelReview";

    [MenuItem("Skyloft/Optimization/Create Model Review Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play Mode and save the current scene first.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Model Optimization Review";

        AddRow(scene, "Enemy", 0f, "Assets/Game/Art/Characters/Enemy Humanoid.fbx", 1f);
        AddRow(scene, "Player", 4f, "Assets/Game/Art/Characters/Player Humanoid.fbx", 1f);
        AddRow(scene, "Rifle", 8f, "Assets/Art Source/Original/rifle.fbx", 3f);

        var lightObject = new GameObject("Review Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        var cameraObject = new GameObject("Review Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.13f, 0.16f, 0.2f);
        cameraObject.transform.position = new Vector3(0f, 10f, -11f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 4f));

        EditorSceneManager.SaveScene(scene, ReviewScene);
        Directory.CreateDirectory(Output);
        CaptureRow(scene, camera, "Enemy", 0f, "enemy.png");
        CaptureRow(scene, camera, "Player", 4f, "player.png");
        CaptureRow(scene, camera, "Rifle", 8f, "rifle.png");
        camera.orthographicSize = 8f;
        cameraObject.transform.position = new Vector3(0f, 10f, -11f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 4f));
        EditorSceneManager.SaveScene(scene, ReviewScene);
        Debug.Log("Model comparison scene and screenshots created: " + ReviewScene);
    }

    private static void AddRow(Scene scene, string kind, float z, string modelPath, float scale)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (asset == null) throw new InvalidOperationException("Source model missing: " + modelPath);
        string[] levels = { "Current", "Radical 1", "Radical 2" };
        for (int level = 0; level < 3; level++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            instance.name = kind + " - " + levels[level];
            instance.transform.position = new Vector3((level - 1) * 3.5f, 0f, z);
            instance.transform.localScale *= scale;
            if (kind == "Rifle") instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            ApplyMesh(kind, level, instance);

            var label = new GameObject(instance.name + " Label");
            SceneManager.MoveGameObjectToScene(label, scene);
            var text = label.AddComponent<TextMesh>();
            text.text = levels[level];
            text.fontSize = 24;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.white;
            label.transform.position = new Vector3((level - 1) * 3.5f, kind == "Rifle" ? 1.2f : 2.25f, z);
        }
    }

    private static void ApplyMesh(string kind, int level, GameObject instance)
    {
        if (level == 0)
        {
            if (kind == "Enemy")
                FindSkin(instance, "Ch30").sharedMesh =
                    AssetDatabase.LoadAssetAtPath<Mesh>(
                        "Assets/Game/Art/Characters/Optimized/Enemy LOD2.asset");
        }
        else if (kind == "Enemy")
            FindSkin(instance, "Ch30").sharedMesh = Load(kind + " Radical " + level);
        else if (kind == "Player")
        {
            FindSkin(instance, "Soldier_body").sharedMesh = Load("Player Body Radical " + level);
            FindSkin(instance, "Soldier_head").sharedMesh = Load("Player Head Radical " + level);
        }
        else
        {
            FindFilter(instance, "body").sharedMesh = Load("Rifle Body Radical " + level);
            FindFilter(instance, "mag").sharedMesh = Load("Rifle Magazine Radical " + level);
        }

        if (kind == "Rifle")
        {
            var gameMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Source/SAR80.mat");
            if (gameMaterial == null) throw new InvalidOperationException("Game rifle material missing");
            FindFilter(instance, "body").GetComponent<MeshRenderer>().sharedMaterial = gameMaterial;
            FindFilter(instance, "mag").GetComponent<MeshRenderer>().sharedMaterial = gameMaterial;
        }

        if (kind == "Enemy")
        {
            var live = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemy.prefab")
                .GetComponentInChildren<SkinnedMeshRenderer>(true);
            FindSkin(instance, "Ch30").sharedMaterials = live.sharedMaterials;
        }
    }

    private static Mesh Load(string name)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Radical + name + ".asset");
        if (mesh == null) throw new InvalidOperationException("Review mesh missing: " + name);
        return mesh;
    }

    private static SkinnedMeshRenderer FindSkin(GameObject go, string name) =>
        go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer => renderer.name == name);

    private static MeshFilter FindFilter(GameObject go, string name) =>
        go.GetComponentsInChildren<MeshFilter>(true).Single(filter => filter.name == name);

    private static void CaptureRow(Scene scene, Camera camera, string kind, float z, string file)
    {
        var roots = scene.GetRootGameObjects();
        bool[] active = roots.Select(root => root.activeSelf).ToArray();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].name.StartsWith("Enemy -") || roots[i].name.StartsWith("Player -") ||
                roots[i].name.StartsWith("Rifle -"))
                roots[i].SetActive(roots[i].name.StartsWith(kind + " -"));

        camera.orthographicSize = 2.2f;
        camera.transform.position = new Vector3(0f, 3f, z - 7f);
        camera.transform.LookAt(new Vector3(0f, 0.85f, z));
        var target = new RenderTexture(1800, 700, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1800, 700, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1800, 700), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output, file), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            for (int i = 0; i < roots.Length; i++) roots[i].SetActive(active[i]);
        }
    }
}
