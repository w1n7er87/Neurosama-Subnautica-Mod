using Story;
using UWE;

namespace Immersion.Trackers;

public sealed class PlayingVO : Tracker
{
    internal const string ENDPOINT = "mute";
    // SN ency logs don't use subtitles
    private SoundQueue Sounds => PDASounds.queue;
    // some specific BZ cutscenes don't use SoundQueue
    // now... technically this won't work if subtitles are off; however: just don't disable subtitles
    private bool UseSubs;
    public bool IsPlaying => (IsPlayingVO || UseSubs && IsShowingSubtitles) && !FreezeTime.HasFreezers();
    public bool IsShowingSubtitles => Subtitles.HasQueued();
    public bool IsPlayingVO => Sounds is { _current.host: SoundHost.Encyclopedia or SoundHost.Log or SoundHost.Realtime };

    private bool _wasPlaying;

    private void Start()
    {
        Nautilus.Utility.SaveUtils.RegisterOnQuitEvent(OnQuit);
        StoryGoals.OnStoryGoalCompleted += OnGoalComplete;
    }
    private void Destroy()
    {
        Nautilus.Utility.SaveUtils.UnregisterOnQuitEvent(OnQuit);
        StoryGoals.OnStoryGoalCompleted -= OnGoalComplete;
    }
    private void OnQuit()
    {
        UseSubs = false;
    }

    private void FixedUpdate()
    {
        bool isPlaying = IsPlaying;
        if (isPlaying == _wasPlaying) return;

        Notify(isPlaying);
        _wasPlaying = isPlaying;
    }

    private void OnDisable()
    {
        if (_wasPlaying) Notify(false);
        _wasPlaying = false;
    }

    internal void Notify(bool isPlaying)
    {
        Send(ENDPOINT, isPlaying);
    }

    public void OnGoalComplete(string key)
    {
        /// <see cref="IsShowingSubtitles" />
        if (StoryGoal.Equals(key, "OnEndGameBegin"))
            UseSubs = true;
    }
}
