record ChatRequest(
    string ConversationId,
    string Message);

record SearchRequest(
    string Title
);

record ChatEventInfo(
    string ConversationId,
    ChatEventType EventType,
    Track? Track
);

enum ChatEventType
{
    Play = 1,
    StopOrPause = 2,
    Test = 3
}
