using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Minio;
using Paperless.Worker.OCR.Connectors;
using SkiaSharp;
using Tesseract;

namespace Paperless.Worker.OCR.Test;

[TestFixture]
public class ElasticsearchWorkerTests
{
    // -------------------------
    // "Show function with unit-tests."  (ES BaseAddress wird im Worker korrekt initialisiert)
    // -------------------------
    [Test]
    public async Task StartAsync_CreatesElasticsearchClient_FromEnvHostPort()
    {
        var oldHost = Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST");
        var oldPort = Environment.GetEnvironmentVariable("ELASTICSEARCH_PORT");

        try
        {
            Environment.SetEnvironmentVariable("ELASTICSEARCH_HOST", "paperless-elasticsearch");
            Environment.SetEnvironmentVariable("ELASTICSEARCH_PORT", "9201");

            var worker = CreateWorker();

            // Cancel sofort, damit RabbitMQ Setup nicht hängt (SetupAsync beendet dann schnell)
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await worker.StartAsync(cts.Token);

            var esHttp = GetPrivateField<HttpClient?>(worker, "_esHttp");
            Assert.That(esHttp, Is.Not.Null);
            Assert.That(esHttp!.BaseAddress, Is.EqualTo(new Uri("http://paperless-elasticsearch:9201")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ELASTICSEARCH_HOST", oldHost);
            Environment.SetEnvironmentVariable("ELASTICSEARCH_PORT", oldPort);
        }
    }

    [Test]
    public async Task StartAsync_FallsBack_WhenPlaceholders()
    {
        var oldHost = Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST");
        var oldPort = Environment.GetEnvironmentVariable("ELASTICSEARCH_PORT");

        try
        {
            Environment.SetEnvironmentVariable("ELASTICSEARCH_HOST", "{ELASTICSEARCH_HOST}");
            Environment.SetEnvironmentVariable("ELASTICSEARCH_PORT", "{ELASTICSEARCH_PORT}");

            var worker = CreateWorker();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await worker.StartAsync(cts.Token);

            var esHttp = GetPrivateField<HttpClient?>(worker, "_esHttp");
            Assert.That(esHttp, Is.Not.Null);
            Assert.That(esHttp!.BaseAddress, Is.EqualTo(new Uri("http://elasticsearch:9200")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ELASTICSEARCH_HOST", oldHost);
            Environment.SetEnvironmentVariable("ELASTICSEARCH_PORT", oldPort);
        }
    }

    // -------------------------
    // "Store OCR text in Elasticsearch" + "Show functionality with unit-tests"
    // -> Test prüft: POST /document_texts/_update/{guid} + doc_as_upsert + doc.ocr enthält OCR Text
    // -------------------------
    [Test]
    public async Task HandleMessageAsync_UpsertsOcrTextIntoElasticsearch()
    {
        var worker = CreateWorker();

        // Tesseract so konfigurieren, dass Test-Setup (tessdata_root + x64/x86) verwendet wird
        ConfigureTesseractForTests(worker);

        // Minio mock: liefert PNG bytes zurück (kein echtes MinIO)
        var pngBytes = CreatePngBytesWithText("HELLO");
        InjectFakeMinioClient(worker, pngBytes);

        // Elasticsearch mock: Capture HTTP Request (kein echtes ES)
        var capture = new CaptureHandler();
        var fakeEsHttp = new HttpClient(capture)
        {
            BaseAddress = new Uri("http://fake-es:9200")
        };
        SetPrivateField(worker, "_esHttp", fakeEsHttp);

        // Rabbit publish deaktivieren (damit test nicht am Publish hängt)
        SetPrivateField(worker, "_publishChannel", null);

        // Message: document_id muss mit GUID beginnen (dein Code nimmt erstes Segment)
        var docId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { document_id = $"{docId}/file.png" });

        await InvokeHandleMessageAsync(worker, json);

        // Assert: ES wurde aufgerufen
        Assert.That(capture.LastRequest, Is.Not.Null);
        Assert.That(capture.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capture.LastRequest!.RequestUri!.AbsolutePath,
            Is.EqualTo($"/document_texts/_update/{docId}"));

        Assert.That(capture.LastBody, Is.Not.Null);

        using var bodyJson = JsonDocument.Parse(capture.LastBody!);
        var root = bodyJson.RootElement;

        Assert.That(root.GetProperty("doc_as_upsert").GetBoolean(), Is.True);

        var doc = root.GetProperty("doc");
        var ocr = doc.GetProperty("ocr").GetString() ?? "";
        Assert.That(ocr.ToUpperInvariant(), Does.Contain("HELLO"),
            $"OCR text not found in ES payload. OCR:\n{ocr}");

        Assert.That(doc.TryGetProperty("timestamp", out _), Is.True);
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static OcrWorker CreateWorker()
    {
        var opts = Options.Create(new MinioStorageOptions
        {
            Endpoint = "localhost:9000",
            AccessKey = "x",
            SecretKey = "y",
            BucketName = "bucket",
            UseSSL = false
        });

        return new OcrWorker(opts);
    }

    private static async Task InvokeHandleMessageAsync(OcrWorker worker, string json)
    {
        var mi = typeof(OcrWorker).GetMethod("HandleMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(mi, Is.Not.Null, "HandleMessageAsync not found via reflection.");

        var task = (Task?)mi!.Invoke(worker, new object[] { json });
        Assert.That(task, Is.Not.Null);
        await task!;
    }

    private static void ConfigureTesseractForTests(OcrWorker worker)
    {
        var baseDir = AppContext.BaseDirectory;

        // Native DLLs: x64 oder x86 (muss im Output liegen)
        var archFolder = Environment.Is64BitProcess ? "x64" : "x86";
        var nativeDir = Path.Combine(baseDir, archFolder);
        Assert.That(Directory.Exists(nativeDir), Is.True, $"Missing native folder in output: {nativeDir}");

        // Tesseract .NET soll native libs dort suchen
        TesseractEnviornment.CustomSearchPath = nativeDir;

        // tessdata_root im Output
        var tessRoot = Path.Combine(baseDir, "tessdata_root");

        // !!! WICHTIG: dein Connector erwartet direkt den tessdata-Ordner !!!
        var tessData = Path.Combine(tessRoot, "tessdata");
        Assert.That(Directory.Exists(tessData), Is.True, $"Missing tessdata in output: {tessData}");
        Assert.That(File.Exists(Path.Combine(tessData, "eng.traineddata")), Is.True, "Missing eng.traineddata.");

        // Prefix setzen
        Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessData);

        // TesseractConnector default ist "/tessdata" -> im Test per Reflection auf tessData setzen
        var tesseractConnector = GetPrivateField<TesseractConnector>(worker, "_tesseract");

        var field = typeof(TesseractConnector).GetField("_tessdataPath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Could not find _tessdataPath in TesseractConnector.");

        field!.SetValue(tesseractConnector, tessData);
    }

    private static byte[] CreatePngBytesWithText(string text)
    {
        using var bitmap = new SKBitmap(600, 250);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 96,
            IsAntialias = true
        };

        canvas.DrawText(text, 20, 160, paint);

        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void InjectFakeMinioClient(OcrWorker worker, byte[] payload)
    {
        var minioConnector = GetPrivateField<MinioConnector>(worker, "_minio");

        // Proxy erzeugen: IMinioClient wird zur Laufzeit implementiert
        var client = DispatchProxy.Create<IMinioClient, MinioClientProxy>();
        var proxy = (MinioClientProxy)(object)client!;
        proxy.Payload = payload;

        // privaten _client in MinioConnector ersetzen
        var clientField = typeof(MinioConnector).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(clientField, Is.Not.Null, "Could not find _client field in MinioConnector.");
        clientField!.SetValue(minioConnector, client);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(fi, Is.Not.Null, $"Field '{fieldName}' not found on {obj.GetType().Name}");
        return (T)fi!.GetValue(obj)!;
    }

    private static void SetPrivateField(object obj, string fieldName, object? value)
    {
        var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(fi, Is.Not.Null, $"Field '{fieldName}' not found on {obj.GetType().Name}");
        fi!.SetValue(obj, value);
    }

    // -------------------------
    // Capture Elasticsearch HTTP calls
    // -------------------------
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }

    // -------------------------
    // MinIO DispatchProxy (wichtig: implementiert NICHT IMinioClient!)
    // -------------------------
    private class MinioClientProxy : DispatchProxy
    {
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            // MinioConnector nutzt nur:
            // - StatObjectAsync(StatObjectArgs, CancellationToken?)
            // - GetObjectAsync(GetObjectArgs, CancellationToken?)
            if (targetMethod.Name == "StatObjectAsync")
            {
                // ReturnType Task<ObjectStat>
                return CreateTaskForExpectedReturnType(targetMethod.ReturnType);
            }

            if (targetMethod.Name == "GetObjectAsync")
            {
                if (args == null || args.Length == 0 || args[0] == null)
                    return CreateTaskForExpectedReturnType(targetMethod.ReturnType);

                var getArgs = args[0];
                var cb = FindStreamCallback(getArgs);
                if (cb == null)
                    throw new InvalidOperationException("Could not locate callback delegate in GetObjectArgs.");

                // Callback ausführen (damit MinioConnector die Bytes bekommt)
                var cbTask = InvokeCallback(cb, Payload);

                // ReturnType exakt liefern
                return ContinueWithExpectedReturnType(cbTask, targetMethod.ReturnType);
            }


            throw new NotSupportedException($"MinioClientProxy does not support method: {targetMethod.Name}");
        }

        private static object CreateTaskForExpectedReturnType(Type returnType)
        {
            // Erwartet: Task<T>
            if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
                return Task.CompletedTask;

            var tResult = returnType.GetGenericArguments()[0];

            // ObjectStat (oder was auch immer Minio erwartet) dummy erstellen
            object dummy = CreateDummyInstance(tResult);

            // Task.FromResult<T>(dummy) via Reflection
            var fromResult = typeof(Task).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethodDefinition);

            var generic = fromResult.MakeGenericMethod(tResult);
            return generic.Invoke(null, new[] { dummy })!;
        }

        private static object CreateDummyInstance(Type t)
        {
            try
            {
                // Falls type public ctor hat
                return Activator.CreateInstance(t)!;
            }
            catch
            {
                // Fallback: uninitialisiertes Objekt reicht, weil dein Code ObjectStat nicht ausliest
                #pragma warning disable SYSLIB0050
                return FormatterServices.GetUninitializedObject(t);
                #pragma warning restore SYSLIB0050
            }
        }

        private static Delegate? FindStreamCallback(object getObjectArgs)
        {
            var t = getObjectArgs.GetType();

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                var del = f.GetValue(getObjectArgs) as Delegate;
                if (del != null && LooksLikeStreamCallback(del))
                    return del;
            }

            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(Delegate).IsAssignableFrom(p.PropertyType)) continue;
                var del = p.GetValue(getObjectArgs) as Delegate;
                if (del != null && LooksLikeStreamCallback(del))
                    return del;
            }

