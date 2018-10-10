namespace ILMerge
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly)]
    public class IncludeAssembliesAttribute : Attribute
    {
        public IncludeAssembliesAttribute(params string[] includeExpressions)
        {
            IncludeExpressions = includeExpressions;
        }

        public string[] IncludeExpressions { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ExcludeAssembliesAttribute : Attribute
    {
        public ExcludeAssembliesAttribute(params string[] excludeExpressions)
        {
            ExcludeExpressions = excludeExpressions;
        }

        public string[] ExcludeExpressions { get; }
    }
}
