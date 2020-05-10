using System;
using System.Diagnostics.SymbolStore;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace AydenIO.Management {
    internal static class ManagementSessionDebugUtilities {
#if DEBUG
        public static bool IsDebugBuild => true;

        private static ISymbolDocumentWriter _document;

        public static void DefineDocument(ModuleBuilder moduleBuilder, [CallerFilePath]string sourceFilePath = "") {
            ManagementSessionDebugUtilities._document = moduleBuilder.DefineDocument(sourceFilePath, SymLanguageType.CSharp, SymLanguageVendor.Microsoft, SymDocumentType.Text);
        }

        public static void MarkPoint(ILGenerator il, [CallerLineNumber]int lineNumber = -1) {
            il.MarkSequencePoint(ManagementSessionDebugUtilities._document, lineNumber, 0, lineNumber, 1);
        }
#else
        public static bool IsDebugBuild => false;

        public static void DefineDocument(ModuleBuilder moduleBuilder, string sourceFilePath = "") {
            
        }

        public static void MarkPoint(ILGenerator il, int lineNumber = -1) {
            
        }
#endif
    }
}
