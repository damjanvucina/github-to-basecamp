using GithubEndpoint.Protobuf;
using Microsoft.Azure.ServiceBus;
using Octokit;
using System.IO;
using Google.Protobuf;

namespace GithubEndpoint
{
    class ProtobufEncoder
    {
        public static string Commit = "COMMIT";
        public static string File = "FILE";
        
        public static Message EncodeCommitMessage(GitHubCommit commit)
        {
            IMessage protobufMessage = new GithubCommit
            {
                Message = {new GithubCommit.Types.MessageData
                {
                    Type = Commit
                } },

                Author = { new GithubCommit.Types.AuthorData
                {
                    Name = commit.Commit.Author.Name,
                    Email = commit.Commit.Author.Email,
                    AvatarUrl = commit.Author.AvatarUrl,
                    HtmlUrl = commit.Author.HtmlUrl
                } },

                Commit = { new GithubCommit.Types.CommitData
                {
                    Sha = commit.Sha,
                    Date = commit.Commit.Author.Date.ToLocalTime().ToString(),
                    Message = commit.Commit.Message,
                    HtmlUrl = commit.HtmlUrl,
                    CommentCount = commit.Commit.CommentCount
                } },

                TotalStats = { new GithubCommit.Types.TotalStatsData
                {
                    Total = commit.Stats.Total,
                    Additions = commit.Stats.Additions,
                    Deletions = commit.Stats.Deletions
                } }
            };

            return GenerateServiceBusMessage(protobufMessage);
        }

        public static Message EncodeFileMessage(GitHubCommitFile file, string commitSha, string storageUrl)
        {
            IMessage protobufMessage = new GithubFile
            {
                Message = { new GithubFile.Types.MessageData
                {
                    Type = File
                } },

                Commit = { new GithubFile.Types.CommitData
                {
                    Sha = commitSha
                } },

                File = { new GithubFile.Types.FileData
                {
                    Sha = file.Sha,
                    Filename = file.Filename,
                    Status = file.Status,
                    RawUrl = storageUrl
                } },

                Stats = { new GithubFile.Types.StatsData
                {
                    Total = file.Changes,
                    Additions = file.Additions,
                    Deletions = file.Deletions
                } }
            };

            return GenerateServiceBusMessage(protobufMessage);
        }


        private static Message GenerateServiceBusMessage(IMessage protobufMessage)
        {
            byte[] bytes;

            using (MemoryStream stream = new MemoryStream())
            {
                protobufMessage.WriteTo(stream);
                bytes = stream.ToArray();
            }

            return new Message(bytes);
        }
    }
}
