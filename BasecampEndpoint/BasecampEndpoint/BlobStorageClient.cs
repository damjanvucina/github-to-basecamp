using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace BasecampEndpoint
{
    class BlobStorageClient
    {
        private static string storageConnectionString;
        private static string containerName;
        private static CloudStorageAccount storageAccount = null;
        private static CloudBlobContainer cloudBlobContainer = null;
        private static bool printDetails = true;

        public static void DeleteBlobItem(string fileName)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                cloudBlobContainer.GetBlockBlobReference(fileName).Delete();

                if (printDetails)
                {
                    Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    Console.WriteLine("File " + fileName + " sucessfully deleted from Azure Blob Storage.");
                    Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                }
            }
        }

        public static void SetStorageConnectionString(string storageConnectionString)
        {
            BlobStorageClient.storageConnectionString = storageConnectionString;
        }

        public static void SetContainerName(string containerName)
        {
            BlobStorageClient.containerName = containerName;
        }
    }
}
