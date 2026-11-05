using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Serialization;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;

var path = Path.GetTempFileName();
var serializer = new XmlSerializer(typeof(MailConfiguration));
var results = new List<object>();
try
{
    foreach (var templateCount in new[] { 1, 10, 100, 1000 })
    {
        const string markup = "<p>Hello {User.Value}</p>";
        var configuration = new MailConfiguration
        {
            MailMessages = Enumerable.Range(0, templateCount).Select(index => new MailMessageConfiguration
            {
                MessageType = $"Template{index}",
                MessageSubject = $"Template {index}: {{User.Value}}",
                MessageBody = markup + new string('x', 1024 - markup.Length),
                IsHtml = true
            }).ToList()
        };
        using (var stream = File.Create(path)) serializer.Serialize(stream, configuration);
        using (var stream = File.OpenRead(path)) configuration = (MailConfiguration)serializer.Deserialize(stream);

        var fileServices = new ServiceCollection();
        fileServices.AddTemplateEmail(path, _ => { });
        var sharedServices = new ServiceCollection();
        sharedServices.AddTemplateEmail(configuration, _ => { });
        using var fileProvider = fileServices.BuildServiceProvider();
        using var sharedProvider = sharedServices.BuildServiceProvider();

        // Verify equivalent output and builder isolation outside the timed operations.
        foreach (var provider in new[] { fileProvider, sharedProvider })
        {
            var helper = provider.GetRequiredService<IEmailHelper>();
            var alice = helper.ForType("Template0").Map("User", "Alice");
            var bob = helper.ForType("Template0").Map("User", "Bob");
            var bobMessage = bob.BuildMessage(["to@example.com"]);
            var aliceMessage = alice.BuildMessage(["to@example.com"]);
            if (aliceMessage.Subject != "Template 0: Alice" || bobMessage.Subject != "Template 0: Bob"
                || !aliceMessage.Body.StartsWith("<p>Hello Alice</p>", StringComparison.Ordinal)
                || !bobMessage.Body.StartsWith("<p>Hello Bob</p>", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Benchmark configuration or builder isolation check failed.");
            }
        }

        foreach (var buildMessage in new[] { false, true })
        {
            foreach (var entry in new[] { (Mode: "FilePath", Provider: fileProvider), (Mode: "SharedConfiguration", Provider: sharedProvider) })
            {
                Action operation = () =>
                {
                    var helper = entry.Provider.GetRequiredService<IEmailHelper>();
                    if (buildMessage)
                    {
                        GC.KeepAlive(helper.ForType("Template0").Map("User", "Alice").BuildMessage(["to@example.com"]));
                    }
                    else
                    {
                        GC.KeepAlive(helper);
                    }
                };
                for (var index = 0; index < 100; index++) operation();
                var calibration = Measure(operation, 100);
                var iterations = (int)Math.Clamp(150_000 / calibration.Microseconds, 50, 200_000);
                var samples = Enumerable.Range(0, 7).Select(_ => Measure(operation, iterations)).ToArray();
                var times = samples.Select(sample => sample.Microseconds).Order().ToArray();
                var bytes = samples.Select(sample => sample.BytesPerOperation).Order().ToArray();
                if (times[3] <= 0 || bytes[3] < 0) throw new InvalidOperationException("Invalid benchmark measurement.");

                results.Add(new
                {
                    TemplateCount = templateCount,
                    XmlBytes = new FileInfo(path).Length,
                    Scenario = buildMessage ? "ResolveAndBuild" : "ResolveOnly",
                    entry.Mode,
                    IterationsPerSample = iterations,
                    MedianMicroseconds = times[3],
                    MinMicroseconds = times[0],
                    MaxMicroseconds = times[^1],
                    MedianBytesPerOperation = bytes[3]
                });
                Console.Error.WriteLine($"{templateCount} templates, {(buildMessage ? "resolve + build" : "resolve")}, {entry.Mode}: {times[3]:F2} us/op, {bytes[3]:F0} B/op");
            }
        }
    }
}
finally
{
    File.Delete(path);
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    MeasuredAtUtc = DateTimeOffset.UtcNow,
    Runtime = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    ProcessorCount = Environment.ProcessorCount,
    ServerGC = GCSettings.IsServerGC,
    SamplesPerCase = 7,
    Results = results
}, new JsonSerializerOptions { WriteIndented = true }));

static (double Microseconds, double BytesPerOperation) Measure(Action operation, int iterations)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    for (var index = 0; index < iterations; index++) operation();
    var elapsed = Stopwatch.GetElapsedTime(started);
    return (elapsed.TotalMicroseconds / iterations, (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)iterations);
}
