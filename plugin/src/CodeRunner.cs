using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using DApp = Deswik.Graphics.Application;

namespace Kennet.DeswikClaude
{
    /// <summary>
    /// Compiles and runs a C# snippet inside Deswik, with access to the CAD application and the IS schedule.
    /// Deswik 2026.1 runs on .NET 8, which has no built-in compiler, so the plugin ships Roslyn
    /// (Microsoft.CodeAnalysis*.dll) next to its own DLL.
    /// </summary>
    public static class CodeRunner
    {
        private static readonly string[] DefaultUsings =
        {
            "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO"
        };


        public static string BuildSource(string body, IEnumerable<string> extraUsings)
        {
            var usings = new List<string>(DefaultUsings);
            if (extraUsings != null)
                foreach (string u in extraUsings)
                {
                    string clean = (u ?? "").Trim().TrimEnd(';');
                    if (clean.StartsWith("using ")) clean = clean.Substring(6).Trim();
                    if (clean.Length > 0 && !usings.Contains(clean)) usings.Add(clean);
                }

            var sb = new StringBuilder();
            foreach (string u in usings) sb.Append("using ").Append(u).Append(";\n");
            sb.Append("namespace KennetDynamic {\n");
            sb.Append("public static class Script {\n");
            sb.Append("public static object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, System.Text.StringBuilder Out) {\n");
            sb.Append("#line 1 \"snippet\"\n");
            sb.Append(body);
            sb.Append("\n#line default\n");
            sb.Append("#pragma warning disable 162\nreturn null;\n}\n}\n}\n");
            return sb.ToString();
        }

        /// <summary>Returns the text Claude sees: compile errors, or Out text plus the return value.</summary>
        public static string CompileAndRun(string body, IEnumerable<string> usings, DApp app, Deswik.Scheduler.Engine.Schedule schedule)
        {
            string source = BuildSource(body, usings);
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));

            // Reference everything already loaded in Deswik (one copy per assembly name).
            var refs = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location) || !File.Exists(asm.Location)) continue;
                    string name = asm.GetName().Name;
                    if (name.StartsWith("KennetDynamic", StringComparison.Ordinal) || refs.ContainsKey(name)) continue;
                    refs[name] = MetadataReference.CreateFromFile(asm.Location);
                }
                catch { /* some assemblies refuse Location or aren't readable metadata; skip */ }
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug, allowUnsafe: true,
                reportSuppressedDiagnostics: false);
            string asmName = "KennetDynamic_" + Guid.NewGuid().ToString("N");

            List<Diagnostic> errors;
            byte[] image = Emit(CSharpCompilation.Create(asmName, new[] { tree }, refs.Values, options), out errors);

            // Retry: a type lives in a Deswik DLL that isn't loaded yet (CS0012) -> add it from the install folder.
            var missing = errors.Where(e => e.Id == "CS0012")
                                .Select(e => MissingAssemblyName(e.GetMessage())).Where(n => n != null).Distinct().ToList();
            if (missing.Count > 0)
            {
                foreach (string m in missing)
                {
                    string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, m + ".dll");
                    if (File.Exists(p) && !refs.ContainsKey(m)) refs[m] = MetadataReference.CreateFromFile(p);
                }
                image = Emit(CSharpCompilation.Create(asmName, new[] { tree }, refs.Values, options), out errors);
            }

            // Retry: a loaded assembly can't be used as a reference (native/mixed image, unreadable metadata).
            var unreadable = errors.Where(e => e.Id == "CS0009" || e.Id == "CS0006").Select(e => e.GetMessage()).ToList();
            if (unreadable.Count > 0)
            {
                foreach (var kv in refs.ToList())
                {
                    string file = Path.GetFileName(((PortableExecutableReference)kv.Value).FilePath ?? "");
                    if (file.Length > 0 && unreadable.Any(m => m.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0)) refs.Remove(kv.Key);
                }
                image = Emit(CSharpCompilation.Create(asmName, new[] { tree }, refs.Values, options), out errors);
            }

            if (image == null)
            {
                var sb = new StringBuilder("COMPILE FAILED (C# 12, .NET 8).\n");
                foreach (var e in errors.Take(25))
                {
                    var pos = e.Location.GetMappedLineSpan();
                    sb.AppendFormat("line {0}: {1} {2}\n", pos.StartLinePosition.Line + 1, e.Id, e.GetMessage());
                }
                return sb.ToString();
            }

            Assembly compiled = Assembly.Load(image);
            Type t = compiled.GetType("KennetDynamic.Script");
            MethodInfo run = t.GetMethod("Run");
            var output = new StringBuilder();
            try
            {
                object ret = run.Invoke(null, new object[] { app, schedule, output });
                if (ret != null) output.Append("\n[return] ").Append(ret);
                try { if (app != null) app.Redraw(true); } catch { }
                return output.Length == 0 ? "OK (no output)" : output.ToString();
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                return "RUNTIME ERROR: " + inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace +
                       (output.Length > 0 ? "\n--- output before error ---\n" + output : "");
            }
        }

        /// <summary>Compiles to an in-memory DLL; returns null and the errors if it fails.</summary>
        private static byte[] Emit(CSharpCompilation compilation, out List<Diagnostic> errors)
        {
            using (var ms = new MemoryStream())
            {
                EmitResult res = compilation.Emit(ms);
                errors = res.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                return res.Success ? ms.ToArray() : null;
            }
        }

        /// <summary>Pulls 'Deswik.Geometry' out of "...add a reference to assembly 'Deswik.Geometry, Version=...'".</summary>
        private static string MissingAssemblyName(string errorText)
        {
            int i = errorText.IndexOf("assembly '", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            int start = i + 10;
            int end = errorText.IndexOfAny(new[] { ',', '\'' }, start);
            return end > start ? errorText.Substring(start, end - start).Trim() : null;
        }

        public static string SaveScript(string description, string body, IEnumerable<string> usings)
        {
            string safe = new string((description ?? "script").Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray()).Trim();
            if (safe.Length > 60) safe = safe.Substring(0, 60);
            if (safe.Length == 0) safe = "script";
            string path = Path.Combine(PluginSettings.ScriptsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss") + " " + safe + ".cs");
            File.WriteAllText(path, "// " + description + "\n// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)\n// Usings: " +
                                    string.Join(", ", usings ?? new string[0]) + "\n\n" + body);
            return path;
        }
    }
}
