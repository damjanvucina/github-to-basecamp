using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Net;

namespace GithubEndpoint
{
    class BlobStorageUploader
    {
        private static string storageConnectionString;
        private static string containerName;
        private static CloudStorageAccount storageAccount = null;
        private static CloudBlobContainer cloudBlobContainer = null;
        private static bool printDetails = true;

        public static string UploadFile(string filename, string rawUrl)
        {
            CloudBlockBlob cloudBlockBlob = null;

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rawUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream inputStream = response.GetResponseStream();
                    cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);
                    cloudBlockBlob.UploadFromStream(inputStream);

                    if(printDetails)
                    {
                        Console.WriteLine();
                        Console.WriteLine("========================================================");
                        Console.WriteLine("Successfully uploaded " + filename + " to " + cloudBlockBlob.StorageUri.PrimaryUri.ToString());
                        Console.WriteLine("========================================================");
                        Console.WriteLine();
                    }
                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'storageconnectionstring' with your storage " +
                    "connection string as a value.");
            }

            return cloudBlockBlob.StorageUri.PrimaryUri.ToString();
        }

        public static void SetStorageConnectionString(string storageConnectionString)
        {
            BlobStorageUploader.storageConnectionString = storageConnectionString;
        }

        public static void SetContainerName(string containerName)
        {
            BlobStorageUploader.containerName = containerName;
        }
    }
}
