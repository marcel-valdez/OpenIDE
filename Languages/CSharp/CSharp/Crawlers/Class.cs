using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharp.Crawlers.TypeResolvers;
using CSharp.Projects;
namespace CSharp.Crawlers
{
	public class Class : TypeBase<Class>, ICodeReference
	{
        public bool AllTypesAreResolved { get; set; }

		public string Type { get; private set; }
        public FileRef File { get; private set; }
		public string Signature { get { return string.Format("{0}.{1}", Namespace, Name); } }
		public string Namespace { get; private set; }
		public string Name { get; private set; }
        public string Scope { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

        public Class(FileRef file, string ns, string name, string scope, int line, int column)
		{
            setThis(this);
			File = file;
			Namespace = ns;
			Name = name;
            Scope = scope;
			Line = line;
			Column = column;
		}

        public string GenerateFullSignature() {
            return Signature;
        }

        public IEnumerable<ResolveStatement> GetResolveStatements() {
            return getTypeResolveStatements();
        }

        protected override string getNamespace() {
            return Namespace;
        }
    }

    public class Field : CodeItemBase<Field>, ICodeReference 
	{
        public bool AllTypesAreResolved { get; set; }

		public string Type { get; private set; }
        public FileRef File { get; private set; }
		public string Signature { get {
                return string.Format("{0} {1}.{2}",
                    ReturnType,
                    Namespace,
                    Name);
            }
        }
		public string Namespace { get; private set; }
		public string Name { get; private set; }
        public string Scope { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

        public string ReturnType { get; private set; }

        public Field(FileRef file, string ns, string name, string scope, int line, int column, string returnType)
		{
            setThis(this);
            ReturnType = returnType;
			File = file;
			Namespace = ns;
			Name = name;
            Scope = scope;
			Line = line;
			Column = column;
		}

        public string GenerateFullSignature() {
            return Signature;
        }

        public IEnumerable<ResolveStatement> GetResolveStatements() {
            var list = new List<ResolveStatement>();
            list.Add(new ResolveStatement(ReturnType, Namespace, (s) => ReturnType = s));
            list.AddRange(getTypeResolveStatements());
            return list;
        }

        protected override string getNamespace() {
            return Namespace;
        }
    }

    public class Method : CodeItemBase<Method>, ICodeReference 
	{
        public bool AllTypesAreResolved { get; set; }

		public string Type { get; private set; }
        public FileRef File { get; private set; }
		public string Signature { get {
                var paramString = getParamString(Parameters);
                return string.Format("{0} {1}.{2}({3})",
                    ReturnType,
                    Namespace,
                    Name,
                    paramString);
            }
        }
		public string Namespace { get; private set; }
		public string Name { get; private set; }
        public string Scope { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

        public Parameter[] Parameters { get; private set; }
        public string ReturnType { get; private set; }

        public Method(FileRef file, string ns, string name, string scope, int line, int column, string returnType, IEnumerable<Parameter> parameters)
		{
            setThis(this);
            Parameters = parameters.ToArray();
            ReturnType = returnType;
			File = file;
			Namespace = ns;
			Name = name;
            Scope = scope;
			Line = line;
			Column = column;
		}

        public string GenerateFullSignature() {
            return Signature;
        }

        public string GenerateNameSignature() {
            var paramString = getParamString(Parameters);
            return string.Format("{0}.{1}({2})",
                Namespace,
                Name,
                paramString);
        }

        public IEnumerable<ResolveStatement> GetResolveStatements() {
            var list = new List<ResolveStatement>();
            list.Add(new ResolveStatement(ReturnType, Namespace, (s) => ReturnType = s));
            foreach (var parameter in Parameters)
                list.AddRange(parameter.GetResolveStatements());
            list.AddRange(getTypeResolveStatements());
            return list;
        }

        protected override string getJSON()
        {
            var json = new JSONWriter();
            base.getJSON(json);
            if (Parameters.Length > 0) {
                var parameters = new JSONWriter();
                foreach (var param in Parameters)
                    parameters.Append(param.Name, param.DeclaringType);
                json.AppendSection("parameters", parameters);
            }
            return json.ToString();
        }

        protected override string getNamespace() {
            return Namespace;
        }

        private string getParamString(IEnumerable<Parameter> parameters)
        {
            var sb = new StringBuilder();
            foreach (var param in parameters) {
                if (sb.Length == 0)
                    sb.Append(param.DeclaringType);
                else
                    sb.Append("," + param.DeclaringType);
            }
            return sb.ToString();
        }
    }

    public class Parameter : Variable
    {
        public Parameter(FileRef file, string ns, string name, string scope, int line, int column, string declaringType)
            : base(file, ns, name, scope, line, column, declaringType)
        {
        }
    }

    public class Variable : CodeItemBase<Variable>, ICodeReference
    {
        public bool AllTypesAreResolved { get; set; }

        public string Type { get; protected set; }
        public FileRef File { get; protected set; }
        public string Signature { get { return string.Format("{0} {1}.{2}", DeclaringType, Namespace, Name); } }
        public string Namespace { get; protected set; }
        public string Name { get; protected set; }
        public string Scope { get; protected set; }
        public int Line { get; protected set; }
        public int Column { get; protected set; }

        public string DeclaringType { get; protected set; }

        public Variable(FileRef file, string ns, string name, string scope, int line, int column, string declaringType)
        {
            setThis(this);
            File = file;
            Namespace = ns;
            Name = name;
            Scope = scope;
            Line = line;
            Column = column;
            DeclaringType = declaringType;
        }

        public string GenerateFullSignature() {
            return Signature;
        }

        public IEnumerable<ResolveStatement> GetResolveStatements() {
            var list = new List<ResolveStatement>();
            list.Add(new ResolveStatement(DeclaringType, Namespace, (s) => DeclaringType = s));
            list.AddRange(getTypeResolveStatements());
            return list;
        }

        protected override string getNamespace()
        {
            return Namespace;
        }
    }
}