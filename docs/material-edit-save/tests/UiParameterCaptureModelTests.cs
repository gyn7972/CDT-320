using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using QMC.Common.Data.Store;

internal static class UiParameterCaptureModelTests
{
    public static int Main(string[] args)
    {
        try
        {
            string assemblyPath = Path.GetFullPath(args[0]);
            string outputPath = Path.GetFullPath(args[1]);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string file = Path.Combine(Path.GetDirectoryName(assemblyPath), new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.UnsafeLoadFrom(file) : null;
            };
            var assembly = Assembly.UnsafeLoadFrom(assemblyPath);
            Type[] types = assembly.GetTypes().Where(t => t.IsPublic && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null &&
                t.GetInterfaces().Any(i => i.FullName == "QMC.Common.ISetupData" || i.FullName == "QMC.Common.IConfigData" || i.FullName == "QMC.Common.IRecipeData"))
                .OrderBy(t => t.Name).ToArray();
            int count = 0;
            double totalMs = 0;
            foreach (Type type in types)
            {
                // DTO만 생성합니다. Machine/Unit/Axis/IO 생성자는 호출하지 않습니다.
                object model = Activator.CreateInstance(type);
                string path = Path.Combine(outputPath, type.Name + ".json");
                var firstCapture = Stopwatch.StartNew();
                var prepared = JsonDataSaveCoordinator.Capture(model, path);
                double firstMs = firstCapture.Elapsed.TotalMilliseconds;
                using (var expected = new MemoryStream())
                {
                    JsonPrettySerializer.WriteObject(expected, type, model, JsonPrettySerializer.CreateSettings(true));
                    if (!prepared.Payload.SequenceEqual(expected.ToArray())) throw new Exception("JSON contract differs: " + type.Name);
                }
                double[] elapsed = new double[15];
                for (int i = 0; i < elapsed.Length; i++)
                {
                    var watch = Stopwatch.StartNew();
                    JsonDataSaveCoordinator.Capture(model, path);
                    elapsed[i] = watch.Elapsed.TotalMilliseconds;
                }
                Array.Sort(elapsed);
                totalMs += elapsed[elapsed.Length / 2];
                count++;
                Console.WriteLine(type.Name + ": bytes=" + prepared.Payload.Length + ", firstMs=" + firstMs.ToString("F3") + ", medianMs=" + elapsed[7].ToString("F3") + ", maxMs=" + elapsed[14].ToString("F3"));
            }
            Console.WriteLine("PASS: actual DTO snapshot contracts=" + count + ", totalMedianMs=" + totalMs.ToString("F3"));
            return count == 0 ? 1 : 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
