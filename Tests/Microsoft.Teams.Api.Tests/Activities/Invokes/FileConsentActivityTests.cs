

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities;

public class FileConsentActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string fileConsentActivityJson = File.ReadAllText(
            @"../../../Json/Activity/FileConsentActivity.json"
        );

    private static FileConsentActivity SetupFileConsentActivity()
    {
        return new FileConsentActivity()
        {
            Id = "fileConsentId123",
            Value = new FileConsentCardResponse()
            {

                Action = Action.Accept,
                Context = new FileConsentCard()
                {
                    Description = "File description",
                    SizeInBytes = 123456,
                    AcceptContext = "Accepted context",
                    DeclineContext = "Declined context"
                },
                UploadInfo = new FileUploadInfo()
                {
                    Name = "example.txt",
                    FileType = "text/plain",
                    ContentUrl = "https://example.com/content.txt",
                    UniqueId = "unique-id-12345",
                    UploadUrl = "https://example.com/upload.txt"
                }
            }
        };
    }

    [Fact]
    public void FileConsentActivity_JsonSerialize()
    {
        var activity = SetupFileConsentActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.FileConsent/invoke";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(fileConsentActivityJson, json);
    }

    [Fact]
    public void FileConsentActivity_JsonSerialize_Derived()
    {
        InvokeActivity activity = SetupFileConsentActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(fileConsentActivityJson, json);
    }

    [Fact]
    public void FileConsentActivity_JsonSerialize_Interface_Derived()
    {
        IActivity activity = SetupFileConsentActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(fileConsentActivityJson, json);
    }


    [Fact]
    public void FileConsentActivity_JsonDeserialize()
    {
        var activity = JsonSerializer.Deserialize<FileConsentActivity>(fileConsentActivityJson);
        var expected = SetupFileConsentActivity();
        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void FileConsentActivity_JsonDeserialize_Derived()
    {
        var activity = JsonSerializer.Deserialize<InvokeActivity>(fileConsentActivityJson);
        var expected = SetupFileConsentActivity();

        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void FileConsentActivity_JsonDeserialize_Interface_Derived()
    {
        var activity = JsonSerializer.Deserialize<IActivity>(fileConsentActivityJson);
        var expected = SetupFileConsentActivity();

        Assert.Equal(expected.ToString(), activity?.ToString());
    }

}