            return null;
        }

        private static bool LooksLikeStreamCallback(Delegate del)
        {
            var invoke = del.GetType().GetMethod("Invoke");
            if (invoke == null) return false;

            var parms = invoke.GetParameters();
            return parms.Length >= 1 && typeof(Stream).IsAssignableFrom(parms[0].ParameterType);
        }

        private static Task InvokeCallback(Delegate cb, byte[] payload)
        {
            return Task.Run(async () =>
            {
                using var ms = new MemoryStream(payload);

                var invoke = cb.GetType().GetMethod("Invoke")!;
                var parms = invoke.GetParameters();

                object? result;
                if (parms.Length == 1)
                    result = cb.DynamicInvoke(ms);
                else if (parms.Length == 2 && parms[1].ParameterType == typeof(CancellationToken))
                    result = cb.DynamicInvoke(ms, CancellationToken.None);
                else
                    result = cb.DynamicInvoke(ms);

                if (result is Task task)
                    await task;
            });
        }
        private static object ContinueWithExpectedReturnType(Task cbTask, Type returnType)
        {
            // wenn GetObjectAsync sowieso Task zurückgibt:
            if (returnType == typeof(Task))
                return cbTask;

            // erwartet Task<T> -> wir geben Task<T> zurück, die nach cbTask completed
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var tResult = returnType.GetGenericArguments()[0];
                return ContinueWithGeneric(cbTask, tResult);
            }

            // Fallback
            return cbTask;
        }

        private static object ContinueWithGeneric(Task cbTask, Type tResult)
        {
            // Wir wollen: cbTask.ContinueWith(_ => dummyT)
            // und dann als Task<T> zurückgeben

            var dummy = CreateDummyInstance(tResult);

            // Task<T> via reflection: Task.FromResult<T>(dummy) aber erst NACH cbTask
            var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(tResult);
            var tcs = Activator.CreateInstance(tcsType)!;

            var setResult = tcsType.GetMethod("SetResult")!;
            var setException = tcsType.GetMethod("SetException", new[] { typeof(Exception) })!;
            var taskProp = tcsType.GetProperty("Task")!;
            var taskObj = taskProp.GetValue(tcs)!;

            _ = cbTask.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    setException.Invoke(tcs, new object[] { t.Exception.InnerException ?? t.Exception });
                    return;
                }

                if (t.IsCanceled)
                {
                    // canceled -> set exception (für Tests ok)
                    setException.Invoke(tcs, new object[] { new TaskCanceledException(t) });
                    return;
                }

                setResult.Invoke(tcs, new object[] { dummy });
            });

            return taskObj!;
        }
    }
}
