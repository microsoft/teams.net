// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace A2ABot;

// Discriminated union carried in A2A DataPart so both bots share one endpoint.
record AskMessage(string Kind, string Qid, string Question, string From, string ReplyBaseUrl)
{
    public AskMessage(string qid, string question, string from, string replyBaseUrl)
        : this("ask", qid, question, from, replyBaseUrl) { }
}

record ReplyMessage(string Kind, string Qid, string Answer, string From)
{
    public ReplyMessage(string qid, string answer, string from)
        : this("reply", qid, answer, from) { }
}
