using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace STPToXMLConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string stpFilePath = @"path\File1_STP.stp";
            string xmlFilePath = @"path\File1_OUTPUT_XML.xml";

            // Read STP file
            string stpContent = File.ReadAllText(stpFilePath);

            // Create an XmlWriter to write XML file
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(xmlFilePath, settings);

            // HEADER section
            // structure: KEYWORD(ATTRIBUTE_LIST);
            // ...

            // DATA section
            // structure: INSTANCE_NUMBERINSTANCE=KEYWORD(ATTRIBUTE_LIST);
            // ...

            // Write XML file header
            //writer.WriteStartDocument();
            
            // Split STP file into lines
            string[] stpLines = stpContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string materials_section = "/* MATERIALS */";
            string nodes_section = "/* NODES */";
            List<string> materialLines = new List<string>();
            List<string> nodesLines = new List<string>();

            // Find the line with "/* MATERIALS */"
            int materialsIndex = Array.IndexOf(stpLines, materials_section);
            if (materialsIndex != -1)
            {
                // Add each line after "/* MATERIALS */" that starts with "#" to the list
                for (int i = materialsIndex + 1; i < stpLines.Length; i++)
                {
                    string line = stpLines[i].Trim();
                    if (line.StartsWith("#"))
                    {
                        materialLines.Add(line);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            writer.WriteStartElement("model");
            writer.WriteStartElement("basic_objects");
            writer.WriteStartElement("material");

            // Check if nextLine contains the material definition and process accordingly
            foreach (string material in materialLines)
            {
                string[] lineParts = material.Split(new[] { ",", "'", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                string material_no = lineParts[1];
                string material_name = lineParts[2];

                // Write XML tags
                writer.WriteStartElement("item");
                writer.WriteElementString("no", material_no);
                writer.WriteElementString("material_type", "TYPE_STEEL");
                writer.WriteElementString("material_model", "MODEL_ISOTROPIC_LINEAR_ELASTIC");
                writer.WriteElementString("application_context", "STEEL_DESIGN");
                writer.WriteElementString("user_defined_name_enabled", "false");
                writer.WriteElementString("name", $"{material_name} | EN 1993-1-1:2005-05");
                writer.WriteElementString("user_defined", "false");
                writer.WriteElementString("definition_type", "DERIVED_G");
                writer.WriteElementString("is_temperature_dependent", "false");
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); //end material

            // Find the line with "/* NODES */"
            int nodesIndex = Array.IndexOf(stpLines, nodes_section);
            if (nodesIndex != -1)
            {
                // Add each line after "/* NODES */" that starts with "#" to the list
                for (int i = nodesIndex + 1; i < stpLines.Length; i++)
                {
                    string line = stpLines[i].Trim();
                    if (line.StartsWith("#"))
                    {
                        nodesLines.Add(line);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Group into list of lists with two lines: VERTEX + NODE
            List<List<string>> subLists = nodesLines.Select((x, i) => new { Value = x, Index = i })
                                .GroupBy(x => x.Index / 2)
                                .Select(g => g.Select(x => x.Value).ToList())
                                .ToList();


            writer.WriteStartElement("node");
            foreach (List<string> sublist in subLists)
            {
                string vertex = sublist[0];
                string node = sublist[1];
                string[] vertexParts = vertex.Split(new[] { ",", "'", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                string[] nodeParts = node.Split(new[] { ",", "'", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);

                double[] coordinates_m = new double[3];

                // extract coordinates at indices 2,3,4
                // COORDINATES NEED TO BE MULTIPLIED BY GLOBAL COORDINATES IN THE /* STRUCTURAL DATA */ ??
                for (int i = 2; i < 5; i++)
                {
                    string coordinate_str = vertexParts[i];
                    double coordinate_mm = double.Parse(coordinate_str, CultureInfo.InvariantCulture);
                    if (coordinate_mm == 0 && coordinate_str.StartsWith("-"))
                    {
                        coordinate_mm = 0;
                    }
                    double coordinate_m = coordinate_mm / 1000.0;

                    // create a list with coordinates converted to [m]
                    coordinates_m[i - 2] = coordinate_m;
                }

                string node_no = nodeParts[2];

                // Write XML tags
                writer.WriteStartElement("item");
                writer.WriteElementString("no", node_no);
                writer.WriteElementString("type", "TYPE_STANDARD");
                writer.WriteElementString("coordinate_system", "1");
                writer.WriteElementString("coordinate_system_type", "COORDINATE_SYSTEM_CARTESIAN");
                writer.WriteStartElement("coordinates");
                writer.WriteElementString("x", coordinates_m[0].ToString());
                writer.WriteElementString("y", coordinates_m[1].ToString());
                writer.WriteElementString("z", coordinates_m[2].ToString());
                writer.WriteEndElement();
                writer.WriteElementString("coordinate_1", coordinates_m[0].ToString());
                writer.WriteElementString("coordinate_2", coordinates_m[1].ToString());
                writer.WriteElementString("coordinate_3", coordinates_m[2].ToString());
                writer.WriteStartElement("global_coordinates");
                writer.WriteElementString("x", coordinates_m[0].ToString());
                writer.WriteElementString("y", coordinates_m[1].ToString());
                writer.WriteElementString("z", coordinates_m[2].ToString());
                writer.WriteEndElement();
                writer.WriteElementString("global_coordinate_1", coordinates_m[0].ToString());
                writer.WriteElementString("global_coordinate_2", coordinates_m[1].ToString());
                writer.WriteElementString("global_coordinate_3", coordinates_m[2].ToString());
                writer.WriteElementString("is_generated", "false");
                writer.WriteElementString("support", "placeholder");
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); //end node


            // Close XML file
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }
    }
}
