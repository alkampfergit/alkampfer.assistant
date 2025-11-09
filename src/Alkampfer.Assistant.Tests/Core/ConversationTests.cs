using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Interfaces;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class ConversationTests
{
    [Fact]
    public async Task AddMessageAsync_Should_Add_Message_To_Conversation()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        await conversation.AddMessageAsync(MessageRole.User, "Hello", CancellationToken.None);

        // Assert
        var messages = await conversation.GetMessagesAsync(CancellationToken.None);
        Assert.Single(messages);
        Assert.Equal(MessageRole.User, messages[0].Role);
        Assert.Equal("Hello", messages[0].Content);
    }

    [Fact]
    public async Task AddMessageAsync_Should_Add_Multiple_Messages_In_Order()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        await conversation.AddMessageAsync(MessageRole.System, "You are a helpful assistant", CancellationToken.None);
        await conversation.AddMessageAsync(MessageRole.User, "What is 2+2?", CancellationToken.None);
        await conversation.AddMessageAsync(MessageRole.Assistant, "4", CancellationToken.None);

        // Assert
        var messages = await conversation.GetMessagesAsync(CancellationToken.None);
        Assert.Equal(3, messages.Count);
        Assert.Equal(MessageRole.System, messages[0].Role);
        Assert.Equal("You are a helpful assistant", messages[0].Content);
        Assert.Equal(MessageRole.User, messages[1].Role);
        Assert.Equal("What is 2+2?", messages[1].Content);
        Assert.Equal(MessageRole.Assistant, messages[2].Role);
        Assert.Equal("4", messages[2].Content);
    }

    [Fact]
    public async Task GetMessagesAsync_Should_Return_Empty_List_When_No_Messages()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        var messages = await conversation.GetMessagesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetMessagesAsync_Should_Return_Copy_Of_Messages()
    {
        // Arrange
        var conversation = new Conversation();
        await conversation.AddMessageAsync(MessageRole.User, "Test", CancellationToken.None);

        // Act
        var messages1 = await conversation.GetMessagesAsync(CancellationToken.None);
        await conversation.AddMessageAsync(MessageRole.Assistant, "Response", CancellationToken.None);
        var messages2 = await conversation.GetMessagesAsync(CancellationToken.None);

        // Assert
        Assert.Single(messages1);
        Assert.Equal(2, messages2.Count);
    }

    [Fact]
    public async Task AddMessageAsync_Should_Throw_When_Content_Is_Null()
    {
        // Arrange
        var conversation = new Conversation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => conversation.AddMessageAsync(MessageRole.User, null!, CancellationToken.None));
    }

    [Theory]
    [InlineData(MessageRole.System)]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    public async Task AddMessageAsync_Should_Support_All_Message_Roles(MessageRole role)
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        await conversation.AddMessageAsync(role, "Test message", CancellationToken.None);

        // Assert
        var messages = await conversation.GetMessagesAsync(CancellationToken.None);
        Assert.Single(messages);
        Assert.Equal(role, messages[0].Role);
    }

    [Fact]
    public async Task Conversation_Should_Be_Thread_Safe()
    {
        // Arrange
        var conversation = new Conversation();
        var tasks = new List<Task>();
        const int messageCount = 100;

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
                await conversation.AddMessageAsync(MessageRole.User, $"Message {index}", CancellationToken.None)));
        }

        await Task.WhenAll(tasks);

        // Assert
        var messages = await conversation.GetMessagesAsync(CancellationToken.None);
        Assert.Equal(messageCount, messages.Count);
    }

    [Fact]
    public void Statistics_Should_Be_Initialized_With_Zero_Values()
    {
        // Arrange
        var conversation = new Conversation();

        // Assert
        Assert.NotNull(conversation.Statistics);
        Assert.Equal(0, conversation.Statistics.TotalInputTokens);
        Assert.Equal(0, conversation.Statistics.TotalOutputTokens);
        Assert.Equal(0, conversation.Statistics.LastCallInputTokens);
        Assert.Equal(0, conversation.Statistics.LastCallOutputTokens);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Update_Last_Call_Tokens()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        conversation.Statistics.UpdateStats(100, 50);

        // Assert
        Assert.Equal(100, conversation.Statistics.LastCallInputTokens);
        Assert.Equal(50, conversation.Statistics.LastCallOutputTokens);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Accumulate_Total_Tokens()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        conversation.Statistics.UpdateStats(100, 50);
        conversation.Statistics.UpdateStats(200, 75);
        conversation.Statistics.UpdateStats(150, 100);

        // Assert
        Assert.Equal(450, conversation.Statistics.TotalInputTokens);
        Assert.Equal(225, conversation.Statistics.TotalOutputTokens);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Update_Last_Call_With_Latest_Values()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        conversation.Statistics.UpdateStats(100, 50);
        conversation.Statistics.UpdateStats(200, 75);

        // Assert
        Assert.Equal(200, conversation.Statistics.LastCallInputTokens);
        Assert.Equal(75, conversation.Statistics.LastCallOutputTokens);
        Assert.Equal(300, conversation.Statistics.TotalInputTokens);
        Assert.Equal(125, conversation.Statistics.TotalOutputTokens);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Throw_When_InputTokens_Is_Negative()
    {
        // Arrange
        var conversation = new Conversation();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => conversation.Statistics.UpdateStats(-1, 50));
        Assert.Equal("inputTokens", exception.ParamName);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Throw_When_OutputTokens_Is_Negative()
    {
        // Arrange
        var conversation = new Conversation();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => conversation.Statistics.UpdateStats(100, -1));
        Assert.Equal("outputTokens", exception.ParamName);
    }

    [Fact]
    public void Statistics_UpdateStats_Should_Accept_Zero_Values()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        conversation.Statistics.UpdateStats(0, 0);

        // Assert
        Assert.Equal(0, conversation.Statistics.LastCallInputTokens);
        Assert.Equal(0, conversation.Statistics.LastCallOutputTokens);
        Assert.Equal(0, conversation.Statistics.TotalInputTokens);
        Assert.Equal(0, conversation.Statistics.TotalOutputTokens);
    }

    [Fact]
    public void Statistics_Should_Track_Multiple_Updates_Correctly()
    {
        // Arrange
        var conversation = new Conversation();

        // Act - Simulate multiple API calls
        conversation.Statistics.UpdateStats(500, 250);  // First call
        conversation.Statistics.UpdateStats(300, 150);  // Second call
        conversation.Statistics.UpdateStats(400, 200);  // Third call

        // Assert
        Assert.Equal(400, conversation.Statistics.LastCallInputTokens);
        Assert.Equal(200, conversation.Statistics.LastCallOutputTokens);
        Assert.Equal(1200, conversation.Statistics.TotalInputTokens);
        Assert.Equal(600, conversation.Statistics.TotalOutputTokens);
    }

    [Fact]
    public void Statistics_Should_Be_Same_Instance_Across_Calls()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        var stats1 = conversation.Statistics;
        conversation.Statistics.UpdateStats(100, 50);
        var stats2 = conversation.Statistics;

        // Assert
        Assert.Same(stats1, stats2);
        Assert.Equal(100, stats1.TotalInputTokens);
        Assert.Equal(100, stats2.TotalInputTokens);
    }
}
