﻿using SCHIZO.TriInspector.Attributes;
using TriInspector;
using UnityEngine;

[DeclareUnexploredGroup]
public class LiveMixin : MonoBehaviour
{
    [Required, InlineEditor] public LiveMixinData data;
    public float health;

    [UnexploredGroup] public FMODAsset damageSound;
    [UnexploredGroup] public FMODAsset deathSound;
    [UnexploredGroup] public FMODAsset damageClip;
    [UnexploredGroup] public FMODAsset deathClip;
}
