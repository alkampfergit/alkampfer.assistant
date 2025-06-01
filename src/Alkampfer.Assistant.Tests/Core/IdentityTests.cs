using System;
using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Tests.Core;

public class IdentityTests
{

}

internal class TestId : Identity
{
    public TestId(string value) : base(value)
    {
    }

    protected override string Prefix => "test";
}
