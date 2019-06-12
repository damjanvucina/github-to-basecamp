using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GithubEndpoint
{
    class EndpointClient
    {
        private static String githubOAuthToken;
        private static String userLogin;

        private GitHubClient client;
        private Credentials tokenAuth;
        private User user;
        private static DateTimeOffset lastProcessedCommitDate;
        private CommitRequest commitRequest;
        private DateTime lastCheckTime;

        public EndpointClient()
        {
            client = new GitHubClient(new ProductHeaderValue("githubtobasecamp"));
            tokenAuth = new Credentials(githubOAuthToken);
            client.Credentials = tokenAuth;
            commitRequest = new CommitRequest();
            commitRequest.Since = lastProcessedCommitDate;

            SetUpUser();
        }
       
        public void UpdateLastProcessedCommitDate(DateTimeOffset offset)
        {
            lastProcessedCommitDate = offset.AddSeconds(1);
            commitRequest.Since = lastProcessedCommitDate;
        }

        public async Task<List<GitHubCommit>> GetAllCommits(String repositoryName)
        {
            var repository = await client.Repository.Get(userLogin, repositoryName);
            lastCheckTime = DateTime.Now;

            var commitsAll = await client.Repository.Commit.GetAll(repository.Id, commitRequest);
            List<GitHubCommit> commits = new List<GitHubCommit>();
            if (commitsAll.Count > 0)
            {
                UpdateLastProcessedCommitDate(commitsAll.First().Commit.Author.Date);

                foreach (var com in commitsAll)
                {
                    var task = await client.Repository.Commit.Get(repository.Id, com.Sha);
                    commits.Add(task);
                }
            }
            return commits;
        }


        public async void SetUpUser()
        {
            user = await client.User.Current();
        }

        public async void PrintAllCommits(String repositoryName)
        {
            Console.WriteLine("Printing all commit messages from repository " + repositoryName);
            var allCommitsInRepo = await client.Repository.Commit.GetAll("damjanvucina", repositoryName);

            var commitsFiltered = allCommitsInRepo.Select(async (_) =>
            {
                return await client.Repository.Commit.Get("damjanvucina", repositoryName, _.Sha);
            }).ToList();

            var commits = await Task.WhenAll(commitsFiltered);

            foreach (var com in commits)
            {
                Console.WriteLine(com.Commit.Message);
            }
        }

        public GitHubClient Client
        {
            get
            {
                return client;
            }
        }

        public DateTime LastCheckTime
        {
            get
            {
                return lastCheckTime;
            }
        }

        public DateTimeOffset LastProcessedCommitDate
        {
            get
            {
                return lastProcessedCommitDate;
            }
            set
            {
                lastProcessedCommitDate = value;
            }
        }

        public static void SetGithubOAuthToken(string githubOAuthToken)
        {
            EndpointClient.githubOAuthToken = githubOAuthToken;
        }

        public static void SetUserLogin(string userLogin)
        {
            EndpointClient.userLogin = userLogin;
        }

        public static void SetLastProcessedCommitDate(string lastProcessedCommitDate)
        {
            EndpointClient.lastProcessedCommitDate = DateTimeOffset.Parse(lastProcessedCommitDate);
        }
    }
}
