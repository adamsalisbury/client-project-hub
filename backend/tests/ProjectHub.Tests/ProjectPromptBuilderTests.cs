using ProjectHub.Services.Workers;

namespace ProjectHub.Tests;

public sealed class ProjectPromptBuilderTests
{
    [Fact]
    public void Build_WithNoHistory_ReturnsTheNewMessageVerbatim()
    {
        var prompt = ProjectPromptBuilder.Build(Array.Empty<ConversationTurn>(), "Hello, world.");

        Assert.Equal("Hello, world.", prompt);
    }

    [Fact]
    public void Build_WithSingleTurn_IncludesUserAndAssistantInOrder()
    {
        var turns = new[]
        {
            new ConversationTurn("What is 2+2?", "4")
        };

        var prompt = ProjectPromptBuilder.Build(turns, "Now what is that doubled?");

        Assert.Contains("User: What is 2+2?", prompt);
        Assert.Contains("Assistant: 4", prompt);
        Assert.Contains("Now what is that doubled?", prompt);

        var userIdx = prompt.IndexOf("User: What is 2+2?", StringComparison.Ordinal);
        var assistantIdx = prompt.IndexOf("Assistant: 4", StringComparison.Ordinal);
        var newMessageIdx = prompt.IndexOf("Now what is that doubled?", StringComparison.Ordinal);

        Assert.True(userIdx < assistantIdx);
        Assert.True(assistantIdx < newMessageIdx);
    }

    [Fact]
    public void Build_WithMultipleTurns_PreservesChronologicalOrder()
    {
        var turns = new[]
        {
            new ConversationTurn("first message", "first reply"),
            new ConversationTurn("second message", "second reply"),
            new ConversationTurn("third message", "third reply")
        };

        var prompt = ProjectPromptBuilder.Build(turns, "fourth message");

        var firstIdx = prompt.IndexOf("first message", StringComparison.Ordinal);
        var secondIdx = prompt.IndexOf("second message", StringComparison.Ordinal);
        var thirdIdx = prompt.IndexOf("third message", StringComparison.Ordinal);
        var fourthIdx = prompt.IndexOf("fourth message", StringComparison.Ordinal);

        Assert.True(firstIdx < secondIdx);
        Assert.True(secondIdx < thirdIdx);
        Assert.True(thirdIdx < fourthIdx);
    }

    [Fact]
    public void Build_WithHistory_MarksHistoryBoundaries()
    {
        var turns = new[]
        {
            new ConversationTurn("hi", "hello")
        };

        var prompt = ProjectPromptBuilder.Build(turns, "next");

        Assert.Contains("conversation history", prompt);
        Assert.Contains("end of conversation history", prompt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Build_WithEmptyOrWhitespaceMessage_Throws(string newMessage)
    {
        Assert.Throws<ArgumentException>(() =>
            ProjectPromptBuilder.Build(Array.Empty<ConversationTurn>(), newMessage));
    }

    [Fact]
    public void Build_WithNullPriorTurns_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProjectPromptBuilder.Build(null!, "hi"));
    }

    [Fact]
    public void Build_PreservesMultiLineMessageContent()
    {
        var turns = new[]
        {
            new ConversationTurn("line one\nline two", "ack\nok")
        };

        var prompt = ProjectPromptBuilder.Build(turns, "next");

        Assert.Contains("line one\nline two", prompt);
        Assert.Contains("ack\nok", prompt);
    }
}
