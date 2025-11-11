using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Llm;
using Moq;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class ConversationAgentTests
{
    private static Mock<ILanguageModel> CreateMockLanguageModel(bool supportConversation = false)
    {
        var mock = new Mock<ILanguageModel>();
        mock.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = supportConversation });
        return mock;
    }

    [Fact]
    public void Constructor_Should_Throw_When_LanguageModel_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConversationAgent(null!));
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_No_Conversation_In_Context()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = false });
        var agent = new ConversationAgent(mockModel.Object);

        // Ensure no conversation is set
        ConversationContext.Current = null;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.SendMessageAsync("Hello", CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_Message_Is_Null()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => agent.SendMessageAsync(null!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_Message_Is_Empty()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => agent.SendMessageAsync("", CancellationToken.None));
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_Message_Is_Whitespace()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => agent.SendMessageAsync("   ", CancellationToken.None));
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Add_User_Message_To_Conversation()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();
        var userMessage = "Hello, AI!";

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync(userMessage, CancellationToken.None);

            // Assert
            var messages = await conversation.GetMessagesAsync();
            Assert.Contains(messages, m => m.Role == ConversationRole.User && m.Content == userMessage);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Add_Assistant_Response_To_Conversation()
    {
        // Arrange
        var expectedResponse = "AI response";
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse(expectedResponse, new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            var messages = await conversation.GetMessagesAsync();
            Assert.Contains(messages, m => m.Role == ConversationRole.Assistant && m.Content == expectedResponse);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Return_Assistant_Response()
    {
        // Arrange
        var expectedResponse = "AI response";
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse(expectedResponse, new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            var response = await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Call_GenerateResponseAsync_Once()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Update_Conversation_Statistics()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(100, 50)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            Assert.Equal(100, conversation.Statistics.LastCallInputTokens);
            Assert.Equal(50, conversation.Statistics.LastCallOutputTokens);
            Assert.Equal(100, conversation.Statistics.TotalInputTokens);
            Assert.Equal(50, conversation.Statistics.TotalOutputTokens);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Accumulate_Statistics_Across_Multiple_Calls()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(100, 50)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Message 1", CancellationToken.None);
            await agent.SendMessageAsync("Message 2", CancellationToken.None);
            await agent.SendMessageAsync("Message 3", CancellationToken.None);

            // Assert
            Assert.Equal(100, conversation.Statistics.LastCallInputTokens);
            Assert.Equal(50, conversation.Statistics.LastCallOutputTokens);
            Assert.Equal(300, conversation.Statistics.TotalInputTokens);
            Assert.Equal(150, conversation.Statistics.TotalOutputTokens);

            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Maintain_Conversation_History()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Message 1", CancellationToken.None);
            await agent.SendMessageAsync("Message 2", CancellationToken.None);
            await agent.SendMessageAsync("Message 3", CancellationToken.None);

            // Assert
            var messages = await conversation.GetMessagesAsync();
            Assert.Equal(6, messages.Count); // 3 user messages + 3 assistant messages
            Assert.Equal(ConversationRole.User, messages[0].Role);
            Assert.Equal(ConversationRole.Assistant, messages[1].Role);
            Assert.Equal(ConversationRole.User, messages[2].Role);
            Assert.Equal(ConversationRole.Assistant, messages[3].Role);
            Assert.Equal(ConversationRole.User, messages[4].Role);
            Assert.Equal(ConversationRole.Assistant, messages[5].Role);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Include_Conversation_History_In_Prompt()
    {
        // Arrange
        string? capturedPrompt = null;
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt);

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("First message", CancellationToken.None);
            await agent.SendMessageAsync("Second message", CancellationToken.None);

            // Assert
            Assert.NotNull(capturedPrompt);
            Assert.Contains("First message", capturedPrompt);
            Assert.Contains("Second message", capturedPrompt);
            Assert.Contains("User:", capturedPrompt);
            Assert.Contains("Assistant:", capturedPrompt);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Work_With_System_Messages()
    {
        // Arrange
        string? capturedPrompt = null;
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt);

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Add a system message
            await conversation.AddMessageAsync(ConversationRole.System, "You are a helpful assistant", CancellationToken.None);

            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            Assert.NotNull(capturedPrompt);
            Assert.Contains("System:", capturedPrompt);
            Assert.Contains("You are a helpful assistant", capturedPrompt);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Work_With_Different_Conversations()
    {
        // Arrange
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation1 = new Conversation();
        var conversation2 = new Conversation();

        // Act & Assert - First conversation
        using (ConversationContext.StartConversation(conversation1))
        {
            await agent.SendMessageAsync("Conversation 1 message", CancellationToken.None);
            var messages1 = await conversation1.GetMessagesAsync();
            Assert.Equal(2, messages1.Count);
        }

        // Act & Assert - Second conversation
        using (ConversationContext.StartConversation(conversation2))
        {
            await agent.SendMessageAsync("Conversation 2 message", CancellationToken.None);
            var messages2 = await conversation2.GetMessagesAsync();
            Assert.Equal(2, messages2.Count);
        }

        // Verify conversations are independent
        var finalMessages1 = await conversation1.GetMessagesAsync();
        var finalMessages2 = await conversation2.GetMessagesAsync();
        Assert.Equal(2, finalMessages1.Count);
        Assert.Equal(2, finalMessages2.Count);
        Assert.Contains("Conversation 1 message", finalMessages1[0].Content);
        Assert.Contains("Conversation 2 message", finalMessages2[0].Content);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Pass_Cancellation_Token_To_LanguageModel()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", cts.Token);

            // Assert
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<string>(), cts.Token),
                Times.Once);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var mockModel = CreateMockLanguageModel();
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => agent.SendMessageAsync("Hello", cts.Token));
        }
    }
}
