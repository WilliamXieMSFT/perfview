﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Tracing.AutomatedAnalysis
{
    internal sealed class AnalyzerResolver
    {
        private const string AnalyzersDirectoryName = "Analyzers";
        private static string s_analyzersDirectory;

        internal static string AnalyzersDirectory
        {
            get
            {
                if (s_analyzersDirectory == null)
                {
                    // Assume plugins sit in a plugins directory next to the current assembly.
#if AUTOANALYSIS_EXTENSIBILITY
                    string probePath = Environment.GetEnvironmentVariable("TRACEEVENT_ANALYZER_PATH");
                    if (!string.IsNullOrEmpty(probePath))
                    {
                        s_analyzersDirectory = Environment.ExpandEnvironmentVariables(probePath);
                    }
                    else
                    {
                        s_analyzersDirectory = Path.Combine(
                            Path.GetDirectoryName(typeof(AnalyzerResolver).Assembly.Location),
                            AnalyzersDirectoryName);
                    }
#endif
                }
                return s_analyzersDirectory;
            }
        }

        internal static IEnumerable<Analyzer> GetAnalyzers()
        {
#if AUTOANALYSIS_EXTENSIBILITY
            // Iterate through all assemblies in the analyzers directory.
            if (!Directory.Exists(AnalyzersDirectory))
            {
                yield break;
            }

            string[] candidateAssemblies = Directory.GetFiles(AnalyzersDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach(string candidateAssembly in candidateAssemblies)
            {
                // NOTE: If an assembly with the same identity is already loaded,
                // LoadFrom will return the loaded assembly even if a different path was specified.
                Assembly assembly = Assembly.LoadFrom(candidateAssembly);
                if (assembly != null &&
                    assembly.GetCustomAttribute(typeof(AnalyzerProviderAttribute)) is AnalyzerProviderAttribute attr &&
                    attr.ProviderType != null)
                {
                    // Create an instance of the provider.
                    IAnalyzerProvider analyzerProvider = Activator.CreateInstance(attr.ProviderType) as IAnalyzerProvider;
                    if (analyzerProvider != null)
                    {
                        foreach (Analyzer analyzer in analyzerProvider.GetAnalyzers())
                        {
                            yield return analyzer;
                        }
                    }
                }
            }
#else
            yield break;
#endif

        }

        internal static Configuration GetConfiguration()
        {
            Configuration configuration = new Configuration();

#if AUTOANALYSIS_EXTENSIBILITY
            // Iterate through all configuration files in the analyzers directory.
            if (Directory.Exists(AnalyzersDirectory))
            {
                string[] filePaths = Directory.GetFiles(AnalyzersDirectory, "*.config.xml");
                foreach (string filePath in filePaths)
                {
                    configuration.AddConfigurationFile(filePath);
                }
            }
#endif

            return configuration;
        }
    }
}