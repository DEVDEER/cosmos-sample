namespace devdeer.CosmosSample.Ui.CreationConsole
{
    using System;
    using System.Linq;

    public class SecretsModel
    {
        #region properties

        public string CosmosDbName { get; set; }

        public string CosmosDbSecret { get; set; }

        public string CosmosDbDatabase { get; set; }

        public string CosmosDbContainer { get; set; }

        public int DegreeOfParallelism { get; set; }

        #endregion
    }
}