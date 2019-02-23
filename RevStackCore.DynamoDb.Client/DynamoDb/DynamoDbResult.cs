using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace RevStackCore.DynamoDb.Client
{
    public class DynamoDbResult<T> 
    {
        public IEnumerable<T> Data { get; set; }
        public Dictionary<string,AttributeValue> LastEvaluatedKey { get; set; }
    }
}
