using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI logoVersionLabel;
    public TextMeshProUGUI localVersionLabel;
    public TextMeshProUGUI remoteVersionLabel;
    public GameObject updateNotification;
    public TMP_Dropdown branchDropdown;

    [Header("GitHub Info")]
    public string githubOwner = "VirtuaLabs-online";
    public string githubRepo = "HNode";

    private string localVersion; // numeric only
    private string remoteVersion;
    private string remoteUrl;

    private int savedIndex = 0;

    void Start()
    {
        logoVersionLabel.gameObject.SetActive(false);
        savedIndex = PlayerPrefs.GetInt("SelectedBranch", 0);
        branchDropdown.value = savedIndex;
        branchDropdown.onValueChanged.Invoke(savedIndex);
        branchDropdown.onValueChanged.AddListener(OnBranchChanged);

        localVersion = Application.version;

        Invoke(nameof(FetchInitialRelease), 1f); // calls after 1 second
    }

    private void FetchInitialRelease()
    {
        ChangeReleaseBranch(savedIndex);
    }

    void OnBranchChanged(int index)
    {
        PlayerPrefs.SetInt("SelectedBranch", index);
        PlayerPrefs.Save();
        ChangeReleaseBranch(index);
    }

    private void ChangeReleaseBranch(int releaseBranch)
    {
        if (releaseBranch != 0 && releaseBranch != 1) return;

        logoVersionLabel.text = "HNode ver " + localVersion;
        logoVersionLabel.gameObject.SetActive(true);

        FetchLatestReleaseSync(releaseBranch); // blocking call
    }

    private void FetchLatestReleaseSync(int branchIndex)
    {
        string apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("User-Agent", "UnityUpdateManager");
            var operation = request.SendWebRequest();

            while (!operation.isDone) { } // block thread

            if (request.result != UnityWebRequest.Result.Success) return;

            try
            {
                var releases = JsonHelper.FromJson<GitHubRelease>(request.downloadHandler.text);

                GitHubRelease chosenRelease = null;
                foreach (var r in releases)
                {
                    if (branchIndex == 0 && !r.prerelease)
                    {
                        chosenRelease = r;
                        break;
                    }
                    if (branchIndex == 1)
                    {
                        chosenRelease = r;
                        break;
                    }
                }

                if (chosenRelease != null)
                {
                    remoteVersion = chosenRelease.tag_name;
                    remoteUrl = chosenRelease.html_url;
                    VersionLookup(branchIndex);
                }
            }
            catch { }
        }
    }

    private void VersionLookup(int branchIndex)
    {
        var localSem = new SemanticVersion(localVersion);
        var remoteSem = new SemanticVersion(remoteVersion);

        bool newerAvailable = false;

        if (branchIndex == 0)
        {
            if (!remoteSem.IsPreRelease && remoteSem > localSem)
                newerAvailable = true;
        }
        else
        {
            if (remoteSem > localSem)
                newerAvailable = true;
        }

        if (newerAvailable)
        {
            localVersionLabel.text = "Current Version: " + localVersion;
            remoteVersionLabel.text = "New Version: " + remoteVersion;
            updateNotification.SetActive(true);

            Debug.LogWarning($"Update available! Local: {localVersion}, Remote: {remoteVersion}");
        }
        else
        {
            updateNotification.SetActive(false);
            Debug.LogWarning("No update available");
        }
    }

    // ---------------- Semantic Version Class ----------------
    private class SemanticVersion : IComparable<SemanticVersion>
    {
        public Version Numeric { get; private set; }
        public string PreRelease { get; private set; }
        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        public SemanticVersion(string raw)
        {
            if (raw.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(1);

            var match = Regex.Match(raw, @"^(\d+(\.\d+){0,2})(?:-([\w\d]+))?$");
            if (match.Success)
            {
                Numeric = new Version(match.Groups[1].Value);
                PreRelease = match.Groups[3].Value;
            }
            else
            {
                Numeric = new Version(0, 0, 0);
                PreRelease = null;
            }
        }

        public int CompareTo(SemanticVersion other)
        {
            int numComp = Numeric.CompareTo(other.Numeric);
            if (numComp != 0) return numComp;

            if (IsPreRelease && !other.IsPreRelease) return -1;
            if (!IsPreRelease && other.IsPreRelease) return 1;

            return string.Compare(PreRelease, other.PreRelease, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator >(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) > 0;
        public static bool operator <(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) < 0;
        public static bool operator >=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) >= 0;
        public static bool operator <=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) <= 0;
    }
}

// ---------------- JSON Helper ----------------
[Serializable]
public class GitHubRelease
{
    public string tag_name;
    public bool prerelease;
    public string html_url;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
