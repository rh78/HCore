using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using HCore.Cache;
using HCore.Cache.Impl;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace HCore.Test.Cache
{
    public class RedisCacheImplTests
    {
        private const int DatabaseIndex = 0;

        private const string Key = "cache-key";
        private const string SecondKey = "second-cache-key";
        private const string MissingKey = "missing-cache-key";
        private const string Text = "cached text";
        private const string SecondText = "second cached text";
        private const string ModelName = "Example Model";
        private const string SecondModelName = "Second Example Model";
        private const int ModelCount = 42;
        private const int SecondModelCount = 7;

        private static readonly TimeSpan ExpiresIn = TimeSpan.FromMinutes(5);
        private static readonly byte[] MessagePackNilBytes = { 0xC0 };

        private readonly Mock<IRedisConnectionPool> _redisConnectionPool = new Mock<IRedisConnectionPool>();
        private readonly Mock<IConnectionMultiplexer> _connectionMultiplexer = new Mock<IConnectionMultiplexer>();
        private readonly Mock<IDatabase> _database = new Mock<IDatabase>();

        private RedisValue _storedValue;

        public RedisCacheImplTests()
        {
            _redisConnectionPool.Setup(redisConnectionPool => redisConnectionPool.GetConnectionMultiplexer()).Returns(_connectionMultiplexer.Object);

            _connectionMultiplexer.Setup(connectionMultiplexer => connectionMultiplexer.GetDatabase(DatabaseIndex, null)).Returns(_database.Object);
        }

        [Fact]
        public async Task StoreAsync_ShouldCall_StringSetAsync_WithKeyAndExpiresIn_AndWhenAlways()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.StoreAsync(Key, Text, ExpiresIn);

            _database.Verify(database => database.StringSetAsync((RedisKey)Key, It.IsAny<RedisValue>(), (TimeSpan?)ExpiresIn, When.Always, CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task StoreAsync_StringValue_ShouldStore_TheUtf8Bytes()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            var expectedRedisValue = CreateRedisValue(Text);

            await redisCacheImpl.StoreAsync(Key, Text, ExpiresIn);

            _database.Verify(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.Is<RedisValue>(redisValue => redisValue == expectedRedisValue), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task StoreAsync_NullValue_ShouldStore_TheMessagePackNil()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            RedisValue expectedRedisValue = MessagePackNilBytes;

            await redisCacheImpl.StoreAsync<CacheTestModel>(Key, null, ExpiresIn);

            _database.Verify(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.Is<RedisValue>(redisValue => redisValue == expectedRedisValue), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StoreIfNotExistsAsync_ShouldReturn_TheStringSetAsyncResult(bool isStored)
        {
            SetupStringSet(isStored);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.StoreIfNotExistsAsync(Key, Text, ExpiresIn);

            result.Should().Be(isStored);
        }

        [Fact]
        public async Task StoreIfNotExistsAsync_ShouldCall_StringSetAsync_WithKeyAndExpiresIn_AndWhenNotExists()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.StoreIfNotExistsAsync(Key, Text, ExpiresIn);

            _database.Verify(database => database.StringSetAsync((RedisKey)Key, It.IsAny<RedisValue>(), (TimeSpan?)ExpiresIn, When.NotExists, CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task StoreIfNotExistsAsync_NullValue_ShouldStore_TheMessagePackNil()
        {
            SetupStringSet(true);

            var redisCacheImpl = CreateRedisCacheImpl();

            RedisValue expectedRedisValue = MessagePackNilBytes;

            var result = await redisCacheImpl.StoreIfNotExistsAsync<CacheTestModel>(Key, null, ExpiresIn);

            result.Should().BeTrue();

            _database.Verify(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.Is<RedisValue>(redisValue => redisValue == expectedRedisValue), It.IsAny<TimeSpan?>(), When.NotExists, It.IsAny<CommandFlags>()), Times.Once);
        }

        [Theory]
        [InlineData(Text)]
        [InlineData("Grüße aus Köln, Ärger über Öl")]
        [InlineData("She said \"yes\" and 'no'")]
        [InlineData("<b>bold</b> & <script>alert(1)</script>")]
        public async Task GetStringAsync_AfterStoreAsync_ShouldReturn_TheStoredString(string text)
        {
            SetupRoundTrip();

            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.StoreAsync(Key, text, ExpiresIn);

            var result = await redisCacheImpl.GetStringAsync(Key);

            result.Should().Be(text);
        }

        [Fact]
        public async Task GetObjectAsync_AfterStoreAsync_ShouldReturn_TheStoredObject()
        {
            SetupRoundTrip();

            var redisCacheImpl = CreateRedisCacheImpl();

            var cacheTestModel = CreateCacheTestModel(ModelName, ModelCount);

            await redisCacheImpl.StoreAsync(Key, cacheTestModel, ExpiresIn);

            var result = await redisCacheImpl.GetObjectAsync<CacheTestModel>(Key);

            result.Should().BeEquivalentTo(cacheTestModel);
        }

        [Fact]
        public async Task GetStringAsync_MissingKey_ShouldReturn_Null()
        {
            SetupStringGet(RedisValue.Null);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringAsync(MissingKey);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetObjectAsync_MissingKey_ShouldReturn_Null()
        {
            SetupStringGet(RedisValue.Null);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetObjectAsync<CacheTestModel>(MissingKey);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetStringAsync_ShouldCall_StringGetAsync_WithKey()
        {
            SetupStringGet(RedisValue.Null);

            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.GetStringAsync(Key);

            _database.Verify(database => database.StringGetAsync((RedisKey)Key, CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetStringsAsync_FoundKeys_ShouldReturn_TheStringsByKey()
        {
            var redisValues = new RedisValue[] { CreateRedisValue(Text), CreateRedisValue(SecondText) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringsAsync(new[] { Key, SecondKey });

            var expected = new Dictionary<string, string>
            {
                { Key, Text },
                { SecondKey, SecondText }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetStringsAsync_MissingKey_ShouldReturn_NullForThatKey()
        {
            var redisValues = new RedisValue[] { RedisValue.Null, CreateRedisValue(SecondText) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringsAsync(new[] { MissingKey, SecondKey });

            var expected = new Dictionary<string, string>
            {
                { MissingKey, null },
                { SecondKey, SecondText }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetStringsAsync_EmptyKeys_ShouldReturn_AnEmptyDictionary()
        {
            SetupStringGetMany(Array.Empty<RedisValue>());

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringsAsync(Array.Empty<string>());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStringsAsync_NullKeys_ShouldReturn_Null()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringsAsync(null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetStringsAsync_NullKeys_ShouldNotCall_StringGetAsync()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.GetStringsAsync(null);

            _database.Verify(database => database.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.Never);
        }

        [Fact]
        public async Task GetStringsAsync_NullAndEmptyKeys_ShouldCall_StringGetAsync_WithoutThem()
        {
            var redisValues = new RedisValue[] { CreateRedisValue(Text), CreateRedisValue(SecondText) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var expectedRedisKeys = new RedisKey[] { Key, SecondKey };

            await redisCacheImpl.GetStringsAsync(new[] { Key, null, string.Empty, SecondKey });

            _database.Verify(database => database.StringGetAsync(It.Is<RedisKey[]>(redisKeys => redisKeys.SequenceEqual(expectedRedisKeys)), CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetStringsAsync_DuplicateKeys_ShouldThrow_ArgumentException()
        {
            var redisValues = new RedisValue[] { CreateRedisValue(Text), CreateRedisValue(Text) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            Func<Task> getStringsAsync = () => redisCacheImpl.GetStringsAsync(new[] { Key, Key });

            await getStringsAsync.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetObjectsAsync_FoundKeys_ShouldReturn_TheObjectsByKey()
        {
            var cacheTestModel = CreateCacheTestModel(ModelName, ModelCount);

            var secondCacheTestModel = CreateCacheTestModel(SecondModelName, SecondModelCount);

            var redisValues = new RedisValue[] { CreateRedisValue(cacheTestModel), CreateRedisValue(secondCacheTestModel) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetObjectsAsync<CacheTestModel>(new[] { Key, SecondKey });

            var expected = new Dictionary<string, CacheTestModel>
            {
                { Key, cacheTestModel },
                { SecondKey, secondCacheTestModel }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetObjectsAsync_MissingKey_ShouldReturn_NullForThatKey()
        {
            var secondCacheTestModel = CreateCacheTestModel(SecondModelName, SecondModelCount);

            var redisValues = new RedisValue[] { RedisValue.Null, CreateRedisValue(secondCacheTestModel) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetObjectsAsync<CacheTestModel>(new[] { MissingKey, SecondKey });

            var expected = new Dictionary<string, CacheTestModel>
            {
                { MissingKey, null },
                { SecondKey, secondCacheTestModel }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetObjectsAsync_EmptyKeys_ShouldReturn_AnEmptyDictionary()
        {
            SetupStringGetMany(Array.Empty<RedisValue>());

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetObjectsAsync<CacheTestModel>(Array.Empty<string>());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetObjectsAsync_NullKeys_ShouldReturn_Null()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetObjectsAsync<CacheTestModel>(null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetObjectsAsync_NullKeys_ShouldNotCall_StringGetAsync()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.GetObjectsAsync<CacheTestModel>(null);

            _database.Verify(database => database.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.Never);
        }

        [Fact]
        public async Task GetObjectsAsync_NullAndEmptyKeys_ShouldCall_StringGetAsync_WithoutThem()
        {
            var cacheTestModel = CreateCacheTestModel(ModelName, ModelCount);

            var redisValues = new RedisValue[] { CreateRedisValue(cacheTestModel), CreateRedisValue(cacheTestModel) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            var expectedRedisKeys = new RedisKey[] { Key, SecondKey };

            await redisCacheImpl.GetObjectsAsync<CacheTestModel>(new[] { Key, null, string.Empty, SecondKey });

            _database.Verify(database => database.StringGetAsync(It.Is<RedisKey[]>(redisKeys => redisKeys.SequenceEqual(expectedRedisKeys)), CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetObjectsAsync_DuplicateKeys_ShouldThrow_ArgumentException()
        {
            var cacheTestModel = CreateCacheTestModel(ModelName, ModelCount);

            var redisValues = new RedisValue[] { CreateRedisValue(cacheTestModel), CreateRedisValue(cacheTestModel) };

            SetupStringGetMany(redisValues);

            var redisCacheImpl = CreateRedisCacheImpl();

            Func<Task> getObjectsAsync = () => redisCacheImpl.GetObjectsAsync<CacheTestModel>(new[] { Key, Key });

            await getObjectsAsync.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task InvalidateAsync_ShouldCall_KeyDeleteAsync_WithKey()
        {
            var redisCacheImpl = CreateRedisCacheImpl();

            await redisCacheImpl.InvalidateAsync(Key);

            _database.Verify(database => database.KeyDeleteAsync((RedisKey)Key, CommandFlags.None), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAvailableAsync_ShouldReturn_IsConnected(bool isConnected)
        {
            _connectionMultiplexer.SetupGet(connectionMultiplexer => connectionMultiplexer.IsConnected).Returns(isConnected);

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.IsAvailableAsync(default);

            result.Should().Be(isConnected);
        }

        [Fact]
        public async Task GetStringAsync_RedisTimeoutExceptionOnFirstCall_ShouldReturn_TheResultOfTheSecondCall()
        {
            var redisTimeoutException = new RedisTimeoutException("timeout", CommandStatus.Unknown);

            _database.SetupSequence(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(redisTimeoutException)
                .ReturnsAsync(CreateRedisValue(Text));

            var redisCacheImpl = CreateRedisCacheImpl();

            var result = await redisCacheImpl.GetStringAsync(Key);

            result.Should().Be(Text);

            _database.Verify(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetStringAsync_RedisTimeoutExceptionOnEveryCall_ShouldThrow_RedisTimeoutException_AfterThreeCalls()
        {
            var redisTimeoutException = new RedisTimeoutException("timeout", CommandStatus.Unknown);

            _database.Setup(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ThrowsAsync(redisTimeoutException);

            var redisCacheImpl = CreateRedisCacheImpl();

            Func<Task> getStringAsync = () => redisCacheImpl.GetStringAsync(Key);

            await getStringAsync.Should().ThrowAsync<RedisTimeoutException>();

            _database.Verify(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(3));
        }

        private RedisCacheImpl CreateRedisCacheImpl()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            return new RedisCacheImpl(_redisConnectionPool.Object, serviceProvider);
        }

        private void SetupStringSet(bool isStored)
        {
            _database.Setup(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(isStored);
        }

        private void SetupStringGet(RedisValue redisValue)
        {
            _database.Setup(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(redisValue);
        }

        private void SetupStringGetMany(RedisValue[] redisValues)
        {
            _database.Setup(database => database.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>())).ReturnsAsync(redisValues);
        }

        private void SetupRoundTrip()
        {
            _database
                .Setup(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((redisKey, redisValue, expiry, when, commandFlags) => _storedValue = redisValue)
                .ReturnsAsync(true);

            _database
                .Setup(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns(() => Task.FromResult(_storedValue));
        }

        private static CacheTestModel CreateCacheTestModel(string name, int count)
        {
            return new CacheTestModel
            {
                Name = name,
                Count = count
            };
        }

        private static RedisValue CreateRedisValue(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            return bytes;
        }

        private static RedisValue CreateRedisValue(CacheTestModel cacheTestModel)
        {
            var bytes = MessagePackSerializer.Serialize(cacheTestModel, ContractlessStandardResolverAllowPrivate.Options);

            return bytes;
        }
    }
}
