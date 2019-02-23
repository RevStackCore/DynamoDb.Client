using System;
using System.Collections.Generic;

namespace RevStackCore.DynamoDb.Client
{
    public class QueryDataContext
    {
        public IQueryData Dto { get; set; }
        public Dictionary<string, string> DynamicParams { get; set; }
        public IRequest Request { get; set; }
    }
}
