using LibGit2Sharp;

namespace Fushigi.util;

public class GitRepository
{
    private static readonly Dictionary<string, GitRepository> Repositories = new();

    public string WorkingDir { get; }

    public string? GitDir
    {
        get
        {
            if (_repo == null)
            {
                return null;
            }

            return _repo.Info.Path;
        }
    }

    public bool IsValid()
    {
        return _repo != null;
    }

    public Dictionary<string, string> Remotes
    {
        get
        {
            if (IsValid())
            {
                Dictionary<string, string> remotes = new Dictionary<string, string>();

                foreach (Remote remote in _repo.Network.Remotes)
                {
                    remotes.Add(remote.Name, remote.Url);
                }
            }

            return new Dictionary<string, string>();
        }
        set
        {
            if (IsValid())
            {
                foreach (Remote remote in _repo.Network.Remotes)
                {
                    _repo.Network.Remotes.Remove(remote.Name);
                }

                foreach (KeyValuePair<string, string> remote in value)
                {
                    _repo.Network.Remotes.Add(remote.Key, remote.Value);
                }
            }
        }
    }

    private Repository? _repo;

    private GitRepository(string workingDir)
    {
        if (string.IsNullOrEmpty(workingDir))
        {
            this.WorkingDir = "";
            return;
        }
        this.WorkingDir = Path.GetFullPath(workingDir);
        if (Repository.IsValid(WorkingDir))
        {
            _repo = new Repository(WorkingDir);
        }
    }

    public static GitRepository GetInstance(string workingDir)
    {
        if (!Repositories.ContainsKey(workingDir))
        {
            Repositories.Add(workingDir,new GitRepository(workingDir));
        }
        return Repositories[workingDir];
    }

    public bool StageAll()
    {
        if (_repo != null)
        {
            Commands.Stage(_repo, "*");
        }
        return false;
    }

    public bool Init()
    {
        if (_repo == null && !string.IsNullOrEmpty(WorkingDir))
        {
            Repository.Init(WorkingDir);
            _repo = new Repository(WorkingDir);
            return true;
        }
        return false;
    }

    public string? Commit(string message, string authorName, string authorEmail)
    {
        if (_repo == null)
        {
            return "";
        }
        Signature sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);

        if (!_repo.RetrieveStatus().IsDirty)
        {
            return _repo.Head.Tip?.Sha ?? "";
        }
        
        Commit commit = _repo.Commit(message, sig,sig);
        return commit.Sha;
    }

    public bool RemoteAdd(string name, string url)
    {
        if (_repo == null)
        {
            return false;
        }
        
        Remote? existing = _repo.Network.Remotes[name];
        if (existing != null)
        {
            return false;
        }
        _repo.Network.Remotes.Add(name, url);
        return true;
    }

    public bool RemoteRemove(string name)
    {
        if (_repo == null)
        {
            return false;
        }
        
        Remote? existing = _repo.Network.Remotes[name];
        if (existing == null)
        {
            return false;
        }
        _repo.Network.Remotes.Remove(name);
        return true;
    }

    public bool Fetch(string remoteName = "origin", string? username = null, string? passwordOrToken = null)
    {
        if (_repo == null)
        {
            return false;
        }

        Remote? remote = _repo.Network.Remotes[remoteName];
        if (remote == null)
        {
            return false;
        }

        FetchOptions options = new();
        
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(passwordOrToken))
        {
            options.CredentialsProvider = (_url, _user, _types) => 
                new UsernamePasswordCredentials { Username = username, Password = passwordOrToken };
        }
        
        Commands.Fetch(_repo, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification),options, logMessage:null);

        return true;
    }
}