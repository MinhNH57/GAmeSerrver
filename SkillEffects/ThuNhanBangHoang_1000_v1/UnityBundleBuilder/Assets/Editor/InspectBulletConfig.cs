using System;
using System.Linq;
using FogTeam.KiemThe.Entities.Config.ScriptableData;
using UnityEditor;
using UnityEngine;

public static class InspectBulletConfig
{
    public static void InspectSkillBundles()
    {
        InspectBundle(
            @"C:\Users\minh\AppData\LocalLow\ADNX\Kiếm Vương 1\Android\Data\Resources\Skill\1000\AnimFile2.unity3d",
            "AnimFile2");
        InspectBundle(
            @"C:\Users\minh\AppData\LocalLow\ADNX\Kiếm Vương 1\Android\Data\Resources\Skill\1000\AnimFile4.unity3d",
            "AnimFile4");
    }

    private static void InspectBundle(string path, string assetName)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.Log($"SKILL_BUNDLE_RESULT: {assetName}; Bundle=NULL; Path={path}");
            return;
        }

        try
        {
            var sprites = bundle.LoadAssetWithSubAssets<Sprite>(assetName);
            Debug.Log(
                $"SKILL_BUNDLE_RESULT: {assetName}; Bundle=OK; Sprites={sprites.Length}; " +
                $"Names={string.Join(",", sprites.Select(sprite => sprite.name))}");
        }
        finally
        {
            bundle.Unload(true);
        }
    }

    public static void Run()
    {
        const string path = @"C:\Users\minh\OneDrive\Desktop\kv1-client\Temp\config-localLow-decrypted.unity3d";
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
            throw new InvalidOperationException("Cannot load decrypted config bundle: " + path);

        try
        {
            var actionSet = bundle.LoadAllAssets<BulletActionSetSO>().FirstOrDefault();
            if (actionSet == null)
                throw new InvalidOperationException("BulletActionSetSO was not found in config bundle.");

            var item = actionSet.data?.FirstOrDefault(entry => entry.ResID == 1000);
            if (item == null)
            {
                Debug.Log("BULLET_1000_RESULT: MISSING");
                return;
            }

            Debug.Log(
                $"BULLET_1000_RESULT: ResID={item.ResID}; Name={item.Name}; BundleDir={item.BundleDir}; " +
                $"Fly={item.HasFlyAction}; Fade={item.HasFadeOutAction}; Explode={item.HasExplodeAction}; " +
                $"Use16Dir={item.Use16Dir}; AutoRotate={item.AutoRotate}; Trail={item.UseTrailEffect}; " +
                $"TrailDuration={item.TrailDuration}; TrailPeriod={item.TrailPeriod}");
        }
        finally
        {
            bundle.Unload(true);
        }
    }
}
