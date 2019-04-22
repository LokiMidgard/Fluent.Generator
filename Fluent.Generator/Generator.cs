using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fluent.Net.Ast;

namespace Fluent.Generator
{
    internal partial class Generator
    {

        private static readonly Regex translationRegex = new Regex(@"\.\w\w(-\w\w)?\.ftl$");

        public static string Generat(string rootNamespace, IEnumerable<string> ftlFiles)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Fluent.Net;
");

            foreach (var f in ftlFiles.Where(x => !translationRegex.IsMatch(x)))
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var parent = rootNamespace + "." + Path.GetDirectoryName(f).Replace('\\', '.');
                parent = parent.TrimEnd('.');

                using (var stream = File.OpenRead(f))
                using (var reader = new StreamReader(stream))
                {
                    Test(stringBuilder, name, parent, reader);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Controls use of [unicode isolating marks](https://www.w3.org/International/questions/qa-bidi-unicode-controls). Can be overridden on a per thread level using <see cref="UnicodeIsolationCurrentThread"/>.
        /// </summary>
        public static bool UnicodeIsolationGlobal { get; set; } = true;
        /// <summary>
        /// Controls use of [unicode isolating marks](https://www.w3.org/International/questions/qa-bidi-unicode-controls) for the current Thread. If <c>null</c> <see cref="UnicodeIsolationGlobal"/> will be used.
        /// </summary>
        public static bool UnicodeIsolationCurrentThread { get; set; } = true;

        public static void Test(StringBuilder stringBuilder, string name, string @namespace, TextReader ftl)
        {
            var parser = new Fluent.Net.Parser(false);
            var result = parser.Parse(ftl);


            stringBuilder.AppendLine($@"namespace {@namespace}
{{
    public class {name}
    {{
        private const string resourceName = ""{@namespace}.{name}"";
        private static readonly ThreadLocal<CultureInfo> culture = new ThreadLocal<CultureInfo>(() => null);
        private static readonly ThreadLocal<MessageContext> context = new ThreadLocal<MessageContext>(() => null);
        private static readonly ThreadLocal<bool?> targetIsolation = new ThreadLocal<bool?>(() => null);
        private static readonly ThreadLocal<bool?> currentIsolation = new ThreadLocal<bool?>(() => null);

        /// <summary>
        /// Controls use of [unicode isolating marks](https://www.w3.org/International/questions/qa-bidi-unicode-controls). Can be overridden on a per thread level using <see cref=""UnicodeIsolationCurrentThread""/>.
        /// </summary>
        public static bool UnicodeIsolationGlobal {{get; set;}} = true; 
        
        /// <summary>
        /// Controls use of [unicode isolating marks](https://www.w3.org/International/questions/qa-bidi-unicode-controls) for the current Thread. If <c>null</c> <see cref=""UnicodeIsolationGlobal""/> will be used.
        /// </summary>
        public static bool? UnicodeIsolationCurrentThread {{get => targetIsolation.Value; set => targetIsolation.Value = value;}} 


        private static MessageContext GetContext()
        {{
            bool newTargetIsolation = targetIsolation.Value ?? UnicodeIsolationGlobal;
            if (culture.Value != CultureInfo.CurrentUICulture || currentIsolation.Value != newTargetIsolation)
            {{
                var cultureValue = CultureInfo.CurrentUICulture;
                var ctx = new MessageContext(cultureValue.Name, new MessageContextOptions()
                {{ UseIsolating = newTargetIsolation}});
                var assembly = typeof({name}).Assembly;
                var names = assembly.GetManifestResourceNames();
                string correctResurce = null;
                var currentCulture = cultureValue;
                while (correctResurce == null)
                {{
                    var currentResource = $""{{resourceName}}{{(string.IsNullOrEmpty(currentCulture.Name) ? """" : ""."") }}{{currentCulture.Name}}.ftl"";
                    if (names.Contains(currentResource))
                        correctResurce = currentResource;
                    else if (currentCulture.Equals(CultureInfo.InvariantCulture))
                        throw new FileNotFoundException(""Resource not found"",currentResource);
                    currentCulture = currentCulture.Parent;
                }}

                FluentResource fluentResource;
                using (var stream = assembly.GetManifestResourceStream(correctResurce))
                    using (var reader = new StreamReader(stream))
                    {{
                        fluentResource = FluentResource.FromReader(reader);
                    }}

                ctx.AddResource(fluentResource);
                context.Value = ctx;
                culture.Value = cultureValue;
                currentIsolation.Value = newTargetIsolation;
            }}

            return context.Value;
        }}");



            var enumerable = result.Body.OfType<Message>().ToArray();
            foreach (var message in enumerable)
            {
                var id = message.Id.Name;
                var comment = message.Comment;
                var propName = id.ToCharArray();
                propName[0] = char.ToUpperInvariant(propName[0]);
                while (propName.Contains('-'))
                {
                    var index = Array.LastIndexOf(propName, '-');
                    if (index + 1 < propName.Length)
                    {
                        propName[index + 1] = char.ToUpperInvariant(propName[index + 1]);
                        propName[index] = (char)0;
                    }
                }

                var prop = new string(propName);
                prop = prop.Replace(((char)0).ToString(), "");

                var variables = GetVariables(message.Value).Distinct().ToArray();

                var commentData = GenerateDoc(variables, comment.Content);

                if (variables.Length > 0)
                {

                    ComplexMessageStruct(stringBuilder, id, prop, variables, commentData);
                    WriteComment(stringBuilder, commentData, false);
                    GetComplexProperty(stringBuilder, prop);
                }
                else
                {
                    SimpleProperty(stringBuilder, prop, id);

                }
            }

            stringBuilder.AppendLine("}\n}");
        }

        private static void WriteComment(StringBuilder stringBuilder, (string comment, (string name, Type type, string comment)[] parameter) commentData, bool writeParameter)
        {
            if (commentData.comment != null)
            {
                stringBuilder.AppendLine("/// <summary>");
                WritePrefixedLines(commentData.comment);
                stringBuilder.AppendLine("/// </summary>");
            }

            if (writeParameter)
            {
                foreach (var parameter in commentData.parameter)
                {
                    stringBuilder.AppendLine($@"/// <param name=""{parameter.name}"">");
                    WritePrefixedLines(parameter.comment);
                    stringBuilder.AppendLine("/// </param>");
                }
            }

            void WritePrefixedLines(string str)
            {
                foreach (var item in str.Replace("\r\n", "\n").Split('\n'))
                    stringBuilder.AppendLine($"/// {item}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="photoCount"></param>
        /// <param name="userGender"></param>
        /// <returns></returns>


        private static void SimpleProperty(StringBuilder stringBuilder, string propertyName, string messageId)
        {

            stringBuilder.AppendLine($"public static string {propertyName} => GetContext().Format(GetContext().GetMessage(\"{messageId}\"));");
        }

        private static void GetComplexProperty(StringBuilder stringBuilder, string propertyName)
        {
            stringBuilder.AppendLine($"    public static Wrapper.{propertyName}Wrapper {propertyName} => new Wrapper.{propertyName}Wrapper(GetContext());");
        }

        private static (string comment, (string name, Type type, string comment)[] parameter) GenerateDoc(string[] variables, string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return (null, new (string name, Type type, string comment)[0]);

            var data = new List<(string name, Type type, StringBuilder comment)>();
            var normalComment = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(comment))
            {
                var lines = comment.Replace("\r\n", "\n").Split('\n');
                Regex variableFormat = new Regex(@"^\$(?<name>[^ ]+)\s+(\((?<type>[^)]+)\))?\s*-\s*(?<rest>.*)$");

                foreach (var l in lines)
                {
                    var match = variableFormat.Match(l);
                    if (match.Success)
                    {
                        var name = match.Groups["name"].Value;
                        if (variables.Contains(name))
                        {
                            var t = match.Groups["type"];

                            data.Add((name, GetTypeFromMatch(t), new StringBuilder(match.Groups["rest"].Value)));
                        }
                    }
                    else if (data.Any())
                    {
                        data[data.Count - 1].comment.AppendLine(l);
                    }
                    else
                    {
                        normalComment.AppendLine(l);
                    }
                }
            }
            return (normalComment.ToString(), data.Select(x => (x.name, x.type, x.comment.ToString())).ToArray());
        }

        private static Type GetTypeFromMatch(Group t)
        {
            if (!t.Success)
                return null;
            switch (t.Value)
            {
                case "string":
                case "String":
                    return typeof(string);

                case "number":
                case "Number":
                    return typeof(double);

                case "int":
                    return typeof(int);

                case "float":
                    return typeof(float);

                case "double":
                    return typeof(double);

                case "long":
                    return typeof(long);

                default:
                    return Type.GetType(t.Value, false) ?? typeof(object);
            }

        }

        private static void ComplexMessageStruct(StringBuilder stringBuilder, string messageId, string propertyName, string[] variables, (string comment, (string name, Type type, string comment)[] parameter) commentData)
        {


            stringBuilder.AppendLine($@"public static partial class Wrapper{{
public struct {propertyName}Wrapper
        {{
            private readonly MessageContext messageContext;
            public {propertyName}Wrapper(MessageContext messageContext)
            {{
                this.messageContext = messageContext;
            }}");

            WriteComment(stringBuilder, commentData, true);
            stringBuilder.AppendLine($@"
            public string this[{string.Join(", ", variables.Select(x =>
            {
                var type = commentData.parameter.FirstOrDefault(y => x == y.name).type ?? typeof(object);
                return $"{type.FullName} {x}";
            }))}]
            {{
                get
                {{
                    return this.messageContext.Format(this.messageContext.GetMessage(""{messageId}""), new Dictionary<string, object>{{{string.Join(", ", variables.Select(x => $@"{{""{x}"", {x}}}"))}}});
                }}
            }}
        }}
}}");
        }

        //public static Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax Test(string name, string @namespace, TextReader ftl)
        //{
        //    var parser = new Fluent.Net.Parser(false);
        //    var result = parser.Parse(ftl);



        //    var classsDeclaration = GenerateClass(name, $"{@namespace}.{name}");

        //    foreach (var item in result.Body.OfType<Message>())
        //    {
        //        var id = item.Id.Name;
        //        var variables = GetVariables(item.Value).Distinct().ToArray();
        //        if (variables.Length > 0)
        //        {
        //            classsDeclaration = classsDeclaration.AddMembers(ComplexMessageStruct(id, id, variables), GetComplexProperty(id));
        //        }
        //        else
        //        {
        //            classsDeclaration = classsDeclaration.AddMembers(SimpleProperty(id, id));

        //        }
        //    }

        //    var element = GetNamespace(@namespace, classsDeclaration);
        //    return element;
        //}

        private static IEnumerable<string> GetVariables(SyntaxNode value)
        {
            switch (value)
            {
                case Pattern pattern:
                    return pattern.Elements.SelectMany(GetVariables);
                case Placeable placeable:
                    return GetVariables(placeable.Expression);
                case VariableReference variableReference:
                    return new[] { variableReference.Id.Name };
                case SelectExpression selectExpression:
                    return GetVariables(selectExpression.Selector)
                        .Concat(selectExpression.Variants.SelectMany(x => GetVariables(x.Value)));
                case Fluent.Net.Ast.CallExpression callExpression:
                    return callExpression.Named.Select<NamedArgument, SyntaxNode>(x => x.Value).Concat(callExpression.Positional).SelectMany(GetVariables);

                default:
                    return Enumerable.Empty<string>();
            }
        }
    }
}
