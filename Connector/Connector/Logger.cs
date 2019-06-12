using System;
using System.Collections.Generic;
using System.Linq;
using Connector.Protobuf;
using Microsoft.Azure.ServiceBus;

namespace Connector
{
    class Logger
    {
        private Dictionary<string, CampfireLineData> dictCommitShaToCampfireLineData { get; set; }
        private Dictionary<string, BasecampUploadData> dictGithubFileNameToBasecampUploadData { get; set; }
        private Dictionary<string, MessageBoardMessageData> dictCommitShaToMessageBoardMessageData { get; set; }

        public Logger()
        {
            dictCommitShaToCampfireLineData = new Dictionary<string, CampfireLineData>();
            dictGithubFileNameToBasecampUploadData = new Dictionary<string, BasecampUploadData>();
            dictCommitShaToMessageBoardMessageData = new Dictionary<string, MessageBoardMessageData>();
        }

        internal void AddCommitShaToCampfireLineDataMapping(string commitSha, CampfireLineData lineData)
        {
            dictCommitShaToCampfireLineData[commitSha] = lineData;
        }

        internal void AddGithubFileNameToBasecampUploadData(string githubFileName, BasecampUploadData uploadData)
        {
            dictGithubFileNameToBasecampUploadData[githubFileName] = uploadData;
        }

        internal void AddCommitShaToMessageBoardMessageData(string commitSha, MessageBoardMessageData mbMessage)
        {
            dictCommitShaToMessageBoardMessageData[commitSha] = mbMessage;
        }

        public void ProcessGithubCommit(Message message)
        {
            Send(new Message(message.Body));
        }

        public void ProcessGithubFile(Message message)
        {
            Send(new Message(message.Body));
        }

        public void PrintLoggedDicts()
        {
            Console.WriteLine("========================================================================");
            Console.WriteLine("COMMIT SHA TO CAMPFIRE LINE ID DICTIONARY");

            foreach (KeyValuePair<string, CampfireLineData> entry in dictCommitShaToCampfireLineData)
            {
                PrintCampfireDictItem(entry.Key, entry.Value.campfireLineId, entry.Value.campfireId, entry.Value.projectId);
                Console.WriteLine("-----------------------------------------------------------------");
            }

            Console.WriteLine("========================================================================");
            Console.WriteLine();
        }

        private void PrintCampfireDictItem(string commitSha, string campfireLineId, string campfireId, string projectId)
        {
            Console.WriteLine("GITHUB COMMIT SHA: " + commitSha);
            Console.WriteLine("CAMPFIRE LINE ID: " + campfireLineId);
            Console.WriteLine("CAMPFIRE ID: " + campfireId);
            Console.WriteLine("CAMPFIRE PROJECT ID: " + projectId);
        }

        private void PrintBasecampUploadDictItem(string githubFileName, string basecampUploadId,
                                                 string basecampBucketId, string basecampAppDownloadUrl)
        {
            Console.WriteLine("GITHUB FILE NAME: " + githubFileName);
            Console.WriteLine("BASECAMP UPLOAD ID: " + basecampUploadId);
            Console.WriteLine("BASECAMP BUCKET ID: " + basecampBucketId);
            Console.WriteLine("BASECAMP APP DOWNLOAD URL: " + basecampAppDownloadUrl);
        }

        private void PrintBasecampMessageBoardMessageDictItem(string commitSha, string messageBoardMessageId,
                                                              string projectId)
        {
            Console.WriteLine("GITHUB COMMIT SHA: " + commitSha);
            Console.WriteLine("BASECAMP MESSAGE BOARD MESSAGE ID: " + messageBoardMessageId);
            Console.WriteLine("BASECAMP PROJECT ID: " + projectId);
        }

        private void Send(Message message)
        {
            ServiceBusQueueSender.SendMessages(message);
        }

        public void ProcessBasecampCampfireLine(CampfireLine line)
        {
            string commitSha = line.Data.First().CommitSha;
            string campfireLineId = line.Data.First().CampfireLineId;
            string campfireId = line.Data.First().CampfireId;
            string projectId = line.Data.First().ProjectId;

            CampfireLineData lineData = new CampfireLineData(campfireLineId, campfireId, projectId);
            MongoDBClient.StoreCommitShaToCampfireLineData(commitSha, lineData);

            CreateCommitShaToCampfireLineMapping(commitSha, lineData);
        }

