// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class CandidateSetTest
    {
        [Fact]
        public void Create_CreatesCandidateSet()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            // Act
            var candidateSet = new CandidateSet(candidates);

            // Assert
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(candidates[i].Score, state.Score);
                Assert.Null(state.Values);

                candidateSet.SetValidity(i, false);
                Assert.False(candidateSet.IsValidCandidate(i));
            }
        }

        [Fact]
        public void ReplaceEndpoint_WithEndpoint()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];

                var endpoint = CreateEndpoint($"/test{i}");
                var values = new RouteValueDictionary();

                // Act
                candidateSet.ReplaceEndpoint(i, endpoint, values);

                // Assert
                Assert.Same(endpoint, state.Endpoint);
                Assert.Same(values, state.Values);
                Assert.True(candidateSet.IsValidCandidate(i));
            }
        }

        [Fact]
        public void ReplaceEndpoint_WithEndpoint_Null()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];

                // Act
                candidateSet.ReplaceEndpoint(i, (Endpoint)null, null);

                // Assert
                Assert.Null(state.Endpoint);
                Assert.Null(state.Values);
                Assert.False(candidateSet.IsValidCandidate(i));
            }
        }

        [Fact]
        public void ReplaceEndpoint_EmptyList()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            // Act
            candidateSet.ReplaceEndpoint(0, Array.Empty<Endpoint>(), null);

            // Assert

            Assert.Null(candidateSet[0].Endpoint);
            Assert.False(candidateSet.IsValidCandidate(0));

            for (var i = 1; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];

                Assert.Same(endpoints[i], state.Endpoint);
            }
        }

        [Fact]
        public void ReplaceEndpoint_List_Beginning()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            var replacements = new RouteEndpoint[3];
            for (var i = 0; i < replacements.Length; i++)
            {
                replacements[i] = CreateEndpoint($"new /{i}");
            }

            var values = new RouteValueDictionary();

            candidateSet.SetValidity(0, false); // Has no effect. We always count new stuff as valid by default.

            // Act
            candidateSet.ReplaceEndpoint(0, replacements, values);

            // Assert
            Assert.Equal(12, candidateSet.Count);

            Assert.Same(replacements[0], candidateSet[0].Endpoint);
            Assert.Same(values, candidateSet[0].Values);
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.Same(replacements[1], candidateSet[1].Endpoint);
            Assert.Same(values, candidateSet[1].Values);
            Assert.True(candidateSet.IsValidCandidate(1));
            Assert.Same(replacements[2], candidateSet[2].Endpoint);
            Assert.Same(values, candidateSet[2].Values);
            Assert.True(candidateSet.IsValidCandidate(2));

            for (var i = 3; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i - 2], state.Endpoint);
            }
        }

        [Fact]
        public void ReplaceEndpoint_List_Middle()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            var replacements = new RouteEndpoint[3];
            for (var i = 0; i < replacements.Length; i++)
            {
                replacements[i] = CreateEndpoint($"new /{i}");
            }

            var values = new RouteValueDictionary();

            candidateSet.SetValidity(5, false); // Has no effect. We always count new stuff as valid by default.

            // Act
            candidateSet.ReplaceEndpoint(5, replacements, values);

            // Assert
            Assert.Equal(12, candidateSet.Count);

            for (var i = 0; i < 5; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
            }

            Assert.Same(replacements[0], candidateSet[5].Endpoint);
            Assert.Same(values, candidateSet[5].Values);
            Assert.True(candidateSet.IsValidCandidate(5));
            Assert.Same(replacements[1], candidateSet[6].Endpoint);
            Assert.Same(values, candidateSet[6].Values);
            Assert.True(candidateSet.IsValidCandidate(6));
            Assert.Same(replacements[2], candidateSet[7].Endpoint);
            Assert.Same(values, candidateSet[7].Values);
            Assert.True(candidateSet.IsValidCandidate(7));

            for (var i = 8; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i - 2], state.Endpoint);
            }
        }

        [Fact]
        public void ReplaceEndpoint_List_End()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            var candidateSet = new CandidateSet(candidates);

            var replacements = new RouteEndpoint[3];
            for (var i = 0; i < replacements.Length; i++)
            {
                replacements[i] = CreateEndpoint($"new /{i}");
            }

            var values = new RouteValueDictionary();

            candidateSet.SetValidity(9, false); // Has no effect. We always count new stuff as valid by default.

            // Act
            candidateSet.ReplaceEndpoint(9, replacements, values);

            // Assert
            Assert.Equal(12, candidateSet.Count);

            for (var i = 0; i < 9; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
            }

            Assert.Same(replacements[0], candidateSet[9].Endpoint);
            Assert.Same(values, candidateSet[9].Values);
            Assert.True(candidateSet.IsValidCandidate(9));
            Assert.Same(replacements[1], candidateSet[10].Endpoint);
            Assert.Same(values, candidateSet[10].Values);
            Assert.True(candidateSet.IsValidCandidate(10));
            Assert.Same(replacements[2], candidateSet[11].Endpoint);
            Assert.Same(values, candidateSet[11].Values);
            Assert.True(candidateSet.IsValidCandidate(11));
        }

        [Fact]
        public void Create_CreatesCandidateSet_TestConstructor()
        {
            // Arrange
            var count = 10;
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var values = new RouteValueDictionary[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                values[i] = new RouteValueDictionary()
                {
                    { "i", i }
                };
            }

            // Act
            var candidateSet = new CandidateSet(endpoints, values, Enumerable.Range(0, count).ToArray());

            // Assert
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(i, state.Score);
                Assert.NotNull(state.Values);
                Assert.Equal(i, state.Values["i"]);

                candidateSet.SetValidity(i, false);
                Assert.False(candidateSet.IsValidCandidate(i));
            }
        }

        private RouteEndpoint CreateEndpoint(string template)
        {
            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template),
                0,
                EndpointMetadataCollection.Empty,
                "test");
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
        {
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                NullLoggerFactory.Instance,
                Mock.Of<ParameterPolicyFactory>(),
                Mock.Of<EndpointSelector>(),
                policies);
        }
    }
}
