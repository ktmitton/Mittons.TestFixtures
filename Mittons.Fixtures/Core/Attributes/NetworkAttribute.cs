using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class NetworkAttribute : Attribute
    {
        public string Name { get; }

        public NetworkAttribute(string name)
        {
            Name = name;
        }
    }
}
