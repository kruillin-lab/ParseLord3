namespace RotationSolver.Data
{
    public class GitHubCommitComparison
    {
        public GitHubCommitComparison()
        {
            Url = string.Empty;
            HtmlUrl = string.Empty;
            PermalinkUrl = string.Empty;
            DiffUrl = string.Empty;
            PatchUrl = string.Empty;
            BaseCommit = new Commit();
            MergeBaseCommit = new Commit();
            Status = string.Empty;
            Commits = [];
            Files = [];
        }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("permalink_url")]
        public string PermalinkUrl { get; set; }

        [JsonProperty("diff_url")]
        public string DiffUrl { get; set; }

        [JsonProperty("patch_url")]
        public string PatchUrl { get; set; }

        [JsonProperty("base_commit")]
        public Commit BaseCommit { get; set; }

        [JsonProperty("merge_base_commit")]
        public Commit MergeBaseCommit { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("ahead_by")]
        public int AheadBy { get; set; }

        [JsonProperty("behind_by")]
        public int BehindBy { get; set; }

        [JsonProperty("total_commits")]
        public int TotalCommits { get; set; }

        [JsonProperty("commits")]
        public List<Commit> Commits { get; set; }

        [JsonProperty("files")]
        public List<CommitFile> Files { get; set; }
    }

    public class Commit
    {
        public Commit()
        {
            Sha = string.Empty;
            NodeId = string.Empty;
            CommitData = new CommitData();
            Url = string.Empty;
            HtmlUrl = string.Empty;
            CommentsUrl = string.Empty;
            Author = new GithubRelease.Author();
            Committer = new GithubRelease.Author();
            Parents = [];
        }

        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("commit")]
        public CommitData CommitData { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("comments_url")]
        public string CommentsUrl { get; set; }

        [JsonProperty("author")]
        public GithubRelease.Author Author { get; set; }

        [JsonProperty("committer")]
        public GithubRelease.Author Committer { get; set; }

        [JsonProperty("parents")]
        public List<Parent> Parents { get; set; }
    }

    public class CommitData
    {
        public CommitData()
        {
            CommitAuthor = new Author();
            CommitCommitter = new Author();
            Message = string.Empty;
            Tree = new Tree();
            Url = string.Empty;
            Verification = new Verification();
        }

        [JsonProperty("author")]
        public Author CommitAuthor { get; set; }

        [JsonProperty("committer")]
        public Author CommitCommitter { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("tree")]
        public Tree Tree { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }

        [JsonProperty("verification")]
        public Verification Verification { get; set; }
    }

    public class Author
    {
        public Author()
        {
            Name = string.Empty;
            Email = string.Empty;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }

    public class Tree
    {
        public Tree()
        {
            Sha = string.Empty;
            Url = string.Empty;
        }

        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Verification
    {
        public Verification()
        {
            Reason = string.Empty;
            Signature = string.Empty;
            Payload = string.Empty;
        }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }

    public class Parent
    {
        public Parent()
        {
            Sha = string.Empty;
            Url = string.Empty;
            HtmlUrl = string.Empty;
        }

        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }

    public class CommitFile
    {
        public CommitFile()
        {
            Sha = string.Empty;
            Filename = string.Empty;
            Status = string.Empty;
            BlobUrl = string.Empty;
            RawUrl = string.Empty;
            ContentsUrl = string.Empty;
            Patch = string.Empty;
        }

        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("additions")]
        public int Additions { get; set; }

        [JsonProperty("deletions")]
        public int Deletions { get; set; }

        [JsonProperty("changes")]
        public int Changes { get; set; }

        [JsonProperty("blob_url")]
        public string BlobUrl { get; set; }

        [JsonProperty("raw_url")]
        public string RawUrl { get; set; }

        [JsonProperty("contents_url")]
        public string ContentsUrl { get; set; }

        [JsonProperty("patch")]
        public string Patch { get; set; }
    }
}
