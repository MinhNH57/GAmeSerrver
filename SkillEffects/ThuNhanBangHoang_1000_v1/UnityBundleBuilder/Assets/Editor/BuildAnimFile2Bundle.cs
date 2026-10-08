using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildAnimFile2Bundle
{
    private const string SheetPath = "Assets/Skill1000/AnimFile2.png";
    private const string BundleName = "AnimFile2.unity3d";

    public static void BuildAll()
    {
        ConfigureSpriteSheet();

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "Built"));
        string windowsOutput = Path.Combine(outputRoot, "Windows", "Data", "Resources", "Skill", "1000");
        string androidOutput = Path.Combine(outputRoot, "Android", "Data", "Resources", "Skill", "1000");

        BuildForTarget(windowsOutput, BuildTarget.StandaloneWindows64);
        ValidateWindowsBundle(Path.Combine(windowsOutput, BundleName));
        BuildForTarget(androidOutput, BuildTarget.Android);

        Debug.Log($"BUILD_SKILL_1000_OK Windows={Path.Combine(windowsOutput, BundleName)} Android={Path.Combine(androidOutput, BundleName)}");
    }

    private static void ConfigureSpriteSheet()
    {
        AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"TextureImporter not found: {SheetPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed;

        const int columns = 4;
        const int rows = 4;
        const int sheetWidth = 1536;
        const int sheetHeight = 1024;
        const int cellWidth = sheetWidth / columns;
        const int cellHeight = sheetHeight / rows;

        var sprites = new SpriteMetaData[columns * rows];
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                sprites[index] = new SpriteMetaData
                {
                    name = $"AnimFile2_{index:D2}",
                    rect = new Rect(
                        column * cellWidth,
                        sheetHeight - (row + 1) * cellHeight,
                        cellWidth,
                        cellHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                };
            }
        }

#pragma warning disable 0618
        importer.spritesheet = sprites;
#pragma warning restore 0618
        importer.SaveAndReimport();

        var importedSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(SheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
        if (importedSprites.Length != 16)
            throw new InvalidOperationException($"Expected 16 imported sprites, found {importedSprites.Length}.");
    }

    private static void BuildForTarget(string outputDirectory, BuildTarget target)
    {
        Directory.CreateDirectory(outputDirectory);
        var buildMap = new[]
        {
            new AssetBundleBuild
            {
                assetBundleName = BundleName,
                assetNames = new[] { SheetPath },
            },
        };

        var manifest = BuildPipeline.BuildAssetBundles(
            outputDirectory,
            buildMap,
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            target);

        if (manifest == null)
            throw new InvalidOperationException($"BuildAssetBundles returned null for {target}.");

        string producedFile = Directory.GetFiles(outputDirectory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), BundleName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(producedFile))
            throw new FileNotFoundException($"Bundle was not produced for {target}: {BundleName}");

        string exactPath = Path.Combine(outputDirectory, BundleName);
        if (!string.Equals(producedFile, exactPath, StringComparison.Ordinal))
        {
            string temporaryCaseFixPath = exactPath + ".casefix";
            if (File.Exists(temporaryCaseFixPath))
                File.Delete(temporaryCaseFixPath);
            File.Move(producedFile, temporaryCaseFixPath);
            File.Move(temporaryCaseFixPath, exactPath);
        }

        if (new FileInfo(exactPath).Length == 0)
            throw new InvalidOperationException($"Built bundle is empty: {exactPath}");
    }

    private static void ValidateWindowsBundle(string bundlePath)
    {
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
            throw new InvalidOperationException($"Unity cannot reopen Windows bundle: {bundlePath}");

        try
        {
            string[] assetNames = bundle.GetAllAssetNames();
            if (assetNames.Length != 1 || !assetNames[0].EndsWith("/animfile2.png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected assets in bundle: " + string.Join(", ", assetNames));

            Sprite[] sprites = bundle.LoadAssetWithSubAssets<Sprite>("AnimFile2");
            if (sprites == null || sprites.Length != 16)
                throw new InvalidOperationException($"Expected 16 Sprite sub-assets, found {sprites?.Length ?? 0}.");

            string[] expected = Enumerable.Range(0, 16).Select(index => $"AnimFile2_{index:D2}").ToArray();
            string[] actual = sprites.Select(sprite => sprite.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException("Unexpected sprite names: " + string.Join(", ", actual));

            Debug.Log("VALIDATE_SKILL_1000_OK asset=AnimFile2 sprites=16 names=" + string.Join(",", actual));
        }
        finally
        {
            bundle.Unload(true);
        }
    }
}
