using System.Linq;
using System.Text;
using BasecampEndpoint.Protobuf;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.ServiceBus;
using Google.Protobuf;
using System.IO;

namespace BasecampEndpoint
{
    class ProtobufEncoder
    {
        private static string CampfireLine = "CAMPFIRE_LINE";
        private static string BasecampUpload = "BASECAMP_UPLOAD";
        private static string BasecampMessageBoardMessage = "BASECAMP_MESSAGE_BOARD_MESSAGE";

        public static string DecodeFileMessage(GithubFile file)
        {
            string status = file.File.First().Status;
            int total = file.Stats.First().Total;
            int additions = file.Stats.First().Additions;
            int deletions = file.Stats.First().Deletions;

            StringBuilder sb = new StringBuilder();
            sb.Append("<strong>STATUS:</strong> ").Append(status).Append("<br/>")
              .Append("<strong>TOTAL CHANGES:</strong> ").Append(total).Append("<br/>")
              .Append("<strong>ADDITIONS:</strong> ").Append(additions).Append("<br/>")
              .Append("<strong>DELETIONS:</strong> ").Append(deletions);

            return sb.ToString();
        }

        public static string DecodeCommitMessage(GithubCommit commit)
        {
            string authorName = commit.Author.First().Name;
            string commitDate = commit.Commit.First().Date;
            string commitMessage = commit.Commit.First().Message;
            int statsTotal = commit.TotalStats.First().Total;
            int statsAdditions = commit.TotalStats.First().Additions;
            int statsDeletions = commit.TotalStats.First().Deletions;

            StringBuilder sb = new StringBuilder();
            sb.Append("NEW GITHUB COMMIT").Append("\\n")
              .Append("USER:").Append(authorName).Append("\\n")
              .Append("DATE:").Append(commitDate).Append("\\n")
              .Append("MESSAGE:").Append(commitMessage).Append("\\n")
              .Append("CHANGES:").Append(statsTotal).Append("\\n")
              .Append("ADDITIONS:").Append(statsAdditions).Append("\\n")
              .Append("DELETIONS").Append(statsDeletions);

            return sb.ToString();
        }

        public static Message EncodeUploadMessage(GithubFile file, JObject jObjectFile)
        {
                        
            IMessage protobufMessage = new BasecampUpload
            {
                Message = { new BasecampUpload.Types.MessageData
                {
                    Type = BasecampUpload
                } },

                GithubData = { new BasecampUpload.Types.GithubData
                {
                    FileName = file.File.First().Filename,
                    Status = file.File.First().Status
                } },

                BasecampData = { new BasecampUpload.Types.BasecampData
                {
                    Id = jObjectFile["id"].ToString(),
                    FileName = jObjectFile["filename"].ToString(),
                    BucketId = jObjectFile["bucket"]["id"].ToString(),
                    AppDownloadUrl = jObjectFile["app_download_url"].ToString()

                } }
            };

            return GenerateServiceBusMessage(protobufMessage);
        }

        public static Message EncodeCampfireLineMessage(string commitSha, JObject jObjectLine)
        {
            IMessage protobufMessage = new CampfireLine
            {
                Message = { new CampfireLine.Types.MessageData
                {
                    Type = CampfireLine
                } },

                Data = { new CampfireLine.Types.LineData
                {
                    CommitSha = commitSha,
                    CampfireLineId = jObjectLine["id"].ToString(),
                    CampfireId = jObjectLine["parent"]["id"].ToString(),
                    ProjectId = jObjectLine["bucket"]["id"].ToString()
                } }
            };

            return GenerateServiceBusMessage(protobufMessage);
        }

        public static Message EncodeMessageBoardMessage(GithubCommit commit, JObject jObjectMessage)
        {
            IMessage protobufMessage = new BasecampMessageBoardMessage
            {
                Message = { new BasecampMessageBoardMessage.Types.MessageData
                {
                    Type = BasecampMessageBoardMessage
                }},

                GithubData = { new BasecampMessageBoardMessage.Types.GithubData
                {
                    CommitSha = commit.Commit.First().Sha
                } },

                BasecampData = { new BasecampMessageBoardMessage.Types.BasecampData
                {
                    MessageBoardMessageId = jObjectMessage["id"].ToString(),
                    ProjectId = jObjectMessage["bucket"]["id"].ToString()
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
