using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LightestNight.System.Caching;
using LightestNight.System.Caching.Memory;
using Shouldly;
using Xunit;

namespace Caching.Memory.Tests
{
    public class CacheTests
    {
        private readonly ICache _sut = new Cache();
        
        [Fact]
        public async Task Should_Save_Item_Without_Expiry()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            
            // Act
            await _sut.Save(cacheKey, testObject);
            
            // Assert
            var expectedCacheKey = $"{nameof(TestObject)}:{cacheKey}";
            var keyedCacheStore = GetKeyedCache();
            keyedCacheStore.ContainsKey(expectedCacheKey).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Save_Item_With_Expiry_In_Future()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            
            // Act
            await _sut.Save(cacheKey, testObject, DateTime.UtcNow.AddMinutes(10));
            
            // Assert
            var expectedCacheKey = $"{nameof(TestObject)}:{cacheKey}";
            var keyedCacheStore = GetKeyedCache();
            keyedCacheStore.ContainsKey(expectedCacheKey).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Not_Save_Item_When_Expiry_Has_Passed()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            
            // Act
            await _sut.Save(cacheKey, testObject, DateTime.UtcNow.AddMinutes(-10));
            
            // Assert
            var expectedCacheKey = $"{nameof(TestObject)}:{cacheKey}";
            var keyedCacheStore = GetKeyedCache();
            keyedCacheStore.ContainsKey(expectedCacheKey).ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Get_Item_From_Cache()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject);
            
            // Act
            var result = await _sut.Get<TestObject>(cacheKey);
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(testObject);
        }

        [Theory]
        [InlineData("Tag1")]
        [InlineData("Tag2")]
        public async Task Should_Get_Item_From_Cache_By_Tag(string tag)
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject, tags: tag);
            
            // Act
            var result = (await _sut.GetByTag<TestObject>(tag)).ToArray();
            
            // Assert
            result.ShouldNotBeEmpty();
            result.Single().ShouldBe(testObject);
        }

        [Fact]
        public async Task Should_Get_Correct_Item_From_Multiple_Tags()
        {
            // Arrange
            const string tag1 = "Test";
            const string tag2 = "Object";
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject, null, tag1, tag2, "Superfluous Tag");
            
            // Act
            var result1 = (await _sut.GetByTag<TestObject>(tag1)).ToArray();
            var result2 = (await _sut.GetByTag<TestObject>(tag2)).ToArray();
            
            // Assert
            result1.ShouldNotBeEmpty();
            result1.Single().ShouldBe(testObject);
            
            result2.ShouldNotBeEmpty();
            result2.Single().ShouldBe(testObject);
        }

        [Fact]
        public async Task Should_Remove_Expired_Items()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject, DateTime.UtcNow.AddSeconds(5));
            var shouldExist = await _sut.Get<TestObject>(cacheKey);
            shouldExist.ShouldNotBeNull();
            
            // Act
            Thread.Sleep(5000);
            var result = await _sut.Get<TestObject>(cacheKey);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Remove_Expired_Tags()
        {
            // Arrange
            const string tag = "Tag";
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject, DateTime.UtcNow.AddSeconds(5), tag);
            var shouldExist = await _sut.GetByTag<TestObject>(tag);
            shouldExist.ShouldNotBeEmpty();
            
            // Act
            Thread.Sleep(5000);
            var result = await _sut.GetByTag<TestObject>(tag);
            
            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Remove_Deleted_From_Cache()
        {
            // Arrange
            var cacheKey = Guid.NewGuid().ToString("N");
            var testObject = new TestObject("Test", "Object", 100, new List<string>{"Test", "Object"});
            await _sut.Save(cacheKey, testObject);
            
            // Act
            await _sut.Delete<TestObject>(cacheKey);
            
            // Assert
            var expectedCacheKey = $"{nameof(TestObject)}:{cacheKey}";
            var keyedCacheStore = GetKeyedCache();
            keyedCacheStore.ContainsKey(expectedCacheKey).ShouldBeFalse();
        }

        private IDictionary<string, string> GetKeyedCache()
        {
            var keyedCacheField = typeof(Cache).GetField("KeyedCache", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (keyedCacheField == null)
                throw new NullReferenceException();

            var keyedCacheValue = keyedCacheField.GetValue(_sut);
            if (keyedCacheValue == null)
                throw new NullReferenceException();

            return (IDictionary<string, string>) keyedCacheValue;
        }
    }
}