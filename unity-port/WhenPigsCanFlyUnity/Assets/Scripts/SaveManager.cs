using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Persists leaderboard, run codes, and settings using PlayerPrefs or a JSON file.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("Keys")]
        [SerializeField] private string leaderboardKey = "neon-oink-lb";
        [SerializeField] private string firstPersonKey = "neon-oink-fp";
        [SerializeField] private string kidsModeKey = "neon-oink-kids";
        [SerializeField] private string settingsKey = "neon-oink-settings";
        [SerializeField] private int leaderboardSize = 10;

        [System.Serializable]
        public class LeaderboardEntry
        {
            public int score;
            public string code;
            public int level;
            public int chain;
            public long timestamp;
        }

        [System.Serializable]
        private class LeaderboardWrapper
        {
            public List<LeaderboardEntry> entries;
        }

        [System.Serializable]
        public class Settings
        {
            public bool firstPerson;
            public bool kidsMode;
            public float masterVolume = 1f;
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
        }

        /// <summary>
        /// Loads the full leaderboard.
        /// </summary>
        public List<LeaderboardEntry> LoadLeaderboard()
        {
            string json = PlayerPrefs.GetString(leaderboardKey, "");
            if (string.IsNullOrEmpty(json))
                return new List<LeaderboardEntry>();
            return JsonUtility.FromJson<LeaderboardWrapper>(json)?.entries ?? new List<LeaderboardEntry>();
        }

        /// <summary>
        /// Submits a score and trims to the top entries.
        /// </summary>
        public void SaveScore(int score, string code, int level, int chain)
        {
            var entries = LoadLeaderboard();
            entries.Add(new LeaderboardEntry
            {
                score = score,
                code = code,
                level = level,
                chain = chain,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            entries = entries.OrderByDescending(e => e.score).Take(leaderboardSize).ToList();
            PlayerPrefs.SetString(leaderboardKey, JsonUtility.ToJson(new LeaderboardWrapper { entries = entries }));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads persistent settings.
        /// </summary>
        public Settings LoadSettings()
        {
            string json = PlayerPrefs.GetString(settingsKey, "");
            if (string.IsNullOrEmpty(json))
                return new Settings();
            return JsonUtility.FromJson<Settings>(json) ?? new Settings();
        }

        /// <summary>
        /// Saves persistent settings.
        /// </summary>
        public void SaveSettings(Settings settings)
        {
            PlayerPrefs.SetString(settingsKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads the first-person camera preference.
        /// </summary>
        public bool LoadFirstPerson()
        {
            return PlayerPrefs.GetInt(firstPersonKey, 0) == 1;
        }

        /// <summary>
        /// Saves the first-person camera preference.
        /// </summary>
        public void SaveFirstPerson(bool value)
        {
            PlayerPrefs.SetInt(firstPersonKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads the kids-mode preference.
        /// </summary>
        public bool LoadKidsMode()
        {
            return PlayerPrefs.GetInt(kidsModeKey, 0) == 1;
        }

        /// <summary>
        /// Saves the kids-mode preference.
        /// </summary>
        public void SaveKidsMode(bool value)
        {
            PlayerPrefs.SetInt(kidsModeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Start() { }
        private void Update() { }
    }
}
