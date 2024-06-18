using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using AmaknaProxy.ProtocolBuilder.Parsing;
using AmaknaProxy.ProtocolBuilder.Parsing.Elements;
using AmaknaProxy.ProtocolBuilder.Profiles;

namespace AmaknaProxy.ProtocolBuilder.XmlPatterns
{
    public class XmlMessageBuilder : XmlPatternBuilder
    {
        public XmlMessageBuilder(Parser parser)
            : base(parser)
        {
        }

        public override void WriteToXml(XmlWriter writer)
        {
            var xmlMessage = new XmlMessage
            {
                Name = Parser.Class.Name,
                Id = Parser.Fields.Find(entry => entry.Name == "protocolId").Value,
                Heritage = Parser.Class.Heritage,
            };
            var xmlFields = new List<XmlField>();

            if (xmlMessage.Name == "GameRolePlayShowActorMessage")
                Console.WriteLine();

            var deserializeAsMethods = Parser.Methods.Where(entry => (entry.Name.Contains("deserializeAs") && !entry.Name.Contains("deserializeAsync")) || entry.Name.Contains("Func")).ToList();
            foreach (var deserializeAsMethod in deserializeAsMethods)
            {
                BuildFields(deserializeAsMethod, xmlFields);
            }
            xmlMessage.Fields = xmlFields.ToArray();
            var serializer = new XmlSerializer(typeof(XmlMessage));
            serializer.Serialize(writer, xmlMessage);
        }

