namespace DockTemplate.Models.Documents;

public class DocumentModel
{
    public string Name { get; set; } = "Document";
    public string Content { get; set; } = "// Sample document content\nusing System;\n\nnamespace DockTemplate\n{\n    public class Example\n    {\n        public void Method()\n        {\n            Console.WriteLine(\"Hello, World!\");\n        }\n    }\n}";
}