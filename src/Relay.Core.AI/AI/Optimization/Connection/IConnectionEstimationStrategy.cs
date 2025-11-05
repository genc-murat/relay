using System;

namespace Relay.Core.AI.Optimization.Connection;

internal interface IConnectionEstimationStrategy
{
    int EstimateConnections();
}
