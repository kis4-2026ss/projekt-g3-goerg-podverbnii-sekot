namespace GraderTool.Ai.Prompting;

public static class ReviewResponseSchema
{
    public static object Create()
    {
        return new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["repo_name"] = new { type = "string" },
                ["summary"] = new { type = "string" },
                ["files"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["file"] = new { type = "string" },
                            ["summary"] = new { type = "string" },
                            ["findings"] = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new Dictionary<string, object>
                                    {
                                        ["file"] = new { type = "string" },
                                        ["line"] = new { type = "integer" },
                                        ["comment"] = new { type = "string" }
                                    },
                                    required = new[] { "file", "line", "comment" }
                                }
                            }
                        },
                        required = new[] { "file", "summary", "findings" }
                    }
                }
            },
            required = new[] { "repo_name", "summary", "files" }
        };
    }
}
