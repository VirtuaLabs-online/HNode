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

    private string localVersion = Application.version; // numeric only
    private string remoteVersion;
    private string remoteUrl;

    void Start()
    {
        // Load saved branch selection
        int savedIndex = PlayerPrefs.GetInt("SelectedBranch", 0);
        branchDropdown.value = savedIndex;
        branchDropdown.onValueChanged.Invoke(savedIndex);
        branchDropdown.onValueChanged.AddListener(OnBranchChanged);

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
        if (releaseBranch == 0)
        {
            Debug.Log("Switched Target to stable");
        }
        else if (releaseBranch == 1)
        {
            Debug.Log("Switched Target to pre-release");
        }
        else
        {
            Debug.LogWarning("Unknown branch index");
            return;
        }

        logoVersionLabel.text = "HNode ver " + localVersion;
        StartCoroutine(FetchLatestRelease(releaseBranch));
    }

    private System.Collections.IEnumerator FetchLatestRelease(int branchIndex)
    {
        string apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases";
        using UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("User-Agent", "UnityUpdateManager");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("GitHub API request failed: " + request.error);
            yield break;
        }

        try
        {
            // Parse JSON manually to pick the right release
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
                    // pre-release branch: pick the newest stable or prerelease
                    chosenRelease = r;
                    break;
                }
            }

            if (chosenRelease != null)
            {
                remoteVersion = chosenRelease.tag_name;
                remoteUrl = chosenRelease.html_url;
                Debug.Log($"Latest remote version: {remoteVersion}");
                VersionLookup(branchIndex);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse GitHub releases: " + e.Message);
        }
    }

    private void VersionLookup(int branchIndex)
    {
        var localSem = new SemanticVersion(localVersion);
        var remoteSem = new SemanticVersion(remoteVersion);

        bool newerAvailable = false;

        if (branchIndex == 0)
        {
            // Stable branch: only consider stable releases
            if (!remoteSem.IsPreRelease && remoteSem > localSem)
                newerAvailable = true;
        }
        else
        {
            // Pre-release branch: consider any newer release
            if (remoteSem > localSem)
                newerAvailable = true;
        }

        if (newerAvailable)
        {
            localVersionLabel.text = "Current Version: " + localVersion;
            remoteVersionLabel.text = "New Version: " + remoteVersion;
            updateNotification.SetActive(true);

            // Optionally: add a button click to open release
            // yourButton.onClick.AddListener(() => Application.OpenURL(remoteUrl));
        }
        else
        {
            updateNotification.SetActive(false);
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
                Debug.LogWarning("Invalid version string: " + raw);
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
// GitHub returns an array of releases, so Unity can't directly parse it as a single object
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
