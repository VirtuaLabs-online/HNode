using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Semver;

public class UpdateManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI logoVersionLabel;
    public TextMeshProUGUI localVersionLabel;
    public TextMeshProUGUI remoteVersionLabel;
    public GameObject updateNotification;
    public TMP_Dropdown branchDropdown;
    public GameObject UpdatePromptButton;

    [Header("GitHub Info")]
    public string githubOwner = "VirtuaLabs-online";
    public string githubRepo = "HNode";

    private string localVersion;
    private string remoteVersion;
    private string remoteUrl;
    private int savedIndex = 0;

    void Start()
    {
        logoVersionLabel.gameObject.SetActive(false);
        UpdatePromptButton.SetActive(false);
        updateNotification.SetActive(false);
        savedIndex = PlayerPrefs.GetInt("SelectedBranch", 0);
        branchDropdown.value = savedIndex;
        branchDropdown.onValueChanged.Invoke(savedIndex);
        branchDropdown.onValueChanged.AddListener(OnBranchChanged);
        localVersion = Application.version;
        Invoke(nameof(FetchInitialRelease), 1f);
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
        FetchLatestReleaseSync(releaseBranch);
    }

    private void FetchLatestReleaseSync(int branchIndex)
    {
        string apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases";
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("User-Agent", "UnityUpdateManager");
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }
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
        var localSem = SemVersion.Parse(localVersion, SemVersionStyles.Any);
        var remoteSem = SemVersion.Parse(remoteVersion, SemVersionStyles.Any);

        bool newerAvailable = false;

        if (branchIndex == 0)
        {
            if (!remoteSem.IsPrerelease && remoteSem.ComparePrecedenceTo(localSem) > 0)
                newerAvailable = true;
        }
        else
        {
            if (remoteSem.ComparePrecedenceTo(localSem) > 0)
                newerAvailable = true;
        }

        if (newerAvailable)
        {
            localVersionLabel.text = "Current Version: " + localVersion;
            remoteVersionLabel.text = "New Version: " + remoteVersion;
            updateNotification.SetActive(true);
            UpdatePromptButton.SetActive(true);
        }
        else
        {
            updateNotification.SetActive(false);
            UpdatePromptButton.SetActive(false);
        }
    }

    public void OpenLatestRelease()
    {
        if (string.IsNullOrEmpty(remoteVersion)) return;
        string url = $"https://github.com/{githubOwner}/{githubRepo}/releases/tag/{remoteVersion}";
        Application.OpenURL(url);
    }

    public void DismissUpdateNotification()
    {
        updateNotification.SetActive(false);
    }
}

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
