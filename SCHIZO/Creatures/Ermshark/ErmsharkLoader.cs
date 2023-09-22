﻿using System.Collections.Generic;
using ECCLibrary;
using Nautilus.Handlers;
using Nautilus.Utility;
using SCHIZO.Attributes;
using SCHIZO.Extensions;
using SCHIZO.Helpers;
using SCHIZO.Resources;
using SCHIZO.Sounds;
using SCHIZO.Unity.Creatures;
using UnityEngine;

namespace SCHIZO.Creatures.Ermshark;

[LoadMethod]
public static class ErmsharkLoader
{
    public static readonly SoundCollection AmbientSounds = SoundCollection.Create("ermshark/ambient", AudioUtils.BusPaths.UnderwaterCreatures);
    public static readonly SoundCollection AttackSounds = SoundCollection.Create("ermshark/attack", AudioUtils.BusPaths.UnderwaterCreatures);
    public static readonly SoundCollection SplitSounds = SoundCollection.Create("ermshark/split", AudioUtils.BusPaths.UnderwaterCreatures);

    public static GameObject Prefab;

    [LoadMethod]
    private static void Load()
    {
        CustomCreatureData data = ResourceManager.AssetBundle.LoadAssetSafe<CustomCreatureData>("Ermshark data");

        ErmsharkPrefab ermshark = new(ModItems.Ermshark);
        ermshark.Register();

        string encyPath = IS_BELOWZERO ? "Lifeforms/Fauna/Carnivores" : "Lifeforms/Fauna/Sharks";

        CreatureDataUtils.AddCreaturePDAEncyclopediaEntry(ermshark, encyPath, "Ermshark", data.databankText.text, 5, data.databankTexture, data.unlockSprite);

        List<LootDistributionData.BiomeData> biomes = new();
        foreach (BiomeType biome in BiomeHelpers.GetOpenWaterBiomes())
        {
            biomes.Add(new LootDistributionData.BiomeData { biome = biome, count = 1, probability = 0.005f });
        }
        LootDistributionHandler.AddLootDistributionData(ermshark.PrefabInfo.ClassID, biomes.ToArray());
    }
}
