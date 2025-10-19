extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for pipeline method validation in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerPipelineTests
    {
        /// <summary>
        /// Tests that pipeline methods missing parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task PipelineMethodMissingParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void {|RELAY_GEN_002:ExecutePipeline|}()
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with valid signatures do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidPipelineMethod_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with invalid Order values produce diagnostics.
        /// </summary>
        [Fact]
        public async Task PipelineInvalidOrderValue_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = ""invalid"")]
    public void {|RELAY_GEN_209:ExecutePipeline|}(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with complex parameters work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineComplexParameters_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with complex parameter types work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineComplexParameterTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }

    [Pipeline]
    public void ExecutePipelineWithList(List<string> items, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with various parameter types work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineVariousParameterTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(int param, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with custom attribute combinations work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineCustomAttributes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Name = ""CustomPipeline"", Order = 50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with zero order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineZeroOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = 0)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with negative order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineNegativeOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = -50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with positive order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelinePositiveOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = 50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}