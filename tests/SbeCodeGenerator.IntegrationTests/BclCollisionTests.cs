using Xunit;

// NOTE: This test file is intentionally declared INSIDE the schema's namespace.
// Inside an enclosing namespace block, C# name resolution prefers types declared
// in that namespace over types brought in by `using` directives — so `Boolean`
// here unambiguously binds to the user-declared SBE enum, not System.Boolean.
//
// Consumer code that uses `using Bcl.Collision.Test.V0;` from a DIFFERENT
// namespace will see CS0104 ambiguity. That is a language rule, not a generator
// bug; the user should disambiguate via a using-alias (e.g.
// `using Boolean = Bcl.Collision.Test.V0.Boolean;`) or fully qualified names.
namespace Bcl.Collision.Test.V0
{
    /// <summary>
    /// Regression test for issue #164 (B3 EntryPoint v8.4.2):
    /// proves the GENERATED code for an SBE enum whose name collides with a BCL
    /// type (here <c>Boolean</c>) compiles and runs correctly under
    /// <c>ImplicitUsings=enable</c> + <c>TreatWarningsAsErrors=true</c>.
    /// </summary>
    public class BclCollisionTests
    {
        [Fact]
        public void BooleanEnum_HasUserDeclaredValues_NotSystemBoolean()
        {
            Assert.Equal((byte)0, (byte)Boolean.FALSE_VALUE);
            Assert.Equal((byte)1, (byte)Boolean.TRUE_VALUE);
        }

        [Fact]
        public void QuoteRequest_BooleanField_RoundTrips()
        {
            Span<byte> buffer = stackalloc byte[QuoteRequestData.MESSAGE_SIZE];
            ref var msg = ref System.Runtime.CompilerServices.Unsafe.As<byte, QuoteRequestData>(
                ref System.Runtime.InteropServices.MemoryMarshal.GetReference(buffer));
            msg.IsPrivate = Boolean.TRUE_VALUE;

            Assert.Equal(Boolean.TRUE_VALUE, msg.IsPrivate);
        }

        [Fact]
        public void QuoteRequest_ConstantFields_BindToUserDeclaredBoolean()
        {
            // These constants would fail to compile (CS0117) if `Boolean` here
            // bound to System.Boolean, since System.Boolean has no
            // TRUE_VALUE/FALSE_VALUE members.
            Assert.Equal(Boolean.TRUE_VALUE, QuoteRequestData.DEFAULT_PRIVATE);
            Assert.Equal(Boolean.FALSE_VALUE, QuoteRequestData.DEFAULT_PUBLIC);
        }
    }
}

