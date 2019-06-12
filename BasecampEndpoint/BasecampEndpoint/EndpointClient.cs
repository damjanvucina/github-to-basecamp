using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using BasecampEndpoint.Protobuf;
using Microsoft.Azure.ServiceBus;
using System.Net;
using System.Web;
using RestSharp;

namespace BasecampEndpoint
{
    class EndpointClient
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static RestClient restClient = new RestClient();
        private static string authorizationToken;

        private static string createCampfireLineUri;
        public static string createAttachmentUri;
        private static string createUploadUri;
        private static string createMessageBoardMessageUri;

        public static string CreateAttachment(GithubFile file)
        {
            byte[] bytes;
            using (WebClient client = new WebClient())
            {
                bytes = client.DownloadData(file.File.First().RawUrl);
            }

            string fileName = file.File.First().Filename;
            restClient.BaseUrl = AddParameter(createAttachmentUri, "name", fileName);

            var request = new RestRequest(Method.POST);

            request.AddHeader("User-Agent", "My C# app");
            request.AddHeader("Content-Type", MimeTypes.GetMimeType(fileName));
            request.AddHeader("Content-Length", bytes.Length.ToString());
            request.AddHeader("Authorization", authorizationToken);
            request.AddParameter("undefined", bytes, ParameterType.RequestBody);

            IRestResponse response = restClient.Execute(request);

            JObject jObjectLine = JObject.Parse(response.Content);
            string attachmentId = jObjectLine["attachable_sgid"].ToString();

            return attachmentId;
        }

        public static Uri AddParameter(string url, string paramName, string paramValue)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[paramName] = paramValue;
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        public static Message CreateDocumentUpload(string attachmentID, GithubFile file)
        {
            string fileDescription = ProtobufEncoder.DecodeFileMessage(file);
            string fileName = file.File.First().Filename.Substring(0, file.File.First().Filename.IndexOf("."));

            restClient.BaseUrl = new Uri(createUploadUri);

            var request = new RestRequest(Method.POST);
            request.AddHeader("Host", "3.basecampapi.com");
            request.AddHeader("User-Agent", "My C# App");
            request.AddHeader("Content-Type", "application/json");

            request.AddHeader("Authorization", authorizationToken);

            request.AddParameter("undefined", "{\n    \"attachable_sgid\": \"" + attachmentID + "\",\n    \"description\": \"" + fileDescription + "\",\n    \n    \"base_name\": \"" + fileName + "\"\n}", ParameterType.RequestBody);

            IRestResponse response = restClient.Execute(request);

            //BlobStorageClient.deleteBlobItem(file.File.First().Filename);

            JObject jObjectUpload = JObject.Parse(response.Content);
            Message uploadMessage = ProtobufEncoder.EncodeUploadMessage(file, jObjectUpload);

            return uploadMessage;
        }

        public static Message CreateMessageBoardMessage(GithubCommit commit)
        {
            restClient.BaseUrl = new Uri(createMessageBoardMessageUri);
            var request = new RestRequest(Method.POST);

            request.AddHeader("User-Agent", "My c# App");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", authorizationToken);

            string subject = GenerateMessageBoardMessageSubject(commit);
            string content = GenerateMessageBoardMessageContent(commit);

            request.AddParameter("undefined", "{\n    \"subject\": \"" + subject + "\",\n    \"content\": \"" + content + "\",\n    \"status\": \"active\"\n}", ParameterType.RequestBody);
            IRestResponse response = restClient.Execute(request);

            JObject jObjectMessage = JObject.Parse(response.Content);
            Message messageBoardMessage = ProtobufEncoder.EncodeMessageBoardMessage(commit, jObjectMessage);

            return messageBoardMessage;
        }

        private static string GenerateMessageBoardMessageContent(GithubCommit commit)
        {
            string authorLink = commit.Author.First().HtmlUrl;
            string authorName = commit.Author.First().Name;
            string date = commit.Commit.First().Date;
            string commitMessage = commit.Commit.First().Message;
            int comments = commit.Commit.First().CommentCount;
            int totalChanges = commit.TotalStats.First().Total;
            int additions = commit.TotalStats.First().Additions;
            int deletions = commit.TotalStats.First().Deletions;
            string commitLink = commit.Commit.First().HtmlUrl;

            StringBuilder sb = new StringBuilder();
            sb.Append("<strong>AUTHOR:</strong> <a href=\\\"").Append(authorLink).Append("\\\">").Append(authorName).Append("</a> <br>")
              .Append("<strong>DATE:</strong> ").Append(date).Append(" <br>")
              .Append("<strong>COMMIT MESSAGE:</strong> ").Append(commitMessage).Append("<br>")
              .Append("<strong>GITHUB COMMENTS:</strong> ").Append(comments).Append("<br>")
              .Append("<strong>TOTAL CHANGES:</strong> ").Append(totalChanges).Append("<br>")
              .Append("<strong>ADDITIONS:</strong> ").Append(additions).Append("<br>")
              .Append("<strong>DELETIONS:</strong> ").Append(deletions).Append("<br><br><em>Commit can be viewed <a href=\\\"")
              .Append(commitLink).Append("\\\">here</a></em>");

            return sb.ToString();
        }

        private static string GenerateMessageBoardMessageSubject(GithubCommit commit)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(commit.Commit.First().Message).Append(", by ").Append(commit.Author.First().Name);

            return sb.ToString();
        }

        public static Message CreateCampfireLine(GithubCommit commit)
        {
            string commitMessage = ProtobufEncoder.DecodeCommitMessage(commit);

            Task<HttpResponseMessage> task = httpClient.PostAsync(createCampfireLineUri,
                new StringContent("{\"content\":\" " + commitMessage + "\"}", Encoding.UTF8, "application/json"));
            task.Wait();

            HttpResponseMessage response = task.Result;
            JObject jObjectLine = JObject.Parse(ContentToString(response.Content));
          
            Message lineMessage = ProtobufEncoder.EncodeCampfireLineMessage(commit.Commit.First().Sha, jObjectLine);

            return lineMessage;
        }

        public static string ContentToString(HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        public static void SetDefaultHeaders()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", authorizationToken);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C#App");
        }

        private static string GenerateCampfireLine(string authorName, string commitDate, string commitMessage,
                                                   int statsTotal, int statsAdditions, int statsDeletions)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NEW GITHUB COMMIT").Append("\\n")
              .Append("USER:").Append(authorName).Append("\\n")
              .Append("DATE:").Append(commitDate).Append("\\n")
              .Append("MESSAGE:").Append(" created a new commit at ").Append("\\n")
              .Append("CHANGES:").Append(statsTotal).Append("\\n")
              .Append("ADDITIONS:").Append(statsAdditions).Append("\\n")
              .Append("DELETIONS").Append(statsDeletions);

            return sb.ToString();
        }

        public static void SetAuthorizationToken(string authorizationToken)
        {
            EndpointClient.authorizationToken = authorizationToken;
        }

        public static void SetCreateCampfireLineUri(string createCampfireLineUri)
        {
            EndpointClient.createCampfireLineUri = createCampfireLineUri;
        }

        public static void SetCreateAttachmentUri(string createAttachmentUri)
        {
            EndpointClient.createAttachmentUri = createAttachmentUri;
        }

        public static void SetCreateUploadUri(string createUploadUri)
        {
            EndpointClient.createUploadUri = createUploadUri;
        }

        public static void SetCreateMessageBoardMessageUri(string createMessageBoardMessageUri)
        {
            EndpointClient.createMessageBoardMessageUri = createMessageBoardMessageUri;
        }
    }
}
