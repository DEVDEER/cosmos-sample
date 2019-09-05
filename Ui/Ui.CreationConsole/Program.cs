/*
 * Idea taken from https://github.com/Azure/azure-cosmos-dotnet-v2/tree/master/samples/documentdb-benchmark
 */

namespace devdeer.CosmosSample.Ui.CreationConsole
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Configuration;

    internal class Program
    {
        #region constants

        private static int _degreeOfParallelism = -1;
        private const int ItemsPerTask = 300;
        private const int MinThreadPoolSize = 100;

        private static int _currentCollectionThroughput;
        private static long _documentsInserted;

        private static int _pendingTaskCount;
        private static readonly ConcurrentDictionary<int, double> RequestUnitsConsumed = new ConcurrentDictionary<int, double>();

        #endregion

        #region methods

        private static async Task InsertDocumentAsync(int taskId, Container container, long numberOfDocumentsToInsert)
        {
            RequestUnitsConsumed[taskId] = 0;
            var random = new Random(DateTime.Now.Millisecond);
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
                var response = await container.CreateItemAsync(order);
                RequestUnitsConsumed[taskId] += response.RequestCharge;
                Interlocked.Increment(ref _documentsInserted);
            }
            Interlocked.Decrement(ref _pendingTaskCount);
        }

        private static async Task InsertDocumentsAsync(Container container, long numberOfDocumentsToInsert)
        {
            var taskCount = 0;
            if (_degreeOfParallelism == -1)
            {
                // set TaskCount = 10 for each 10k RUs, minimum 1, maximum 250
                taskCount = Math.Max(_currentCollectionThroughput / ItemsPerTask, 1);
                taskCount = Math.Min(taskCount, 250);
            }
            else
            {
                taskCount = _degreeOfParallelism;
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
                tasks.Add(InsertDocumentAsync(i, container, docs));
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
                    $"Inserted {currentCount} docs @ {Math.Round(_documentsInserted / seconds)} writes/s, {Math.Round(ruPerSecond)} RU/s of {_currentCollectionThroughput} RU/s configured ({Math.Round(ruPerMonth / (1000 * 1000 * 1000))}B max monthly 1KB reads)");
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
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            if (isDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets<SecretsModel>();
            }
            var configuration = builder.Build();

            string cosmosAccountName;
            string cosmosAccountSecret;
            string cosmosDatabase;
            string cosmosContainer;

            if (isDevelopment)
            {
                cosmosAccountName = configuration["SecretsModel:CosmosDbName"];
                cosmosAccountSecret = configuration["SecretsModel:CosmosDbSecret"];
                cosmosDatabase = configuration["SecretsModel:CosmosDbDatabase"];
                cosmosContainer = configuration["SecretsModel:CosmosDbContainer"];
                _degreeOfParallelism = int.Parse(configuration["SecretsModel:DegreeOfParallelism"]);
            }
            else
            {
                cosmosAccountName = Environment.GetEnvironmentVariable("COSMOS_ACCOUNT");
                cosmosAccountSecret = Environment.GetEnvironmentVariable("COSMOS_SECRET");
                cosmosDatabase = Environment.GetEnvironmentVariable("COSMOS_DATABASE");
                cosmosContainer = Environment.GetEnvironmentVariable("COSMOS_CONTAINER");
                _degreeOfParallelism = int.Parse(Environment.GetEnvironmentVariable("MAX_DOP") ?? "-1");
            }
            // check configuration values
            if (string.IsNullOrEmpty(cosmosAccountName) || string.IsNullOrEmpty(cosmosAccountSecret) || string.IsNullOrEmpty(cosmosDatabase) || string.IsNullOrEmpty(cosmosContainer))
            {
                Console.WriteLine("Either configure app-secrets when using development-mode or define environment variables COSMOS_ACCOUNT, COSMOS_SECRET, COSMOS_DATABASE, COSMOS_CONTAINER and MAX_DOP.");
                return;
            }
            // all config values are set
            ThreadPool.SetMinThreads(MinThreadPoolSize, MinThreadPoolSize);
            var connectionPolicy = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(0, 0, 10)
            };
            using (var client = new CosmosClient(
                cosmosAccountName,
                cosmosAccountSecret,
                connectionPolicy))
            {
                var database = client.GetDatabase(cosmosDatabase);
                var container = database.GetContainer(cosmosContainer);
                _currentCollectionThroughput = await database.ReadThroughputAsync() ?? 400;
                await InsertDocumentsAsync(container, 5000);
                Console.WriteLine("DocumentDBBenchmark completed successfully.");
            }
            Console.WriteLine("Finished");
        }

        #endregion
    }
}