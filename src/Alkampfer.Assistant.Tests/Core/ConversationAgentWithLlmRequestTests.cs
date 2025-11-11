using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Llm;
using Moq;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

/// <summary>
/// Tests for ConversationAgent interaction with ILanguageModel, particularly around
/// conversation support and LlmRequest usage.
/// </summary>
public class ConversationAgentWithLlmRequestTests
{
    [Fact]
    public void Constructor_Should_Check_Model_Capabilities()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);

        // Act
        var agent = new ConversationAgent(mockModel.Object);

        // Assert
        mockModel.Verify(m => m.GetCapability(), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Use_LlmRequest_When_Model_Supports_Conversation()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, "response-id-1"));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert - Should call the LlmRequest overload, not the string overload
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Use_String_Prompt_When_Model_Does_Not_Support_Conversation()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = false };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20)));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert - Should call the string overload, not the LlmRequest overload
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
            mockModel.Verify(
                m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Store_ResponseId_From_Conversation_Capable_Model()
    {
        // Arrange
        var expectedResponseId = "response-id-123";
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, expectedResponseId));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponseId, agent.LastResponseId);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Pass_Null_PreviousConversationId_On_First_Request()
    {
        // Arrange
        LlmRequest? capturedRequest = null;
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, "response-id-1"))
            .Callback<LlmRequest, CancellationToken>((req, _) => capturedRequest = req);

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Null(capturedRequest.PreviousConversationId);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Pass_Previous_ResponseId_On_Subsequent_Requests()
    {
        // Arrange
        var firstResponseId = "response-id-1";
        var secondResponseId = "response-id-2";
        LlmRequest? firstRequest = null;
        LlmRequest? secondRequest = null;
        
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        
        var callCount = 0;
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmRequest req, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    firstRequest = req;
                    return new LanguageModelResponse("First response", new LanguageModelStatistics(10, 20), null, firstResponseId);
                }
                else
                {
                    secondRequest = req;
                    return new LanguageModelResponse("Second response", new LanguageModelStatistics(10, 20), null, secondResponseId);
                }
            });

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("First message", CancellationToken.None);
            await agent.SendMessageAsync("Second message", CancellationToken.None);

            // Assert
            Assert.NotNull(firstRequest);
            Assert.Null(firstRequest.PreviousConversationId);
            
            Assert.NotNull(secondRequest);
            Assert.Equal(firstResponseId, secondRequest.PreviousConversationId);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Chain_ResponseIds_Across_Multiple_Requests()
    {
        // Arrange
        var responseIds = new[] { "resp-1", "resp-2", "resp-3", "resp-4" };
        var requests = new List<LlmRequest>();
        
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        
        var callCount = 0;
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmRequest req, CancellationToken _) =>
            {
                requests.Add(req);
                var responseId = responseIds[callCount];
                callCount++;
                return new LanguageModelResponse($"Response {callCount}", new LanguageModelStatistics(10, 20), null, responseId);
            });

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Message 1", CancellationToken.None);
            await agent.SendMessageAsync("Message 2", CancellationToken.None);
            await agent.SendMessageAsync("Message 3", CancellationToken.None);
            await agent.SendMessageAsync("Message 4", CancellationToken.None);

            // Assert
            Assert.Equal(4, requests.Count);
            Assert.Null(requests[0].PreviousConversationId);
            Assert.Equal("resp-1", requests[1].PreviousConversationId);
            Assert.Equal("resp-2", requests[2].PreviousConversationId);
            Assert.Equal("resp-3", requests[3].PreviousConversationId);
            Assert.Equal("resp-4", agent.LastResponseId);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Only_Send_Current_User_Message_When_Conversation_Supported()
    {
        // Arrange
        LlmRequest? capturedRequest = null;
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, "response-id-1"))
            .Callback<LlmRequest, CancellationToken>((req, _) => capturedRequest = req);

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Add some history
            await conversation.AddMessageAsync(ConversationRole.System, "You are a helpful assistant", CancellationToken.None);
            await conversation.AddMessageAsync(ConversationRole.User, "Previous message", CancellationToken.None);
            await conversation.AddMessageAsync(ConversationRole.Assistant, "Previous response", CancellationToken.None);

            // Act
            await agent.SendMessageAsync("New message", CancellationToken.None);

            // Assert - Should only contain the new user message, not the history
            Assert.NotNull(capturedRequest);
            Assert.Single(capturedRequest.Messages);
            Assert.Equal(ConversationRole.User, capturedRequest.Messages[0].Role);
            Assert.Equal("New message", capturedRequest.Messages[0].Content);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Send_Full_History_When_Conversation_Not_Supported()
    {
        // Arrange
        string? capturedPrompt = null;
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = false };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20)))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt);

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Add some history
            await conversation.AddMessageAsync(ConversationRole.System, "You are a helpful assistant", CancellationToken.None);
            await conversation.AddMessageAsync(ConversationRole.User, "Previous message", CancellationToken.None);
            await conversation.AddMessageAsync(ConversationRole.Assistant, "Previous response", CancellationToken.None);

            // Act
            await agent.SendMessageAsync("New message", CancellationToken.None);

            // Assert - Should contain all history in the prompt
            Assert.NotNull(capturedPrompt);
            Assert.Contains("You are a helpful assistant", capturedPrompt);
            Assert.Contains("Previous message", capturedPrompt);
            Assert.Contains("Previous response", capturedPrompt);
            Assert.Contains("New message", capturedPrompt);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Update_Statistics_Regardless_Of_Conversation_Support()
    {
        // Arrange & Act - With conversation support
        var mockModelWithSupport = new Mock<ILanguageModel>();
        mockModelWithSupport.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = true });
        mockModelWithSupport
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(100, 50), null, "resp-1"));

        var agentWithSupport = new ConversationAgent(mockModelWithSupport.Object);
        var conversation1 = new Conversation();

        using (ConversationContext.StartConversation(conversation1))
        {
            await agentWithSupport.SendMessageAsync("Hello", CancellationToken.None);
            Assert.Equal(100, conversation1.Statistics.TotalInputTokens);
            Assert.Equal(50, conversation1.Statistics.TotalOutputTokens);
        }

        // Arrange & Act - Without conversation support
        var mockModelWithoutSupport = new Mock<ILanguageModel>();
        mockModelWithoutSupport.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = false });
        mockModelWithoutSupport
            .Setup(m => m.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("Response", new LanguageModelStatistics(100, 50)));

        var agentWithoutSupport = new ConversationAgent(mockModelWithoutSupport.Object);
        var conversation2 = new Conversation();

        using (ConversationContext.StartConversation(conversation2))
        {
            await agentWithoutSupport.SendMessageAsync("Hello", CancellationToken.None);
            Assert.Equal(100, conversation2.Statistics.TotalInputTokens);
            Assert.Equal(50, conversation2.Statistics.TotalOutputTokens);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Still_Add_Messages_To_Conversation_History_When_Conversation_Supported()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        var capabilities = new LlmCapabilities { SupportConversation = true };
        mockModel.Setup(m => m.GetCapability()).Returns(capabilities);
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, "response-id-1"));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert - Even with conversation support, we should maintain local history
            var messages = await conversation.GetMessagesAsync();
            Assert.Equal(2, messages.Count);
            Assert.Equal(ConversationRole.User, messages[0].Role);
            Assert.Equal("Hello", messages[0].Content);
            Assert.Equal(ConversationRole.Assistant, messages[1].Role);
            Assert.Equal("AI response", messages[1].Content);
        }
    }

    [Fact]
    public async Task LastResponseId_Should_Be_Null_Initially()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        mockModel.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = true });

        // Act
        var agent = new ConversationAgent(mockModel.Object);

        // Assert
        Assert.Null(agent.LastResponseId);
    }

    [Fact]
    public async Task LastResponseId_Should_Remain_Null_When_Model_Does_Not_Support_Conversation()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        mockModel.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = false });
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
            Assert.Null(agent.LastResponseId);
        }
    }

    [Fact]
    public async Task LastResponseId_Should_Update_With_Each_Response()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        mockModel.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = true });
        
        var callCount = 0;
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmRequest req, CancellationToken _) =>
            {
                callCount++;
                return new LanguageModelResponse($"Response {callCount}", new LanguageModelStatistics(10, 20), null, $"resp-{callCount}");
            });

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act & Assert
            Assert.Null(agent.LastResponseId);
            
            await agent.SendMessageAsync("Message 1", CancellationToken.None);
            Assert.Equal("resp-1", agent.LastResponseId);
            
            await agent.SendMessageAsync("Message 2", CancellationToken.None);
            Assert.Equal("resp-2", agent.LastResponseId);
            
            await agent.SendMessageAsync("Message 3", CancellationToken.None);
            Assert.Equal("resp-3", agent.LastResponseId);
        }
    }

    [Fact]
    public async Task SendMessageAsync_Should_Handle_Null_ResponseId_From_Conversation_Capable_Model()
    {
        // Arrange
        var mockModel = new Mock<ILanguageModel>();
        mockModel.Setup(m => m.GetCapability()).Returns(new LlmCapabilities { SupportConversation = true });
        mockModel
            .Setup(m => m.GenerateResponseAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageModelResponse("AI response", new LanguageModelStatistics(10, 20), null, null));

        var agent = new ConversationAgent(mockModel.Object);
        var conversation = new Conversation();

        using (ConversationContext.StartConversation(conversation))
        {
            // Act
            await agent.SendMessageAsync("Hello", CancellationToken.None);

            // Assert - Should handle null ResponseId gracefully
            Assert.Null(agent.LastResponseId);
        }
    }
}
