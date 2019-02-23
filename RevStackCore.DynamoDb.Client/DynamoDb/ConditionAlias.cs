using System;
namespace RevStackCore.DynamoDb.Client
{
    public static class ConditionAlias
    {
        public new const string Equals = "=";
        public const string NotEqual = "<>";
        public const string LessEqual = "<=";
        public const string Less = "<";
        public const string Greater = ">";
        public const string GreaterEqual = ">=";
        public const string StartsWith = "StartsWith";
        public const string Contains = "Contains";
        public const string EndsWith = "EndsWith";
        public const string In = "In";
        public const string Between = "Between";
        public const string Like = "Like";
        public const string False = "false";
    }

}
