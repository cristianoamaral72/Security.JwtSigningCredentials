using FluentAssertions;
using Jwks.Manager.Interfaces;
using Jwks.Manager.Jwk;
using Jwks.Manager.Jwks;
using Jwks.Manager.Store.FileSystem;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Jwks.Manager.Tests.Jwks
{
    public class JsonWebKeySetServiceTests
    {
        private readonly JwksService _jwksService;
        private readonly IJsonWebKeyStore _store;
        private readonly Mock<IOptions<JwksOptions>> _options;

        public JsonWebKeySetServiceTests()
        {
            _options = new Mock<IOptions<JwksOptions>>();
            _store = new FileSystemStore(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "/JsonWebKeySetServiceTests")), _options.Object);
            _jwksService = new JwksService(_store, new JwkService(), _options.Object);
            _options.Setup(s => s.Value).Returns(new JwksOptions());
        }

        [Fact]
        public void ShouldGenerateDefaultSigning()
        {
            _options.Setup(s => s.Value).Returns(new JwksOptions());
            var sign = _jwksService.Generate();
            var current = _jwksService.GetCurrent();
            current.Kid.Should().Be(sign.Kid);
        }

        [Fact]
        public void ShouldGenerateFiveDefaultSigning()
        {
            _options.Setup(s => s.Value).Returns(new JwksOptions());
            _store.Clear();
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < 5; i++)
            {
                var sign = _jwksService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _jwksService.GetLastKeysCredentials(5);
            foreach (var securityKey in current)
            {
                keysGenerated.Should().Contain(s => s.Kid == securityKey.KeyId);
            }
        }
        [Fact]
        public void ShouldGenerateRsa()
        {
            _options.Setup(s => s.Value).Returns(new JwksOptions());
            var sign = _jwksService.Generate();
            var current = _store.GetCurrentKey();
            current.KeyId.Should().Be(sign.Kid);
        }

        [Fact]
        public void ShouldGenerateFiveRsa()
        {
            _store.Clear();
            _options.Setup(s => s.Value).Returns(new JwksOptions() { Algorithm = Algorithm.RS256 });

            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < 5; i++)
            {
                var sign = _jwksService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _store.Get(10);
            foreach (var securityKey in current)
            {
                keysGenerated.Should().Contain(s => s.Kid == securityKey.KeyId && s.Algorithm == SecurityAlgorithms.RsaSha256);
            }
        }


        [Fact]
        public void ShouldGenerateECDsa()
        {
            _options.Setup(s => s.Value).Returns(new JwksOptions() { Algorithm = Algorithm.ES256 });
            var sign = _jwksService.Generate();
            var current = _store.GetCurrentKey();
            current.KeyId.Should().Be(sign.Kid);
            current.Algorithm.Should().Be(SecurityAlgorithms.EcdsaSha256);
        }

        [Fact]
        public void ShouldGenerateFiveCEDsa()
        {
            _options.Setup(s => s.Value).Returns(new JwksOptions() { Algorithm = Algorithm.ES512 });
            _store.Clear();
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < 5; i++)
            {
                var sign = _jwksService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _store.Get(50);
            foreach (var securityKey in current)
            {
                keysGenerated.Should().Contain(s => s.Kid == securityKey.KeyId && s.Algorithm == SecurityAlgorithms.EcdsaSha512);
            }
        }
    }
}
