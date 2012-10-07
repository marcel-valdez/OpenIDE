﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharp.Crawlers.TypeResolvers.CodeEngine;
using CSharp.Projects;
using OpenIDE.Core.CodeEngineIntegration;

namespace CSharp.Crawlers.TypeResolvers
{
    public class EnclosingSignatureFromPosition
    {
        private Func<ICodeEngineInstanceSimple> _codeEngineFactory;
        private Func<string,string> _fileReader;
        private ICodeEngineInstanceSimple _codeEngine;

        public EnclosingSignatureFromPosition(
            Func<ICodeEngineInstanceSimple> codeEngineFactory,
            Func<string,string> fileReader) {
            _codeEngineFactory = codeEngineFactory;
            _fileReader = fileReader;
        }

        public string GetSignature(string file, int line, int column) {
            var dirtyFile = getCodeEngine().Query("editor get-dirty-files \"" + file + "\"");
            if (dirtyFile != "")
                file = dirtyFile;

            var parser = new NRefactoryParser();
            var cache = new OutputWriter();
            parser.SetOutputWriter(cache);
            var fileRef = new FileRef(file, null);
            parser.ParseFile(fileRef, () => _fileReader(file));

            cache.BuildTypeIndex();
            new TypeResolver(new OutputWriterCacheReader(cache))
                .ResolveAllUnresolved(cache);
            
            var references = new List<ICodeReference>();
            references.AddRange(cache.Namespaces);
            references.AddRange(cache.Classes);
            references.AddRange(cache.Interfaces);
            references.AddRange(cache.Structs);
            references.AddRange(cache.Enums);
            references.AddRange(cache.Methods);

            if (references.Count == 0)
                return null;

            var insideOf = references
                    .Where(x => x.Line <= line && x.EndLine >= line);
            if (insideOf.Count() == 0)
                return null;

            var match = references
                .FirstOrDefault(x => x.Line == insideOf.Max(y => y.Line));

            if (match == null)
                return null;
            return match.GenerateFullSignature();
        }

        public ICodeEngineInstanceSimple getCodeEngine() {
            if (_codeEngine == null) {
                _codeEngine = _codeEngineFactory();
                if (_codeEngine == null)
                    return null;
                _codeEngine.KeepAlive();
            }
            return _codeEngine;
        }
    }
}