using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace GithubEndpoint
{
    class ServiceBusSender
    {
        private static string serviceBusConnectionString;
        private static string topicName;
        private static ITopicClient topicClient;
        private static bool printDetails = true;

        public static void SendMessage(Message message)
        {
            MainAsync(message).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(Message message)
        {
            topicClient = new TopicClient(serviceBusConnectionString, topicName);
            await SendMessagesAsync(message);
            await topicClient.CloseAsync();
        }

        private static async Task SendMessagesAsync(Message message)
        {
            try
            {
                if (printDetails)
                {
                    Console.WriteLine();
                    Console.WriteLine("**********************************************************");
                    Console.WriteLine("Sending message to the topic: " + Encoding.UTF8.GetString(message.Body));
                }

                await topicClient.SendAsync(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
                Console.WriteLine(exception.StackTrace);
            }

            if (printDetails)
            {
                Console.WriteLine("Message successfully sent to the topic.");
                Console.WriteLine("**********************************************************");
                Console.WriteLine();
            }
        }

        public static void SetServiceBusConnectionString(string serviceBusConnectionString)
        {
            ServiceBusSender.serviceBusConnectionString = serviceBusConnectionString;
        }

        public static void SetTopicName(string topicName)
        {
            ServiceBusSender.topicName = topicName;
        }
    }
}
