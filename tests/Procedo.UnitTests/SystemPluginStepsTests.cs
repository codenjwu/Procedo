using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.UnitTests;

public sealed class SystemPluginStepsTests
{
    [Fact]
    public void AddSystemPlugin_Registers_Default_System_Steps()
    {
        var registry = new PluginRegistry();
        registry.AddSystemPlugin();

        var expected = new[]
        {
            "system.echo",
            "system.guid",
            "system.now",
            "system.concat",
            "system.sleep",
            "system.wait_signal",
            "system.wait_until",
            "system.wait_file",
            "system.http",
            "system.file_write_text",
            "system.file_read_text",
            "system.file_copy",
            "system.file_move",
            "system.file_delete",
            "system.base64_encode",
            "system.base64_decode",
            "system.hash",
            "system.zip_create",
            "system.zip_extract",
            "system.dir_create",
            "system.dir_list",
            "system.dir_delete",
            "system.json_get",
            "system.json_set",
            "system.json_merge",
            "system.process_run",
            "system.csv_read",
            "system.csv_write",
            "system.xml_get",
            "system.xml_set"
        };

        foreach (var stepType in expected)
        {
            var resolved = registry.TryResolve(stepType, out var stepFactory);
            Assert.True(resolved, $"Expected '{stepType}' to be registered.");
            Assert.NotNull(stepFactory);
        }
    }

