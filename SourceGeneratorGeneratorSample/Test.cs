using System;
using System.Collections.Generic;
using System.Text;

namespace SourceGeneratorGeneratorSample
{
    [CopySource("BrabrabraSource", typeof(BrabrabraAttribute))]
    partial class Test
    {
        public Test()
        {
            Console.WriteLine(BrabrabraSource);
        }
    }
}
