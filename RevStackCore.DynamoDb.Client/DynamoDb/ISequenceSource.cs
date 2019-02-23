using System;
namespace RevStackCore.DynamoDb.Client
{
    public interface IRequiresSchema
    {
        /// <summary>
        /// Unifed API to create any missing Tables, Data Structure Schema 
        /// or perform any other tasks dependencies require to run at Startup.
        /// </summary>
        void InitSchema();
    }

    public interface ISequenceSource : IRequiresSchema
    {
        long Increment(string key, long amount = 1);

        void Reset(string key, long startingAt = 0);
    }
}