    [Fact]
    public async Task GuidStep_Returns_Guid_Output()
    {
        var step = new GuidStep();
        var context = CreateContext(new Dictionary<string, object>
        {
            ["format"] = "N",
            ["uppercase"] = true
        });

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Outputs.TryGetValue("guid", out var guidValue));
        var guidText = Assert.IsType<string>(guidValue);
        Assert.Equal(32, guidText.Length);
        Assert.Equal(guidText, guidText.ToUpperInvariant());
    }

    [Fact]
    public async Task NowStep_Returns_Utc_And_Unix_Outputs()
    {
        var step = new NowStep();
        var context = CreateContext();

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Outputs.ContainsKey("utc"));
        Assert.True(result.Outputs.ContainsKey("unix_ms"));
        Assert.True(result.Outputs.ContainsKey("unix_s"));
    }

    [Fact]
    public async Task ConcatStep_Joins_Values_With_Separator()
    {
        var step = new ConcatStep();
        var context = CreateContext(new Dictionary<string, object>
        {
            ["separator"] = "-",
            ["values"] = new object[] { "alpha", 42, "omega" }
        });

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("alpha-42-omega", result.Outputs["value"]);
    }

    [Fact]
    public async Task SleepStep_Returns_Slept_Milliseconds()
    {
        var step = new SleepStep();
        var context = CreateContext(new Dictionary<string, object>
        {
            ["milliseconds"] = 1
        });

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(1, result.Outputs["slept_ms"]);
    }

    [Fact]
    public async Task File_Steps_Can_Write_Copy_Read_Move_Delete()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-steps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "source.txt");
            var copyPath = Path.Combine(root, "copy", "copy.txt");
            var movedPath = Path.Combine(root, "moved.txt");

            var write = new FileWriteTextStep();
            var copy = new FileCopyStep();
            var read = new FileReadTextStep();
            var move = new FileMoveStep();
            var delete = new FileDeleteStep();

            var writeResult = await write.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = sourcePath,
                ["content"] = "hello procedo",
                ["create_directory"] = true
            }));

            Assert.True(writeResult.Success);

            var copyResult = await copy.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["source"] = sourcePath,
                ["target"] = copyPath,
                ["overwrite"] = true
            }));

            Assert.True(copyResult.Success);
            Assert.True(File.Exists(copyPath));

            var readResult = await read.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = copyPath
            }));

            Assert.True(readResult.Success);
            Assert.Equal("hello procedo", readResult.Outputs["content"]);

            var moveResult = await move.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["source"] = copyPath,
                ["target"] = movedPath,
                ["overwrite"] = true
            }));

            Assert.True(moveResult.Success);
            Assert.False(File.Exists(copyPath));
            Assert.True(File.Exists(movedPath));

            var deleteResult = await delete.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = movedPath
            }));

            Assert.True(deleteResult.Success);
            Assert.False(File.Exists(movedPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                DeleteDirectoryWithRetry(root);
            }
        }
    }

    [Fact]
    public async Task HttpStep_Returns_Response_Body_And_Status()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
            };
            response.Headers.Add("x-demo", "yes");
            return response;
        });

        var client = new HttpClient(handler);
        var step = new HttpStep(client);

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["url"] = "https://unit.test/demo",
            ["method"] = "GET"
        }));

        Assert.True(result.Success);
        Assert.Equal(200, result.Outputs["status_code"]);
        Assert.Equal(true, result.Outputs["is_success"]);
        Assert.Contains("ok", result.Outputs["body"].ToString());
    }

    [Fact]
    public async Task Base64_Steps_Roundtrip_Text()
    {
        var encode = new Base64EncodeStep();
        var decode = new Base64DecodeStep();

        var encoded = await encode.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["text"] = "Procedo"
        }));

        Assert.True(encoded.Success);

        var decoded = await decode.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["base64"] = encoded.Outputs["value"]
        }));

        Assert.True(decoded.Success);
        Assert.Equal("Procedo", decoded.Outputs["value"]);
    }

    [Fact]
    public async Task HashStep_Computes_Sha256_For_Text()
    {
        var step = new HashStep();
        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["algorithm"] = "SHA256",
            ["text"] = "Procedo"
        }));

        Assert.True(result.Success);
        Assert.Equal("SHA256", result.Outputs["algorithm"]);
        Assert.Equal("b7d0465f916fdf0a077949f2279a4d557aaf1bc2bb35ab64b0eb3ed15e4c8cf4", result.Outputs["value"]);
    }

    [Fact]
    public async Task Zip_Steps_Can_Create_And_Extract_Archive()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-zip", Guid.NewGuid().ToString("N"));
        var inputDirectory = Path.Combine(root, "input");
        var extractDirectory = Path.Combine(root, "extract");
        var zipPath = Path.Combine(root, "archive.zip");

        Directory.CreateDirectory(inputDirectory);
        File.WriteAllText(Path.Combine(inputDirectory, "a.txt"), "alpha");
        File.WriteAllText(Path.Combine(inputDirectory, "b.txt"), "beta");

        try
        {
            var zipCreate = new ZipCreateStep();
            var zipExtract = new ZipExtractStep();

            var createResult = await zipCreate.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["source_directory"] = inputDirectory,
                ["zip_path"] = zipPath,
                ["overwrite"] = true
            }));

            Assert.True(createResult.Success);
            Assert.True(File.Exists(zipPath));

            var extractResult = await zipExtract.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["zip_path"] = zipPath,
                ["destination_directory"] = extractDirectory,
                ["overwrite"] = true
            }));

            Assert.True(extractResult.Success);
            Assert.Equal("alpha", File.ReadAllText(Path.Combine(extractDirectory, "a.txt")));
            Assert.Equal("beta", File.ReadAllText(Path.Combine(extractDirectory, "b.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                DeleteDirectoryWithRetry(root);
            }
        }
    }

    [Fact]
    public async Task Directory_Steps_Can_Create_List_And_Delete()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-dir", Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "nested");

        try
        {
            var create = new DirectoryCreateStep();
            var list = new DirectoryListStep();
            var delete = new DirectoryDeleteStep();

            var createResult = await create.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = nested
            }));

            Assert.True(createResult.Success);
            File.WriteAllText(Path.Combine(nested, "a.txt"), "alpha");

            var listResult = await list.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = root,
                ["recursive"] = true
            }));

            Assert.True(listResult.Success);
            Assert.True(Convert.ToInt32(listResult.Outputs["count"]) >= 2);

            var deleteResult = await delete.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = root,
                ["recursive"] = true
            }));

            Assert.True(deleteResult.Success);
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                DeleteDirectoryWithRetry(root);
            }
        }
    }

    [Fact]
    public async Task Json_Steps_Can_Merge_Set_And_Get()
    {
        var merge = new JsonMergeStep();
        var set = new JsonSetStep();
        var get = new JsonGetStep();

        var mergeResult = await merge.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["left"] = "{\"app\":{\"name\":\"procedo\",\"version\":1}}",
            ["right"] = "{\"app\":{\"owner\":\"codenj.wu\"},\"features\":[\"engine\"]}"
        }));

        Assert.True(mergeResult.Success);

        var setResult = await set.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["json"] = mergeResult.Outputs["json"],
            ["path"] = "app.version",
            ["value"] = 2
        }));

        Assert.True(setResult.Success);

        var getResult = await get.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["json"] = setResult.Outputs["json"],
            ["path"] = "app.version"
        }));

        Assert.True(getResult.Success);
        Assert.Equal(2L, getResult.Outputs["value"]);
    }

    [Fact]
    public async Task ProcessRunStep_Blocks_Shells_By_Default()
    {
        var step = new ProcessRunStep();
        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["file_name"] = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "bash"
        }));

        Assert.False(result.Success);
        Assert.Contains("blocked by default", result.Error);
    }

    [Fact]
    public async Task ProcessRunStep_Can_Run_Dotnet_Version()
    {
        var step = new ProcessRunStep();
        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["file_name"] = "dotnet",
            ["arguments"] = new object[] { "--version" },
            ["timeout_ms"] = 10000
        }));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, result.Outputs["exit_code"]);
        Assert.False(string.IsNullOrWhiteSpace(result.Outputs["stdout"].ToString()));
    }

    [Fact]
    public async Task Csv_Steps_Can_Write_And_Read_Roundtrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-csv", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "data.csv");
        Directory.CreateDirectory(root);

        try
        {
            var write = new CsvWriteStep();
            var read = new CsvReadStep();

            var writeResult = await write.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = path,
                ["rows"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "alpha",
                        ["value"] = "1"
                    },
                    new Dictionary<string, object>
                    {
                        ["name"] = "beta",
                        ["value"] = "2"
                    }
                }
            }));

            Assert.True(writeResult.Success);
            Assert.True(File.Exists(path));

            var readResult = await read.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = path,
                ["has_header"] = true
            }));

            Assert.True(readResult.Success);
            Assert.Equal(2, readResult.Outputs["count"]);

            var rows = Assert.IsType<List<Dictionary<string, object>>>(readResult.Outputs["rows"]);
            Assert.Equal("alpha", rows[0]["name"]);
            Assert.Equal("2", rows[1]["value"]);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                DeleteDirectoryWithRetry(root);
            }
        }
    }

    [Fact]
    public async Task Xml_Steps_Can_Set_And_Get_Element_And_Attribute_Values()
    {
        var set = new XmlSetStep();
        var get = new XmlGetStep();

        const string input = "<app><settings mode=\"dev\"><name>Procedo</name></settings></app>";

        var setElementResult = await set.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["xml"] = input,
            ["path"] = "settings/name",
            ["value"] = "Procedo Runtime"
        }));

        Assert.True(setElementResult.Success);

        var setAttributeResult = await set.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["xml"] = setElementResult.Outputs["xml"],
            ["path"] = "settings/@mode",
            ["value"] = "prod"
        }));

        Assert.True(setAttributeResult.Success);

        var getElementResult = await get.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["xml"] = setAttributeResult.Outputs["xml"],
            ["path"] = "settings/name"
        }));

        var getAttributeResult = await get.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["xml"] = setAttributeResult.Outputs["xml"],
            ["path"] = "settings/@mode"
        }));

        Assert.True(getElementResult.Success);
        Assert.True(getAttributeResult.Success);
        Assert.Equal("Procedo Runtime", getElementResult.Outputs["value"]);
        Assert.Equal("prod", getAttributeResult.Outputs["value"]);
    }
    [Fact]
    public async Task FileWriteTextStep_Should_Block_Path_Outside_Allowed_Root()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-policy", Guid.NewGuid().ToString("N"));
        var allowedRoot = Path.Combine(root, "allowed");
        var blockedPath = Path.Combine(root, "blocked", "test.txt");
        Directory.CreateDirectory(allowedRoot);

        try
        {
            var step = new FileWriteTextStep(new SystemPluginSecurityOptions { AllowedPathRoots = { allowedRoot } });

            var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = blockedPath,
                ["content"] = "hello"
            }));

            Assert.False(result.Success);
            Assert.Contains("outside the allowed", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                DeleteDirectoryWithRetry(root);
            }
        }
    }

    [Fact]
    public async Task HttpStep_Should_Block_Host_Not_In_AllowList()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var step = new HttpStep(new HttpClient(handler), new SystemPluginSecurityOptions
        {
            AllowedHttpHosts = { "allowed.test" }
        });

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["url"] = "https://blocked.test/api"
        }));

        Assert.False(result.Success);
        Assert.Contains("not allowed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessRunStep_Should_Block_Executable_Not_In_AllowList()
    {
        var step = new ProcessRunStep(new SystemPluginSecurityOptions
        {
            AllowedExecutables = { "git" }
        });

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["file_name"] = "dotnet",
            ["arguments"] = new object[] { "--version" }
        }));

        Assert.False(result.Success);
        Assert.Contains("not allowed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WaitUntilStep_Should_Return_Waiting_When_Time_Has_Not_Arrived()
    {
        var step = new WaitUntilStep();
        var untilUtc = DateTimeOffset.UtcNow.AddMinutes(5).ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["until_utc"] = untilUtc
        }));

        Assert.True(result.Waiting);
        Assert.Equal("time", result.Wait?.Type);
        Assert.Equal(untilUtc, result.Wait?.Key);
    }

    [Fact]
    public async Task WaitUntilStep_Should_Complete_When_Time_Has_Arrived()
    {
        var step = new WaitUntilStep();
        var untilUtc = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["until_utc"] = untilUtc
        }));

        Assert.True(result.Success);
        Assert.False(result.Waiting);
        Assert.Equal(untilUtc, result.Outputs["until_utc"]?.ToString());
    }
    [Fact]
    public async Task WaitFileStep_Should_Return_Waiting_When_File_Does_Not_Exist()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-steps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var path = Path.Combine(root, "arrival.txt");
            var step = new WaitFileStep(new SystemPluginSecurityOptions
            {
                AllowedPathRoots = { root }
            });

            var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = path
            }));

            Assert.True(result.Waiting);
            Assert.Equal("file", result.Wait?.Type);
            Assert.Equal(Path.GetFullPath(path), result.Wait?.Key);
        }
        finally
        {
            DeleteDirectoryWithRetry(root);
        }
    }

    [Fact]
    public async Task WaitFileStep_Should_Complete_When_File_Exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "procedo-system-steps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var path = Path.Combine(root, "arrival.txt");
            await File.WriteAllTextAsync(path, "ready");
            var step = new WaitFileStep(new SystemPluginSecurityOptions
            {
                AllowedPathRoots = { root }
            });

            var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
            {
                ["path"] = path
            }));

            Assert.True(result.Success);
            Assert.False(result.Waiting);
            Assert.True((bool)result.Outputs["exists"]);
            Assert.Equal(Path.GetFullPath(path), result.Outputs["path"]);
        }
        finally
        {
            DeleteDirectoryWithRetry(root);
        }
    }
    [Fact]
    public async Task WaitSignalStep_Should_Return_Waiting_When_No_Resume_Signal_Is_Present()
    {
        var step = new WaitSignalStep();

        var result = await step.ExecuteAsync(CreateContext(new Dictionary<string, object>
        {
            ["signal_type"] = "continue",
            ["reason"] = "Waiting for operator signal"
        }));

        Assert.True(result.Waiting);
        Assert.False(result.Success);
        Assert.NotNull(result.Wait);
        Assert.Equal("signal", result.Wait!.Type);
        Assert.Equal("Waiting for operator signal", result.Wait.Reason);
        Assert.Equal("continue", result.Wait.Metadata["expected_signal_type"]?.ToString());
    }

    [Fact]
    public async Task WaitSignalStep_Should_Complete_When_Matching_Resume_Signal_Is_Present()
    {
        var step = new WaitSignalStep();
        var context = CreateContext(new Dictionary<string, object>
        {
            ["signal_type"] = "continue"
        });
        context.Resume = new Procedo.Core.Runtime.ResumeRequest
        {
            SignalType = "continue",
            Payload = new Dictionary<string, object>
            {
                ["approved_by"] = "operator"
            }
        };

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.False(result.Waiting);
        Assert.Equal("continue", result.Outputs["signal_type"]?.ToString());
        var payload = Assert.IsType<Dictionary<string, object>>(result.Outputs["payload"]);
        Assert.Equal("operator", payload["approved_by"]?.ToString());
    }
    private static void DeleteDirectoryWithRetry(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(50 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(50 * attempt);
            }
        }

        Directory.Delete(path, true);
    }

    private static StepContext CreateContext(IDictionary<string, object>? inputs = null)
    {
        return new StepContext
        {
            RunId = "run-1",
            StepId = "step-1",
            Inputs = inputs ?? new Dictionary<string, object>(),
            Variables = new Dictionary<string, object>(),
            Logger = new ConsoleLogger()
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}











