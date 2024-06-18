using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using AmaknaProxy.ProtocolBuilder.Parsing;
using AmaknaProxy.ProtocolBuilder.Templates;
using AmaknaProxy.ProtocolBuilder.XmlPatterns;
using Microsoft.VisualStudio.TextTemplating;

namespace AmaknaProxy.ProtocolBuilder.Profiles
{
    public class MessagesProfile : ParsingProfile
    {
        public override void ExecuteProfile(Parser parser)
        {
            string file = Path.Combine(Program.Configuration.Output, OutPutPath, GetRelativePath(parser.Filename), Path.GetFileNameWithoutExtension(parser.Filename));
            var xmlMessage = Program.Configuration.XmlMessagesProfile.SearchXmlPattern(Path.GetFileNameWithoutExtension(parser.Filename));

            if (xmlMessage == null)
                Program.Shutdown(string.Format("File {0} not found", file));

            var engine = new Engine();
            var host = new TemplateHost(TemplatePath);
            host.Session["Message"] = xmlMessage;
            host.Session["Profile"] = this;
            var output = engine.ProcessTemplate(File.ReadAllText(TemplatePath), host);

            foreach (CompilerError error in host.Errors)
            {
                Console.WriteLine(error.ErrorText);
            }

            if (host.Errors.Count > 0)
                Program.Shutdown();

            File.WriteAllText(file + host.FileExtension, FixType(output));

            Console.WriteLine("Wrote {0}", file);
        }

        private string FixType(string type)
        {

            string _type = type;

            _type = _type.Replace("ReadVarint", "ReadVarInt");
            _type = _type.Replace("ReadVaruhint", "ReadVarUhInt");
            _type = _type.Replace("ReadVarshort", "ReadVarShort");
            _type = _type.Replace("ReadVaruhshort", "ReadVarUhShort");
            _type = _type.Replace("ReadVarlong", "ReadVarLong");
            _type = _type.Replace("ReadVaruhlong", "ReadVarUhLong");

            _type = _type.Replace("WriteVarint", "WriteVarInt");
            _type = _type.Replace("WriteVaruhint", "WriteVarInt");
            _type = _type.Replace("WriteVarshort", "WriteVarShort");
            _type = _type.Replace("WriteVaruhshort", "WriteVarShort");
            _type = _type.Replace("WriteVarlong", "WriteVarLong");
            _type = _type.Replace("WriteVaruhlong", "WriteVarLong");

            _type = _type.Replace("WriteVarInt(", "WriteVarInt((int)");
            _type = _type.Replace("WriteVarShort(", "WriteVarShort((int)");

            _type = _type.Replace("varint", "int");
            _type = _type.Replace("varuhint", "uint");
            _type = _type.Replace("varshort", "int");
            _type = _type.Replace("varuhshort", "uint");
            _type = _type.Replace("varlong", "double");
            _type = _type.Replace("varuhlong", "double");

            return _type;
        }
    }
}