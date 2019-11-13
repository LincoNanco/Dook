using System;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class TestFunction : DbFunction<TestFunctionModel>
    {
        [IsParameter(0)]
        public string Parameter0 { get; set; }
        [IsParameter(1)]
        public string Parameter1 { get; set ;}

        public TestFunction(QueryProvider provider) : base(provider) {}

    }
}