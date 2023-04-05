using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;

string inputFolderPath = "InputFolder";
string outputFolderPath = "OutputFolder";

// Create the input and output folders if they don't exist
if (!Directory.Exists(inputFolderPath))
{
    Directory.CreateDirectory(inputFolderPath);
}

if (!Directory.Exists(outputFolderPath))
{
    Directory.CreateDirectory(outputFolderPath);
}

string outputFileName = Path.Combine(outputFolderPath, "erDiagramSource.cs");
string outputDiagramFileName = Path.Combine(outputFolderPath, "erDiagram.mmd");
// Delete the output file if it already exists
if (File.Exists(outputFileName))
{
    File.Delete(outputFileName);
}
if (File.Exists(outputDiagramFileName))
{
    File.Delete(outputDiagramFileName);
}

int fileCount = 0;
// Loop through all files in the input folder
foreach (string inputFile in Directory.GetFiles(inputFolderPath))
{
    // Build the script from the input file
    string script = BuildScript(inputFile);
    fileCount ++;
    // Save the script to the output folder with the same name but with .mmd extension
    //string outputFileName = Path.Combine(outputFolderPath, Path.GetFileNameWithoutExtension(inputFile) + ".mmd");

    SaveScript(script, outputFileName);
}

Console.WriteLine($"{fileCount} files processed successfully.");
string code = ReadOutputFile(outputFileName);
string mmdScript = GenerateERDiagram(code);
//string mmdScript = mmdBuilder.ERDiagramBuilder.GenerateMermaidERDiagram(code);
SaveScript(mmdScript, outputDiagramFileName);
Console.WriteLine("mermaid Script created");

static string BuildScript(string inputFile)
{
    // Read the contents of the input file into a StringBuilder object
    StringBuilder sb = new StringBuilder();
    bool isInsideClass = false;
    foreach (string line in File.ReadAllLines(inputFile))
    {
        // Ignore comment lines
        if (line.TrimStart().StartsWith("//"))
        {
            continue;
        }

        if (!isInsideClass && line.TrimStart().StartsWith("public class "))
        {
            isInsideClass = true;
            sb.AppendLine(line.TrimStart());
            sb.AppendLine("{");
        }

        if (isInsideClass)
        {
            if (line.Trim().StartsWith("public "))
            {
                if (line.TrimStart().StartsWith("public class"))
                {
                    continue;
                }
                string[] words = line.Trim().Split(' ');
                if (words[1].ToLower() == "required")
                {
                    sb.AppendFormat("  {0} {1} {2} {3}\n", words[0], words[2], words[3].TrimEnd(';'), "R");
                }
                else
                {
                    sb.AppendFormat("  {0} {1} {2} {3}\n", words[0], words[1], words[2].TrimEnd(';'), "C");
                }
                
            }
        }

        if (isInsideClass && line.Trim() == "}")
        {
            isInsideClass = false;
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    return sb.ToString();
}
static void SaveScript(string script, string outputFileName)
    {
        // Write the script to the output file
        //File.WriteAllText(outputFileName, script);
        File.AppendAllText(outputFileName, script);

    }

 static string GenerateERDiagram(string code)
{
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("```mermaid");
    sb.AppendLine("erDiagram");
    
    Regex classRegex = new Regex(@"\bpublic\s+class\s+(\w+)\s*\{([^}]*)\}");
    //Regex propertyRegex = new Regex(@"\bpublic\s+(\w+)\s+(\w+)\s*\{");
    //Regex propertyRegex = new Regex(@"\bpublic\s+(\w+)\s+(\w+)\s*");
    Regex propertyRegex = new Regex(@"\bpublic\s+(\w+)\s+(\w+)\s+(\w+)\s*");



    MatchCollection classMatches = classRegex.Matches(code);

    //build list of class name for later use
    StringBuilder sbClassName = new StringBuilder();
    foreach (Match match in classMatches)
    {
        if (match.Success)
        {
            sbClassName.AppendLine(match.Groups[1].Value);
        }
    }


    StringBuilder sbRelationShip = new StringBuilder();
    List<string> RelationShips = new List<string>();


    foreach (Match classMatch in classMatches)
    {
        string className = classMatch.Groups[1].Value;

        sb.AppendLine($"{className} {{");

        MatchCollection propertyMatches = propertyRegex.Matches(classMatch.Groups[2].Value);

        foreach (Match propertyMatch in propertyMatches)
        {
            string propertyType = propertyMatch.Groups[1].Value;
            string propertyName = propertyMatch.Groups[2].Value;

            var keyAttribute = propertyName.ToLower() switch
            {
                "id" => "PK",
            var s when s.EndsWith("id") && sbClassName.ToString().ToLower().Contains(s.Replace("id", "")) && s.Length > 5 => "FK",
                _ => string.Empty
            };
            var comment = propertyMatch.Groups[3].Value.ToUpper() == "R" ? "\"*\"" : string.Empty;

            if (keyAttribute == "FK")
            {
                string parentName = propertyMatch.Groups[2].Value.Replace("Id", "");
                sbRelationShip.AppendLine($"{parentName} ||--o{{ {className} : has");
                RelationShips.Add($"{parentName} ||--o{{ {className} : has");
            }
            sb.AppendLine($"  {propertyType} {propertyName} {keyAttribute} {comment}");
        }

        sb.AppendLine("}");
        
    }
    //sb.AppendLine(sbRelationShip.ToString());

    IEnumerable<string> sortedList = RelationShips.OrderBy(str => str);
    foreach (string str in sortedList)
    {
        sb.AppendLine(str);
    }
    sb.AppendLine("```");
    return sb.ToString();
}




static string ReadOutputFile(string outputFileName)
{
    // Read the contents of the output file into a string object
    return File.ReadAllText(outputFileName);
}