        internal void CreateCommitShaToCampfireLineMapping(string commitSha, CampfireLineData lineData)
        {
            dictCommitShaToCampfireLineData[commitSha] = lineData;

            Console.WriteLine("--------------------------------------------------------------");
            PrintCampfireDictItem(commitSha, lineData.campfireLineId, lineData.campfireId, lineData.projectId);
            Console.WriteLine("--------------------------------------------------------------");
        }

        public void ProcessBasecampUpload(BasecampUpload upload)
        {
            string githubFileName = upload.GithubData.First().FileName;
            string uploadId = upload.BasecampData.First().Id;
            string basecampFileName = upload.BasecampData.First().FileName;
            string bucketId = upload.BasecampData.First().BucketId;
            string appDownloadUrl = upload.BasecampData.First().AppDownloadUrl;

            BasecampUploadData uploadData = new BasecampUploadData(uploadId, basecampFileName, bucketId, appDownloadUrl);
            MongoDBClient.StoreGithubFileToBasecampUploadData(githubFileName, uploadData);

            CreateGithubFileNameToBasecampUploadDataMapping(githubFileName, uploadData);
        }

        internal void CreateGithubFileNameToBasecampUploadDataMapping(string githubFileName, BasecampUploadData uploadData)
        {
            dictGithubFileNameToBasecampUploadData[githubFileName] = uploadData;

            Console.WriteLine("--------------------------------------------------------------");
            PrintBasecampUploadDictItem(githubFileName, uploadData.uploadId, uploadData.bucketId, uploadData.appDownloadUrl);
            Console.WriteLine("--------------------------------------------------------------");
        }

        public void ProcessBasecampMessageBoardMessage(BasecampMessageBoardMessage mbMessage)
        {
            string commitSha = mbMessage.GithubData.First().CommitSha;
            string messageBoardMessageId = mbMessage.BasecampData.First().MessageBoardMessageId;
            string projectId = mbMessage.BasecampData.First().ProjectId;

            MessageBoardMessageData mbMessageData = new MessageBoardMessageData(messageBoardMessageId, projectId);
            MongoDBClient.StoreCommitShaToMessageBoardMessageData(commitSha, mbMessageData);

            CreateCommitShaToMessageBoardMessageDataMapping(commitSha, mbMessageData);
        }

        internal void CreateCommitShaToMessageBoardMessageDataMapping(string commitSha, MessageBoardMessageData mbMessageData)
        {
            dictCommitShaToMessageBoardMessageData[commitSha] = mbMessageData;

            Console.WriteLine("--------------------------------------------------------------");
            PrintBasecampMessageBoardMessageDictItem(commitSha, mbMessageData.messageBoardMessageId, mbMessageData.projectId);
            Console.WriteLine("--------------------------------------------------------------");
        }

        internal class CampfireLineData
        {
            internal string campfireLineId { get; set; }
            internal string campfireId { get; set; }
            internal string projectId { get; set; }

            public CampfireLineData(string campfireLineId, string campfireId, string projectId)
            {
                this.campfireLineId = campfireLineId;
                this.campfireId = campfireId;
                this.projectId = projectId;
            }
        }

        internal class BasecampUploadData
        {
            internal string uploadId { get; set; }
            internal string fileName { get; set; }
            internal string bucketId { get; set; }
            internal string appDownloadUrl { get; set; }

            public BasecampUploadData(string uploadId, string fileName, string bucketId, string appDownloadUrl)
            {
                this.uploadId = uploadId;
                this.fileName = fileName;
                this.bucketId = bucketId;
                this.appDownloadUrl = appDownloadUrl;
            }
        }

        internal class MessageBoardMessageData
        {
            internal string messageBoardMessageId { get; set; }
            internal string projectId { get; set; }

            public MessageBoardMessageData(string messageBoardMessageId, string projectId)
            {
                this.messageBoardMessageId = messageBoardMessageId;
                this.projectId = projectId;
            }
        }
    }
}
