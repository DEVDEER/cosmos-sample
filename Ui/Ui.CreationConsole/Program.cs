/*
 * Idea taken from https://github.com/Azure/azure-cosmos-dotnet-v2/tree/master/samples/documentdb-benchmark
 */

namespace devdeer.CosmosSample.Ui.CreationConsole
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    internal class Program
    {
        #region constants

        private const int CurrentCollectionThroughput = 2000;
        private const int DegreeOfParallelism = -1;
        private const int ItemsPerTask = 300;
        private const int MinThreadPoolSize = 100;

        private static long _documentsInserted;

        private static int _pendingTaskCount;
        private static readonly ConcurrentDictionary<int, double> RequestUnitsConsumed = new ConcurrentDictionary<int, double>();

        #endregion

        #region methods

        private static DocumentCollection GetCollectionIfExists(DocumentClient client, string databaseName, string collectionName)
        {
            if (GetDatabaseIfExists(client, databaseName) == null)
            {
                return null;
            }
            return client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(databaseName)).Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();
        }

        private static Database GetDatabaseIfExists(DocumentClient client, string databaseName)
        {
            return client.CreateDatabaseQuery().Where(d => d.Id == databaseName).AsEnumerable().FirstOrDefault();
        }

        private static async Task InsertDocumentAsync(int taskId, DocumentClient client, long numberOfDocumentsToInsert)
        {
            RequestUnitsConsumed[taskId] = 0;
            var random = new Random(DateTime.Now.Millisecond);
            var collectionUri = UriFactory.CreateDocumentCollectionUri("Sample", "orders");
            for (var i = 0; i < numberOfDocumentsToInsert; i++)
            {
                var order = new
                {
                    id = Guid.NewGuid().ToString(),
                    productId = random.Next(20, 30),
                    timeStamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    customerLogin = "hello@test.de",
                    amount = random.Next(1, 100)
                };
                var response = await client.CreateDocumentAsync(collectionUri, order);
                var partition = response.SessionToken.Split(':')[0];
                RequestUnitsConsumed[taskId] += response.RequestCharge;
                Interlocked.Increment(ref _documentsInserted);
            }
            Interlocked.Decrement(ref _pendingTaskCount);
        }

        private static async Task InsertDocumentsAsync(DocumentClient client, long numberOfDocumentsToInsert)
        {
            var taskCount = 0;
            if (DegreeOfParallelism == -1)
            {
                // set TaskCount = 10 for each 10k RUs, minimum 1, maximum 250
                taskCount = Math.Max(CurrentCollectionThroughput / ItemsPerTask, 1);
                taskCount = Math.Min(taskCount, 250);
            }
            else
            {
                taskCount = DegreeOfParallelism;
            }
            Console.WriteLine($"Using {taskCount} parallel tasks.");
            _pendingTaskCount = taskCount;
            var tasks = new List<Task>
            {
                LogOutputStats()
            };
            var documentsPerTask = numberOfDocumentsToInsert / taskCount;
            var lastTaskAdd = numberOfDocumentsToInsert % taskCount;
            for (var i = 0; i < taskCount; i++)
            {
                var docs = documentsPerTask;
                if (i == taskCount - 1)
                {
                    docs += lastTaskAdd;
                }
                tasks.Add(InsertDocumentAsync(i, client, docs));
            }
            await Task.WhenAll(tasks);
            Console.WriteLine("Done");
        }

        private static async Task LogOutputStats()
        {
            long lastCount = 0;
            //double lastRequestUnits = 0;
            //double lastSeconds = 0;
            double requestUnits = 0;
            double ruPerSecond = 0;
            double ruPerMonth = 0;
            var watch = new Stopwatch();
            watch.Start();
            while (_pendingTaskCount > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var seconds = watch.Elapsed.TotalSeconds;
                requestUnits = 0;
                foreach (var taskId in RequestUnitsConsumed.Keys)
                {
                    requestUnits += RequestUnitsConsumed[taskId];
                }
                var currentCount = _documentsInserted;
                ruPerSecond = requestUnits / seconds;
                ruPerMonth = ruPerSecond * 86400 * 30;
                Console.WriteLine(
                    "Inserted {0} docs @ {1} writes/s, {2} RU/s ({3}B max monthly 1KB reads)",
                    currentCount,
                    Math.Round(_documentsInserted / seconds),
                    Math.Round(ruPerSecond),
                    Math.Round(ruPerMonth / (1000 * 1000 * 1000)));
                lastCount = _documentsInserted;
                //lastSeconds = seconds;
                //lastRequestUnits = requestUnits;
            }
            var totalSeconds = watch.Elapsed.TotalSeconds;
            ruPerSecond = requestUnits / totalSeconds;
            ruPerMonth = ruPerSecond * 86400 * 30;
            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine("--------------------------------------------------------------------- ");
            Console.WriteLine(
                "Inserted {0} docs @ {1} writes/s, {2} RU/s ({3}B max monthly 1KB reads)",
                lastCount,
                Math.Round(_documentsInserted / watch.Elapsed.TotalSeconds),
                Math.Round(ruPerSecond),
                Math.Round(ruPerMonth / (1000 * 1000 * 1000)));
            Console.WriteLine("--------------------------------------------------------------------- ");
        }

        private static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(MinThreadPoolSize, MinThreadPoolSize);
            var connectionPolicy = new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp,
                RequestTimeout = new TimeSpan(1, 0, 0),
                MaxConnectionLimit = 1000,
                RetryOptions = new RetryOptions
                {
                    MaxRetryAttemptsOnThrottledRequests = 10,
                    MaxRetryWaitTimeInSeconds = 60
                }
            };
            using (var client = new DocumentClient(
                new Uri("https://cos-ms-test.documents.azure.com:443/"),
                "zF5NUhiFmVXbFT4veJbE5YakKAFcVL3zNlqGck12zRhAWqBUPtq2gh25lMJ6JIjVbqDKIWAVg2UKBau9CEwXcA==",
                connectionPolicy))
            {
                await InsertDocumentsAsync(client, 5000);
                Console.WriteLine("DocumentDBBenchmark completed successfully.");
            }
            Console.WriteLine("Finished");
        }

        #endregion
    }
}