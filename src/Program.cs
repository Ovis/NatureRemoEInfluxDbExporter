using NatureRemoEInfluxDbExporter.Options;
using ZLogger;

namespace NatureRemoEInfluxDbExporter;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging
            .ClearProviders()
            .AddZLoggerConsole(options =>
            {
                options.IncludeScopes = true;
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            })
            .AddZLoggerRollingFile(options =>
            {
                options.IncludeScopes = true;
                options.FilePathSelector = (_, sequenceNumber) =>
                    $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")}/{sequenceNumber:000}.log";

                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });

                options.RollingSizeKB = 1024;
            });

        var configuration = builder.Configuration;

        builder.Services.Configure<NatureRemoOption>(option =>
        {
            option.AccessToken = configuration["NatureRemoOption:AccessToken"] ?? string.Empty;
            option.Interval = int.Parse(configuration["NatureRemoOption:Interval"] ?? "60");
        });

        builder.Services.Configure<InfluxDbOption>(option =>
        {
            option.Url = configuration["InfluxDbOption:Url"] ?? string.Empty;
            option.Token = configuration["InfluxDbOption:Token"] ?? string.Empty;
            option.Bucket = configuration["InfluxDbOption:Bucket"] ?? string.Empty;
            option.Org = configuration["InfluxDbOption:Org"] ?? string.Empty;
        });

        builder.Services.AddSingleton<InfluxDbSender>();

        builder.Services.AddHttpClient<NatureRemoEClient>();
        builder.Services.AddSingleton<NatureRemoEClient>();

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
