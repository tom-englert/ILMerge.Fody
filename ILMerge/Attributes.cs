using System;

namespace ILMerge
{
    /// <summary>
    /// A regular expression matching the assembly names to include in merging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IncludeAssembliesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeAssembliesAttribute"/> class.
        /// </summary>
        /// <param name="pattern">The regular expression pattern.</param>
        public IncludeAssembliesAttribute(string pattern)
        {
            Pattern = pattern;
        }

        /// <summary>
        /// Gets the regular expression pattern.
        /// </summary>
        public string Pattern { get; }
    }

    /// <summary>
    /// A regular expression matching the assembly names to exclude from merging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ExcludeAssembliesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeAssembliesAttribute"/> class.
        /// </summary>
        /// <param name="pattern">The regular expression pattern.</param>
        public ExcludeAssembliesAttribute(string pattern)
        {
            Pattern = pattern;
        }

        /// <summary>
        /// Gets the regular expression pattern.
        /// </summary>
        public string Pattern { get; }
    }

    /// <summary>
    /// A switch to control whether the imported types are hidden (made private) or keep their visibility unchanged. Default is 'true'
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class HideImportedTypesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HideImportedTypesAttribute"/> class.
        /// </summary>
        /// <param name="value">if set to <c>true</c>, imported types are hidden (private/internal).</param>
        public HideImportedTypesAttribute(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether imported types are hidden (private/internal).
        /// </summary>
        public bool Value { get; }
    }
}
