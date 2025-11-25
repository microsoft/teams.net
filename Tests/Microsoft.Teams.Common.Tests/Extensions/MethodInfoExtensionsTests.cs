using Microsoft.Teams.Common.Extensions;

namespace Microsoft.Teams.Common.Tests.Extensions;

public class MethodInfoExtensionsTests
{
    private class TestClass
    {
        public bool VoidCalled { get; private set; }
        public object? Value { get; private set; }

        public void VoidMethod()
        {
            VoidCalled = true;
        }

        public int SyncMethod(int x, int y)
        {
            return x + y;
        }

        public async Task AsyncVoidMethod()
        {
            await Task.Delay(1);
            VoidCalled = true;
        }

        public async Task<int> AsyncValueMethod(int x, int y)
        {
            await Task.Delay(1);
            return x * y;
        }

        public Task<object?> AsyncObjectMethod(object? value)
        {
            Value = value;
            return Task.FromResult(value);
        }
    }

    [Fact]
    public async Task InvokeAsync_SyncMethod_ReturnsResult()
    {
        var obj = new TestClass();
        var method = typeof(TestClass).GetMethod(nameof(TestClass.SyncMethod));
        var result = await method!.InvokeAsync(obj, new object[] { 2, 3 });
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task InvokeAsync_AsyncValueMethod_ReturnsResult()
    {
        var obj = new TestClass();
        var method = typeof(TestClass).GetMethod(nameof(TestClass.AsyncValueMethod));
        var result = await method!.InvokeAsync(obj, new object[] { 2, 4 });
        Assert.Equal(8, result);
    }

    [Fact]
    public async Task InvokeAsync_AsyncVoidMethod_ReturnsNull()
    {
        var obj = new TestClass();
        var method = typeof(TestClass).GetMethod(nameof(TestClass.AsyncVoidMethod));
        var result = await method!.InvokeAsync(obj, null);
        Assert.True(obj.VoidCalled);
    }

    [Fact]
    public async Task InvokeAsync_AsyncObjectMethod_ReturnsObject()
    {
        var obj = new TestClass();
        var method = typeof(TestClass).GetMethod(nameof(TestClass.AsyncObjectMethod));
        var input = "test";
        var result = await method!.InvokeAsync(obj, new object?[] { input });
        Assert.Equal(input, result);
        Assert.Equal(input, obj.Value);
    }

    [Fact]
    public async Task InvokeAsync_VoidMethod_ReturnsNull()
    {
        var obj = new TestClass();
        var method = typeof(TestClass).GetMethod(nameof(TestClass.VoidMethod));
        var result = await method!.InvokeAsync(obj, null);
        Assert.Null(result);
        Assert.True(obj.VoidCalled);
    }
}