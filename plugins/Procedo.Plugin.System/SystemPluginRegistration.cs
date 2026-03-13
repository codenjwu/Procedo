using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public static class SystemPluginRegistration
{
    public static IPluginRegistry AddSystemPlugin(this IPluginRegistry registry, SystemPluginSecurityOptions? securityOptions = null)
    {
        securityOptions ??= new SystemPluginSecurityOptions();

        registry.Register("system.echo", () => new EchoStep());
        registry.Register("system.guid", () => new GuidStep());
        registry.Register("system.now", () => new NowStep());
        registry.Register("system.concat", () => new ConcatStep());
        registry.Register("system.sleep", () => new SleepStep());
        registry.Register("system.wait_signal", () => new WaitSignalStep());
        registry.Register("system.wait_until", () => new WaitUntilStep());
        registry.Register("system.wait_file", () => new WaitFileStep(securityOptions));

        registry.Register("system.http", () => new HttpStep(securityOptions: securityOptions));
        registry.Register("system.file_write_text", () => new FileWriteTextStep(securityOptions));
        registry.Register("system.file_read_text", () => new FileReadTextStep(securityOptions));
        registry.Register("system.file_copy", () => new FileCopyStep(securityOptions));
        registry.Register("system.file_move", () => new FileMoveStep(securityOptions));
        registry.Register("system.file_delete", () => new FileDeleteStep(securityOptions));
        registry.Register("system.base64_encode", () => new Base64EncodeStep());
        registry.Register("system.base64_decode", () => new Base64DecodeStep());
        registry.Register("system.hash", () => new HashStep(securityOptions));
        registry.Register("system.zip_create", () => new ZipCreateStep(securityOptions));
        registry.Register("system.zip_extract", () => new ZipExtractStep(securityOptions));
        registry.Register("system.dir_create", () => new DirectoryCreateStep(securityOptions));
        registry.Register("system.dir_list", () => new DirectoryListStep(securityOptions));
        registry.Register("system.dir_delete", () => new DirectoryDeleteStep(securityOptions));
        registry.Register("system.json_get", () => new JsonGetStep());
        registry.Register("system.json_set", () => new JsonSetStep());
        registry.Register("system.json_merge", () => new JsonMergeStep());
        registry.Register("system.process_run", () => new ProcessRunStep(securityOptions));
        registry.Register("system.csv_read", () => new CsvReadStep(securityOptions));
        registry.Register("system.csv_write", () => new CsvWriteStep(securityOptions));
        registry.Register("system.xml_get", () => new XmlGetStep());
        registry.Register("system.xml_set", () => new XmlSetStep());

        return registry;
    }
}



