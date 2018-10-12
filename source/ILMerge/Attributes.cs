namespace ILMerge
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly)]
    public class IncludeAssembliesAttribute : Attribute
    {
        public IncludeAssembliesAttribute(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ExcludeAssembliesAttribute : Attribute
    {
        public ExcludeAssembliesAttribute(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; }
    }
}
