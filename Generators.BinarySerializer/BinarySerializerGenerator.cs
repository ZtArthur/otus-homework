using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators.BinarySerializer
{
    [Generator]
    public class BinarySerializerGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            const string source =
                @"
                namespace GeneratorsBinarySerializer
                {
                    public static partial class HelloGenerator
                    {
                        public static string GetMessage() => ""Hello"";
                    }
                }
            ";

            const string attributeSource =
                @"
                namespace GeneratorsBinarySerializer
                {
                    [System.AttributeUsage(System.AttributeTargets.Class)]
                    public sealed class GenerateBinarySerializerAttribute : System.Attribute
                    {
                    }
                }
            ";

            context.RegisterPostInitializationOutput(ctx =>
                {
                    ctx.AddSource("GenerateBinarySerializerAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
                    ctx.AddSource(hintName: "HelloGenerator.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            );
        }
    }
}