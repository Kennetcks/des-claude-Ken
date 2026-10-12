using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using DApp = Deswik.Graphics.Application;

namespace Kennet.DeswikClaude
{
    /// <summary>
    /// Compiles and runs a C# snippet inside Deswik, with access to the CAD application and the IS schedule.
    /// Uses the C# compiler that ships with .NET Framework (C# 5 syntax), since Deswik doesn't include Roslyn.
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

            var cp = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false,
                TreatWarningsAsErrors = false,
                CompilerOptions = "/optimize-"
            };

            // Reference everything already loaded in Deswik (one copy per assembly name).
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location) || !File.Exists(asm.Location)) continue;
                    string name = asm.GetName().Name;
                    if (name.StartsWith("KennetDynamic", StringComparison.Ordinal)) continue;
                    if (seen.Add(name)) cp.ReferencedAssemblies.Add(asm.Location);
                }
                catch { /* some assemblies refuse Location; skip */ }
            }

            CompilerResults res = Compile(cp, source);
            var errors = res.Errors.Cast<CompilerError>().Where(e => !e.IsWarning).ToList();

            // Retry 1: a type lives in a Deswik DLL that isn't loaded yet (CS0012) -> add it from the install folder.
            var missing = errors.Where(e => e.ErrorNumber == "CS0012")
                                .Select(e => MissingAssemblyName(e.ErrorText)).Where(n => n != null).Distinct().ToList();
            if (missing.Count > 0)
            {
                foreach (string m in missing)
                {
                    string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, m + ".dll");
                    if (File.Exists(p) && seen.Add(m)) cp.ReferencedAssemblies.Add(p);
                }
                res = Compile(cp, source);
                errors = res.Errors.Cast<CompilerError>().Where(e => !e.IsWarning).ToList();
            }

            // Retry 2: a loaded assembly can't be used as a reference (native image, duplicate identity, etc.).
            if (errors.Any(e => e.ErrorNumber == "CS0006" || e.ErrorNumber == "CS0009" || e.ErrorNumber == "CS1703" || e.ErrorNumber == "CS1704"))
            {
                var keep = cp.ReferencedAssemblies.Cast<string>()
                    .Where(p =>
                    {
                        string n = Path.GetFileNameWithoutExtension(p);
                        return n.StartsWith("Deswik", StringComparison.OrdinalIgnoreCase) || n.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                               n.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) || n.StartsWith("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                               n == "mscorlib" || n == "netstandard" || n == "Newtonsoft.Json" || n.StartsWith("Kennet", StringComparison.OrdinalIgnoreCase);
                    }).ToList();
                var bad = new HashSet<string>(errors.Where(e => e.ErrorNumber == "CS0006" || e.ErrorNumber == "CS0009")
                                                     .Select(e => e.ErrorText), StringComparer.OrdinalIgnoreCase);
                cp.ReferencedAssemblies.Clear();
                foreach (string p in keep)
                    if (!bad.Any(b => b.IndexOf(Path.GetFileName(p), StringComparison.OrdinalIgnoreCase) >= 0))
                        cp.ReferencedAssemblies.Add(p);
                res = Compile(cp, source);
                errors = res.Errors.Cast<CompilerError>().Where(e => !e.IsWarning).ToList();
            }
            if (errors.Count > 0)
            {
                var sb = new StringBuilder("COMPILE FAILED (C# 5 compiler: no $\"\" strings, no ?. operator, no nameof, no => members).\n");
                foreach (var e in errors.Take(25))
                    sb.AppendFormat("line {0}: {1} {2}\n", e.Line, e.ErrorNumber, e.ErrorText);
                return sb.ToString();
            }

            Type t = res.CompiledAssembly.GetType("KennetDynamic.Script");
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

        private static CompilerResults Compile(CompilerParameters cp, string source)
        {
            using (var provider = new CSharpCodeProvider())
                return provider.CompileAssemblyFromSource(cp, source);
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
