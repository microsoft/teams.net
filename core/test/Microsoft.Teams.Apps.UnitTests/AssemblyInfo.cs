// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;

// Tests in this assembly use process-global state (System.Diagnostics.ActivitySource listeners,
// OpenTelemetry.Baggage.Current). Running them in parallel causes captures from one test to
// observe spans/metrics started in another. Disabling parallelization keeps the captures isolated.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
