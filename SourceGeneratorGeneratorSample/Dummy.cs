using System;
namespace SourceGeneratorGeneratorSample;

class BrabrabraAttribute : Attribute
{
    public BrabrabraAttribute(Type t)
    {
        t = t.BaseType;
    }
}
