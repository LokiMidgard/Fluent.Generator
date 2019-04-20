using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fluent.Net.Ast;

namespace Fluent.Generator
{
    internal partial class Generator
    {



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

            foreach (var f in ftlFiles)
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
        private static MessageContext GetContext()
        {{
            if (culture.Value != CultureInfo.CurrentUICulture)
            {{
                var cultureValue = CultureInfo.CurrentUICulture;
                var ctx = new MessageContext(cultureValue.Name);
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
            }}

            return context.Value;
        }}");



            var enumerable = result.Body.OfType<Message>().ToArray();
            foreach (var item in enumerable)
            {
                var id = item.Id.Name;
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

                var variables = GetVariables(item.Value).Distinct().ToArray();
                if (variables.Length > 0)
                {

                    ComplexMessageStruct(stringBuilder, id, prop, variables);
                    GetComplexProperty(stringBuilder, prop);
                }
                else
                {
                    SimpleProperty(stringBuilder, prop, id);

                }
            }

            stringBuilder.AppendLine("}\n}");
        }

        private static void SimpleProperty(StringBuilder stringBuilder, string propertyName, string messageId)
        {
            stringBuilder.AppendLine($"public static string {propertyName} => GetContext().Format(GetContext().GetMessage(\"{messageId}\"));");
        }

        private static void GetComplexProperty(StringBuilder stringBuilder, string propertyName)
        {
            stringBuilder.AppendLine($"    public static Wrapper.{propertyName}Wrapper {propertyName} => new Wrapper.{propertyName}Wrapper(GetContext());");
        }

        private static void ComplexMessageStruct(StringBuilder stringBuilder, string messageId, string propertyName, string[] variables)
        {


            stringBuilder.AppendLine($@"public static partial class Wrapper{{
public struct {propertyName}Wrapper
        {{
            private readonly MessageContext messageContext;
            public {propertyName}Wrapper(MessageContext messageContext)
            {{
                this.messageContext = messageContext;
            }}

            public string this[{string.Join(", ", variables.Select(x => $"object {x}"))}]
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
                default:
                    return Enumerable.Empty<string>();
            }
        }
    }
}
