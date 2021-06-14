using ConTeXt_IDE.Helpers;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConTeXt_IDE.Models
{
    [XmlRoot(ElementName = "resolve")]
	public class Resolve : Argument
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName = "inherit")]
	public class Inherit
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName = "assignments")]
	public class Assignments : Argument
	{
		[XmlElement(ElementName = "inherit")]
		public Inherit Inherit { get; set; }

		[XmlElement(ElementName = "parameter")]
		public List<Parameter> Parameter { get; set; }
	}

	//[XmlInclude(typeof(Resolve))]
	//[XmlInclude(typeof(Assignments))]
	//[XmlInclude(typeof(Keywords))]
	//[XmlRoot(ElementName = "arguments")]
	public class Argument : Bindable
	{
		[XmlIgnore]
		public int Number { get; set; }

		[XmlIgnore]
		public bool IsSelected { get => Get(false); set => Set(value); }

		[XmlAttribute(AttributeName = "optional")]
		public string Optional { get; set; }

		[XmlAttribute(AttributeName = "delimiters")]
		public string Delimiters { get; set; }

		[XmlAttribute(AttributeName = "list")]
		public string List { get; set; }
	}

	[XmlRoot(ElementName = "arguments")]
	public class Arguments
	{
		[XmlElement(typeof(Resolve), ElementName = "resolve")]
		[XmlElement(typeof(Assignments), ElementName = "assignments")]
		[XmlElement(typeof(Keywords), ElementName = "keywords")]
		public List<Argument> ArgumentsList { get; set; }
	}

	[XmlRoot(ElementName = "command")]
	public class Command : Bindable
	{
		[XmlElement(ElementName = "arguments")]
		public Arguments Arguments { get; set; }

		[XmlAttribute(AttributeName = "category")]
		public string Category { get; set; }

		[XmlAttribute(AttributeName = "file")]
		public string File { get; set; }

		[XmlAttribute(AttributeName = "keywords")]
		public string Keywords { get; set; }

		[XmlAttribute(AttributeName = "level")]
		public string Level { get; set; }

		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		[XmlElement(ElementName = "sequence")]
		public Sequence Sequence { get; set; }

		[XmlAttribute(AttributeName = "generated")]
		public string Generated { get; set; }

		[XmlAttribute(AttributeName = "variant")]
		public string Variant { get; set; }

		[XmlElement(ElementName = "instances")]
		public Instances Instances { get; set; }

		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }

		[XmlIgnore]
		public bool IsSelected { get => Get(false); set => Set(value); }

		[XmlIgnore]
		public int SelectedIndex { get => Get(-1); set => Set(value); }
	}

	[XmlRoot(ElementName = "variable")]
	public class Variable
	{
		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
	}

	[XmlRoot(ElementName = "string")]
	public class XMLString
	{
		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
	}

	[XmlRoot(ElementName = "sequence")]
	public class Sequence
	{
		[XmlElement(ElementName = "string")]
		public List<XMLString> String { get; set; }

		[XmlElement(ElementName = "instance")]
		public Instance Instance { get; set; }

		[XmlElement(ElementName = "variable")]
		public Variable Variable { get; set; }
	}

	[XmlRoot(ElementName = "constant")]
	public class Constant
	{
		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }

		[XmlAttribute(AttributeName = "default")]
		public string Default { get; set; }

		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
	}

	[XmlRoot(ElementName = "parameter")]
	public class Parameter
	{
		[XmlElement(ElementName = "constant")]
		public List<Constant> Constant { get; set; }

		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName = "keywords")]
	public class Keywords : Argument
	{
		[XmlElement(ElementName = "constant")]
		public List<Constant> Constant { get; set; }

		[XmlElement(ElementName = "inherit")]
		public Inherit Inherit { get; set; }

		[XmlElement(ElementName = "resolve")]
		public Resolve Resolve { get; set; }
	}

	[XmlRoot(ElementName = "define")]
	public class Define
	{
		[XmlElement(ElementName = "keywords")]
		public Keywords Keywords { get; set; }

		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName = "interfacefile")]
	public class Interfacefile
	{
		[XmlAttribute(AttributeName = "filename")]
		public string Filename { get; set; }
	}

	[XmlRoot(ElementName = "instance")]
	public class Instance
	{
		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
	}

	[XmlRoot(ElementName = "instances")]
	public class Instances
	{
		[XmlElement(ElementName = "constant")]
		public List<Constant> Constant { get; set; }
	}


	[XmlRoot(ElementName = "interface", Namespace = "http://www.pragma-ade.com/commands")]
	public class Interface
	{
		[XmlElement(ElementName = "command")]
		public List<Command> Command { get; set; }

		[XmlElement(ElementName = "define")]
		public List<Define> Define { get; set; }

		[XmlElement(ElementName = "interfacefile")]
		public List<Interfacefile> Interfacefile { get; set; }

		[XmlElement(ElementName = "interface")]
		public List<Interface> InterfaceList { get; set; }

		[XmlAttribute(AttributeName = "cd")]
		public string Cd { get; set; }
	}

	public class CommandGroup : Bindable
	{
		public string Name { get => Get(""); set => Set(value); }

		public bool IsSelected { get => Get(true); set { Set(value); App.VM?.Default?.SaveSettings(); } }

	}
}