        public void BuildFields(MethodInfo deserializeAsMethod, List<XmlField> xmlFields)
        {
            string type = null;
            int limit = 0;
            var _varCache = new List<string>();

            for (int i = 0; i < deserializeAsMethod.Statements.Count; i++)
            {
                if (deserializeAsMethod.Statements[i] is AssignationStatement &&
                    ((AssignationStatement)deserializeAsMethod.Statements[i]).Value.Contains("Read"))
                {
                    var statement = ((AssignationStatement)deserializeAsMethod.Statements[i]);
                    type = Regex.Match(statement.Value, @"Read([\w\d_]+)\(").Groups[1].Value.ToLower();
                    var name = statement.Name;

                    if (type == "bytes")
                        type = "byte[]";

                    Match arrayMatch = Regex.Match(name, @"^([\w\d]+)\[.+\]$");
                    if (arrayMatch.Success)
                    {
                        IEnumerable<string> limitLinq = from entry in Parser.Constructors[0].Statements
                                                        where
                                                            entry is AssignationStatement &&
                                                            ((AssignationStatement)entry).Name == arrayMatch.Groups[1].Value
                                                        let entryMatch =
                                                            Regex.Match(((AssignationStatement)entry).Value,
                                                                        @"new List<[\d\w\._]+>\(([\d]+)\)")
                                                        where entryMatch.Success
                                                        select entryMatch.Groups[1].Value;

                        if (limitLinq.Count() == 1)
                            limit = int.Parse(limitLinq.Single());

                        type += "[]";
                        name = name.Split('[')[0];

                    }
                    FieldInfo field = Parser.Fields.Find(entry => entry.Name == name);

                    if (field != null)
                    {
                        string condition = null;

                        if (i + 1 < deserializeAsMethod.Statements.Count &&
                            deserializeAsMethod.Statements[i + 1] is ControlStatement &&
                            ((ControlStatement)deserializeAsMethod.Statements[i + 1]).ControlType == ControlType.If)
                            condition = ((ControlStatement)deserializeAsMethod.Statements[i + 1]).Condition;
                        if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                            xmlFields.Add(new XmlField
                        {
                            Name = field.Name,
                            Type = type,
                            Limit = limit > 0 ? limit.ToString() : null,
                            Condition = condition,
                        });

                        limit = 0;
                        type = null;
                    }
                }

                if (deserializeAsMethod.Statements[i] is InvokeExpression &&
                    ((InvokeExpression)deserializeAsMethod.Statements[i]).Name == "deserialize")
                {
                    var statement = ((InvokeExpression)deserializeAsMethod.Statements[i]);
                    FieldInfo field = Parser.Fields.Find(entry => entry.Name == statement.Target);

                    if (field != null && xmlFields.Count(entry => entry.Name == field.Name) <= 0)
                    {
                        type = "Types." + field.Type;

                        string condition = null;

                        if (i + 1 < deserializeAsMethod.Statements.Count &&
                            deserializeAsMethod.Statements[i + 1] is ControlStatement &&
                            ((ControlStatement)deserializeAsMethod.Statements[i + 1]).ControlType == ControlType.If)
                            condition = ((ControlStatement)deserializeAsMethod.Statements[i + 1]).Condition;
                        if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                            xmlFields.Add(new XmlField
                        {
                            Name = field.Name,
                            Type = type,
                            Limit = limit > 0 ? limit.ToString() : null,
                            Condition = condition,
                        });

                        limit = 0;
                        type = null;
                    }
                    else if (i > 0 &&
                             deserializeAsMethod.Statements[i - 1] is AssignationStatement)
                    {
                        var substatement = ((AssignationStatement)deserializeAsMethod.Statements[i - 1]);
                        var name = substatement.Name;
                        Match match = Regex.Match(substatement.Value, @"new ([\d\w]+)");

                        if (match.Success)
                        {
                            type = "Types." + match.Groups[1].Value;

                            Match arrayMatch = Regex.Match(name, @"^([\w\d]+)\[.+\]$");
                            if (arrayMatch.Success)
                            {
                                IEnumerable<string> limitLinq = from entry in Parser.Constructors[0].Statements
                                                                where
                                                                    entry is AssignationStatement &&
                                                                    ((AssignationStatement)entry).Name == arrayMatch.Groups[1].Value
                                                                let entryMatch =
                                                                    Regex.Match(((AssignationStatement)entry).Value,
                                                                                @"new List<[\d\w\._]+>\(([\d]+)\)")
                                                                where entryMatch.Success
                                                                select entryMatch.Groups[1].Value;

                                if (limitLinq.Count() == 1)
                                    limit = int.Parse(limitLinq.Single());

                                type += "[]";
                                name = name.Split('[')[0];

                            }
                        }

                        field = Parser.Fields.Find(entry => entry.Name == name);

                        if (field != null && xmlFields.Count(entry => entry.Name == field.Name) <= 0)
                        {
                            string condition = null;

                            if (i + 1 < deserializeAsMethod.Statements.Count &&
                                deserializeAsMethod.Statements[i + 1] is ControlStatement &&
                                ((ControlStatement)deserializeAsMethod.Statements[i + 1]).ControlType == ControlType.If)
                                condition = ((ControlStatement)deserializeAsMethod.Statements[i + 1]).Condition;
                            if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                                xmlFields.Add(new XmlField
                            {
                                Name = field.Name,
                                Type = type,
                                Limit = limit > 0 ? limit.ToString() : null,
                                Condition = condition,
                            });

                            limit = 0;
                            type = null;
                        }
                    }
                }

                if (deserializeAsMethod.Statements[i] is AssignationStatement &&
                    ((AssignationStatement)deserializeAsMethod.Statements[i]).Value.Contains("getFlag"))
                {
                    var statement = ((AssignationStatement)deserializeAsMethod.Statements[i]);
                    FieldInfo field = Parser.Fields.Find(entry => entry.Name == statement.Name);

                    var match = Regex.Match(statement.Value, @"getFlag\([\w\d]+,(\d+)\)");

                    if (match.Success)
                    {
                        type = "flag(" + match.Groups[1].Value + ")";

                        if (field != null)
                        {
                            if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                                xmlFields.Add(new XmlField
                            {
                                Name = field.Name,
                                Type = type,
                            });

                            type = null;
                        }
                    }
                }

                if (deserializeAsMethod.Statements[i] is AssignationStatement &&
                    ((AssignationStatement)deserializeAsMethod.Statements[i]).Value.Contains("getInstance"))
                {
                    var statement = ((AssignationStatement)deserializeAsMethod.Statements[i]);
                    FieldInfo field = Parser.Fields.Find(entry => entry.Name == statement.Name);

                    type = "instance of Types." + Regex.Match(statement.Value, @"getInstance\(([\w\d_\.]+),").Groups[1].Value;

                    if (field != null)
                    {
                        if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                            xmlFields.Add(new XmlField
                        {
                            Name = field.Name,
                            Type = type,
                        });

                        type = null;
                    }
                }

                if (deserializeAsMethod.Statements[i] is InvokeExpression)
                {
                    var invoke = deserializeAsMethod.Statements[i] as InvokeExpression;
                    var function = Parser.Methods.FirstOrDefault(x => (x.Name.Contains("Func") || x.Name == "deserializeByteBoxes") && x.Name == invoke.Name);
                    if (function != null)
                    {
                        BuildFields(function, xmlFields);
                    }
                }

                if (deserializeAsMethod.Statements[i] is InvokeExpression &&
                        ((InvokeExpression)deserializeAsMethod.Statements[i]).Name == "Add" &&
                        type != null)
                {
                    var statement = ((InvokeExpression)deserializeAsMethod.Statements[i]);

                    FieldInfo field = Parser.Fields.Find(entry => entry.Name == statement.Target);

                    string condition = null;

                    if (i + 1 < deserializeAsMethod.Statements.Count &&
                        deserializeAsMethod.Statements[i + 1] is ControlStatement &&
                        ((ControlStatement)deserializeAsMethod.Statements[i + 1]).ControlType == ControlType.If)
                        condition = ((ControlStatement)deserializeAsMethod.Statements[i + 1]).Condition;

                    //if (deserializeAsMethod.Statements.Count >= 4 && i >= 4)
                    //{
                    //    var statement2 = (deserializeAsMethod.Statements[i - 4]);
                    //    var assignationStatement = statement2 as AssignationStatement;
                    //    if (assignationStatement != null)
                    //    {
                    //        type = Regex.Match(assignationStatement.Value, @"Read([\w\d_]+)\(").Groups[1].Value.ToLower();
                    //    }
                    //}

                    if (xmlFields.FirstOrDefault(x => x.Name == field.Name) == null)
                    {
                        if (type == "ushort" && field.Type.Contains("List<"))
                        {
                            type = "Types." + field.Type.Replace("List<", string.Empty).Replace(">", string.Empty);
                        }
                        xmlFields.Add(new XmlField
                        {
                            Name = field.Name,
                            Type = (_varCache.Contains(field.Name) && !type.Contains("var") ? "var" + type : type) + "[]",
                            Limit = limit > 0 ? limit.ToString() : null,
                            Condition = condition,
                        });
                    }

                    limit = 0;
                    type = null;
                }
            }

        }
    }
}