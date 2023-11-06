using System;
using System.Collections;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Nautilus.Handlers;
using Nautilus.Utility;
using SCHIZO.DataStructures;
using SCHIZO.Resources;
using SCHIZO.Sounds.Collections;
using UnityEngine;
using UWE;

namespace SCHIZO.Sounds;

public sealed class FMODSoundCollection
{
    private enum VCA
    {
        Master,
        Music,
        Voice,
        Ambient
    }

    private static readonly Dictionary<string, FMODSoundCollection> _cache = new();

    private readonly string _busName;
    private readonly List<string> _sounds = new();
    private readonly List<Channel> _channels = new();

    private bool _ready;
    private RandomList<string> _randomSounds;
    private List<Coroutine> _runningCoroutines;

    #region Public API

    public float LastPlay { get; private set; } = -1;

    public void CancelAllDelayed()
    {
        if (!Initialize()) return;

        foreach (Coroutine c in _runningCoroutines)
        {
            CoroutineHost.StopCoroutine(c);
        }

        _runningCoroutines.Clear();
    }

    public void Stop()
    {
        if (!Initialize()) return;
        if (Assets.Options_DisableAllSounds.Value) return;

        foreach (Channel channel in _channels)
        {
            channel.stop();
        }
        ClearChannelsCache();
    }

    public void PlayRandom2D(float delay = 0) => Play(-1, null, delay);

    public void PlayRandom3D(FMOD_CustomEmitter emitter, float delay = 0)
    {
        if (!emitter) throw new ArgumentNullException(nameof(emitter));
        Play(-1, emitter, delay);
    }

    public void Play2D(int index, float delay = 0) => Play(index, null, delay);

    public void Play3D(int index, FMOD_CustomEmitter emitter, float delay = 0)
    {
        if (!emitter) throw new ArgumentNullException(nameof(emitter));
        Play(index, emitter, delay);
    }

    #endregion

    #region Initialization

    public static FMODSoundCollection For(SoundCollection soundCollection, string bus)
    {
        int instanceId = soundCollection.GetInstanceID();

        if (_cache.TryGetValue(instanceId + bus, out FMODSoundCollection cached)) return cached;
        return _cache[instanceId + bus] = new FMODSoundCollection(soundCollection, bus);
    }

    private FMODSoundCollection(SoundCollection soundCollection, string bus)
    {
        _busName = bus;
        CoroutineHost.StartCoroutine(LoadSounds(soundCollection));
    }

    private IEnumerator LoadSounds(SoundCollection soundCollection)
    {
        Bus bus = RuntimeManager.GetBus(_busName);

        foreach (AudioClip audioClip in soundCollection.GetSounds())
        {
            string id = Guid.NewGuid().ToString();
            RegisterSound(id, audioClip, bus);
            _sounds.Add(id);

            yield return null;
        }

        _ready = true;
    }

    private bool Initialize()
    {
        if (!_ready) return false;
        if (_randomSounds is {Count: > 0}) return true;

        _randomSounds = new RandomList<string>();
        _runningCoroutines = new List<Coroutine>();
        _randomSounds.AddRange(_sounds);

        return true;
    }

    private void RegisterSound(string id, AudioClip audioClip, Bus bus)
    {
        Sound s = CustomSoundHandler.RegisterCustomSound(id, audioClip, bus, AudioUtils.StandardSoundModes_3D);
        bus.unlockChannelGroup();
        s.set3DMinMaxDistance(1, 30);
    }

    #endregion

    private void StartSoundCoroutine(IEnumerator coroutine)
    {
        _runningCoroutines.Add(CoroutineHost.StartCoroutine(coroutine));
    }

    private void Play(int index, FMOD_CustomEmitter emitter, float delay = 0)
    {
        if (!Initialize()) return;
        if (Assets.Options_DisableAllSounds.Value) return;

        if (delay <= 0)
        {
            PlaySound(index, emitter);
            return;
        }

        StartSoundCoroutine(PlayWithDelay(delay));
        return;

        IEnumerator PlayWithDelay(float del)
        {
            yield return new WaitForSeconds(del);
            PlaySound(index, emitter);
        }
    }

    private void PlaySound(int index, FMOD_CustomEmitter emitter = null)
    {
        LastPlay = Time.time;

        string sound = index == -1 ? _sounds.GetRandom() : _sounds[index];

        if (emitter)
        {
            emitter.SetAsset(AudioUtils.GetFmodAsset(sound));
            emitter.Play();
        }
        else
        {
            CustomSoundHandler.TryPlayCustomSound(sound, out Channel channel);
            channel.setVolume(GetVolumeFor(GetVCAForBus(_busName)));
            channel.set3DLevel(0);

            ClearChannelsCache();
            _channels.Add(channel);
        }
    }

    private void ClearChannelsCache()
    {
        _channels.RemoveAll(c =>
        {
            RESULT result = c.isPlaying(out bool isPlaying);
            return result != RESULT.OK || !isPlaying;
        });
    }

    private static VCA GetVCAForBus(string bus)
    {
        if (bus.StartsWith("bus:/master/Music")) return VCA.Music;
        if (bus.StartsWith("bus:/master/SFX_for_pause/PDA_pause/all/all voice")) return VCA.Voice;
        if (bus.StartsWith("bus:/master/SFX_for_pause/PDA_pause/all/SFX")) return VCA.Ambient;
        return VCA.Master;
    }

    private static float GetVolumeFor(VCA vca)
    {
        return vca switch
        {
            VCA.Music => SoundSystem.masterVolume * SoundSystem.musicVolume,
            VCA.Voice => SoundSystem.masterVolume * SoundSystem.voiceVolume,
            VCA.Ambient => SoundSystem.masterVolume * SoundSystem.ambientVolume,
            _ => SoundSystem.masterVolume,
        };
    }
}
