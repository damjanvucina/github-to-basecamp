using Microsoft.Azure.ServiceBus;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GithubEndpoint
{
    class Program
    {
        private static string MongoConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
        private static string MongoDatabaseName = "GithubEndpointDB";
        private static string MongoCollectionName = "Configuration";

        private static EndpointClient endpointClient;
        private static String repoName;
        private static TimeSpan startTimeSpan = TimeSpan.Zero;
        private static TimeSpan periodTimeSpan = TimeSpan.FromSeconds(20);

        static void Main(string[] args)
        {
            Console.Title = "GITHUB ENDPOINT";
            MongoDBClient.ConnectAndSet(MongoConnectionString, MongoDatabaseName, MongoCollectionName);

            RunEndpoint();
            Console.ReadKey();
        }

        private static void RunEndpoint()
        {
            Console.WriteLine("GITHUB ENDPOINT ACTIVATED");
            try
            {
                endpointClient = new EndpointClient();
                Thread thread = new Thread(new ThreadStart(InvokeMethod));
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static void InvokeMethod()
        {
            while (true)
            {
                CheckForNewCommits();
                Thread.Sleep(1000 * 20);
            }
        }

        private static void CheckForNewCommits()
        {

            Task<List<GitHubCommit>> task = endpointClient.GetAllCommits(repoName);
            task.Wait();

            List<GitHubCommit> newCommits = task.Result;

            if (newCommits.Any())
            {
                newCommits.Reverse(); //so they're ordered by date ascending
                PrintEndpointStatusChanged(true, newCommits);

                foreach (GitHubCommit commit in newCommits)
                {
                    Message commitMessage = ProtobufEncoder.EncodeCommitMessage(commit);
                    ServiceBusSender.SendMessage(commitMessage);
                    
                    foreach (GitHubCommitFile file in commit.Files)
                    {
                        try
                        {
                            String storageUrl = BlobStorageUploader.UploadFile(file.Filename, file.RawUrl);
                            Message fileMessage = ProtobufEncoder.EncodeFileMessage(file, commit.Sha, storageUrl);
                            ServiceBusSender.SendMessage(fileMessage);
                        }
                        catch (System.Net.WebException e) 
                        {
                            //throws when you try to upload .settings, .classpath .project or similar pseudo-files often present in github repos; works perfectly for reguar files like .pdf, .txt, etc.
                            //uncomment next line if you prefer
                            //Console.WriteLine("File " + file.RawUrl + " did not upload since its url does not represent a valid file.");
                        }
                       
                    }

                    DateTimeOffset offset = commit.Commit.Author.Date.AddSeconds(1);
                    MongoDBClient.UpdateLastProcessedCommitDate(offset);
                }
            }
            else
            {
                PrintEndpointStatusChanged(false);
            }

            task.Dispose();
        }

        private static void PrintEndpointStatusChanged(Boolean change, List<GitHubCommit> newCommits = null)
        {
            Console.WriteLine("--------------------------------------------------------");
            Console.WriteLine("ENDPOINT CHECK TIMESTAMP:    " + endpointClient.LastCheckTime);
            if (change)
            {
                Console.WriteLine("NEW COMMITS NUMBER:          " + newCommits.Count());
                Console.WriteLine("LATEST COMMIT TIMESTAMP:     " + newCommits.Last().Commit.Author.Date.ToLocalTime());
                Console.WriteLine("LATEST COMMIT MESSAGE:       " + newCommits.Last().Commit.Message);
            }
            else
            {
                Console.WriteLine("NOTHING CHANGED");
            }

            Console.WriteLine("--------------------------------------------------------");

        }

        public static void SetRepoName(string repoName)
        {
            Program.repoName = repoName;
        }
    }
}

