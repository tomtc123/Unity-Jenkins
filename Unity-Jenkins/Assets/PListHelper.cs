using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections;

public class PListHelper
{
    public XmlDocument Doc;

    bool bReadOnly = false;

    public void SetReadOnly(bool bNowReadOnly)
    {
        bReadOnly = bNowReadOnly;
    }

    public PListHelper(string Source)
    {
        Doc = new XmlDocument();
        Doc.XmlResolver = null;
        Doc.LoadXml(Source);
    }

    public static PListHelper CreateFromFile(string Filename)
    {
        byte[] RawPList = File.ReadAllBytes(Filename);
        return new PListHelper(Encoding.UTF8.GetString(RawPList));
    }

    public void SaveToFile(string Filename)
    {
        File.WriteAllText(Filename, SaveToString(), Encoding.UTF8);
    }

    public PListHelper()
    {
        string EmptyFileText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
            "<plist version=\"1.0\">\n" +
            "<dict>\n" +
            "</dict>\n" +
            "</plist>\n";

        Doc = new XmlDocument();
        Doc.XmlResolver = null;
        Doc.LoadXml(EmptyFileText);
    }

    public XmlElement ConvertValueToPListFormat(object Value)
    {
        XmlElement ValueElement = null;
        if (Value is string)
        {
            ValueElement = Doc.CreateElement("string");
            ValueElement.InnerText = Value as string;
        }
        else if (Value is Dictionary<string, object>)
        {
            ValueElement = Doc.CreateElement("dict");
            foreach (var KVP in Value as Dictionary<string, object>)
            {
                AddKeyValuePair(ValueElement, KVP.Key, KVP.Value);
            }
        }
        else if (Value is PListHelper)
        {
            PListHelper PList = Value as PListHelper;

            ValueElement = Doc.CreateElement("dict");

            XmlNode SourceDictionaryNode = PList.Doc.DocumentElement.SelectSingleNode("/plist/dict");
            foreach (XmlNode TheirChild in SourceDictionaryNode)
            {
                ValueElement.AppendChild(Doc.ImportNode(TheirChild, true));
            }
        }
        else if (Value is Array)
        {
            if (Value is byte[])
            {
                ValueElement = Doc.CreateElement("data");
                ValueElement.InnerText = Convert.ToBase64String(Value as byte[]);
            }
            else
            {
                ValueElement = Doc.CreateElement("array");
                foreach (var A in Value as Array)
                {
                    ValueElement.AppendChild(ConvertValueToPListFormat(A));
                }
            }
        }
        else if (Value is IList)
        {
            ValueElement = Doc.CreateElement("array");
            foreach (var A in Value as IList)
            {
                ValueElement.AppendChild(ConvertValueToPListFormat(A));
            }
        }
        else if (Value is bool)
        {
            ValueElement = Doc.CreateElement(((bool)Value) ? "true" : "false");
        }
        else if (Value is double)
        {
            ValueElement = Doc.CreateElement("real");
            ValueElement.InnerText = ((double)Value).ToString();
        }
        else if (Value is int)
        {
            ValueElement = Doc.CreateElement("integer");
            ValueElement.InnerText = ((int)Value).ToString();
        }
        else
        {
            throw new InvalidDataException(String.Format("Object '{0}' is in an unknown type that cannot be converted to PList format", Value));
        }

        return ValueElement;

    }

    public void AddKeyValuePair(XmlNode DictRoot, string KeyName, object Value)
    {
        if (bReadOnly)
        {
            throw new AccessViolationException("PList has been set to read only and may not be modified");
        }

        XmlElement KeyElement = Doc.CreateElement("key");
        KeyElement.InnerText = KeyName;

        DictRoot.AppendChild(KeyElement);
        DictRoot.AppendChild(ConvertValueToPListFormat(Value));
    }

    public void AddKeyValuePair(string KeyName, object Value)
    {
        XmlNode DictRoot = Doc.DocumentElement.SelectSingleNode("/plist/dict");

        AddKeyValuePair(DictRoot, KeyName, Value);
    }

    /// <summary>
    /// Clones a dictionary from an existing .plist into a new one.  Root should point to the dict key in the source plist.
    /// </summary>
    public static PListHelper CloneDictionaryRootedAt(XmlNode Root)
    {
        // Create a new empty dictionary
        PListHelper Result = new PListHelper();

        // Copy all of the entries in the source dictionary into the new one
        XmlNode NewDictRoot = Result.Doc.DocumentElement.SelectSingleNode("/plist/dict");
        foreach (XmlNode TheirChild in Root)
        {
            NewDictRoot.AppendChild(Result.Doc.ImportNode(TheirChild, true));
        }

        return Result;
    }

    public bool GetString(string Key, out string Value)
    {
        string PathToValue = String.Format("/plist/dict/key[.='{0}']/following-sibling::string[1]", Key);

        XmlNode ValueNode = Doc.DocumentElement.SelectSingleNode(PathToValue);
        if (ValueNode == null)
        {
            Value = "";
            return false;
        }

        Value = ValueNode.InnerText;
        return true;
    }

    public delegate void ProcessOneNodeEvent(XmlNode ValueNode);

    public void ProcessValueForKey(string Key, string ExpectedValueType, ProcessOneNodeEvent ValueHandler)
    {
        string PathToValue = String.Format("/plist/dict/key[.='{0}']/following-sibling::{1}[1]", Key, ExpectedValueType);

        XmlNode ValueNode = Doc.DocumentElement.SelectSingleNode(PathToValue);
        if (ValueNode != null)
        {
            ValueHandler(ValueNode);
        }
    }

    /// <summary>
    /// Merge two plists together.  Whenever both have the same key, the value in the dominant source list wins.
    /// This is special purpose code, and only handles things inside of the <dict> tag
    /// </summary>
    public void MergePlistIn(string DominantPlist)
    {
        if (bReadOnly)
        {
            throw new AccessViolationException("PList has been set to read only and may not be modified");
        }

        XmlDocument Dominant = new XmlDocument();
        Dominant.XmlResolver = null;
        Dominant.LoadXml(DominantPlist);

        XmlNode DictionaryNode = Doc.DocumentElement.SelectSingleNode("/plist/dict");

        // Merge any key-value pairs in the strong .plist into the weak .plist
        XmlNodeList StrongKeys = Dominant.DocumentElement.SelectNodes("/plist/dict/key");
        foreach (XmlNode StrongKeyNode in StrongKeys)
        {
            string StrongKey = StrongKeyNode.InnerText;

            XmlNode WeakNode = Doc.DocumentElement.SelectSingleNode(String.Format("/plist/dict/key[.='{0}']", StrongKey));
            if (WeakNode == null)
            {
                // Doesn't exist in dominant plist, inject key-value pair
                DictionaryNode.AppendChild(Doc.ImportNode(StrongKeyNode, true));
                DictionaryNode.AppendChild(Doc.ImportNode(StrongKeyNode.NextSibling, true));
            }
            else
            {
                // Remove the existing value node from the weak file
                WeakNode.ParentNode.RemoveChild(WeakNode.NextSibling);

                // Insert a clone of the dominant value node
                WeakNode.ParentNode.InsertAfter(Doc.ImportNode(StrongKeyNode.NextSibling, true), WeakNode);
            }
        }
    }

    /// <summary>
    /// Returns each of the entries in the value tag of type array for a given key
    /// If the key is missing, an empty array is returned.
    /// Only entries of a given type within the array are returned.
    /// </summary>
    public List<string> GetArray(string Key, string EntryType)
    {
        List<string> Result = new List<string>();

        ProcessValueForKey(Key, "array",
            delegate (XmlNode ValueNode)
            {
                foreach (XmlNode ChildNode in ValueNode.ChildNodes)
                {
                    if (EntryType == ChildNode.Name)
                    {
                        string Value = ChildNode.InnerText;
                        Result.Add(Value);
                    }
                }
            });

        return Result;
    }

    /// <summary>
    /// Returns true if the key exists (and has a value) and false otherwise
    /// </summary>
    public bool HasKey(string KeyName)
    {
        //XmlNode DictionaryNode = Doc.DocumentElement.SelectSingleNode("/plist/dict");

        string PathToKey = String.Format("/plist/dict/key[.='{0}']", KeyName);

        XmlNode KeyNode = Doc.DocumentElement.SelectSingleNode(PathToKey);
        return (KeyNode != null);
    }

    public void SetValueForKey(string KeyName, object Value)
    {
        if (bReadOnly)
        {
            throw new AccessViolationException("PList has been set to read only and may not be modified");
        }

        XmlNode DictionaryNode = Doc.DocumentElement.SelectSingleNode("/plist/dict");

        string PathToKey = String.Format("/plist/dict/key[.='{0}']", KeyName);
        XmlNode KeyNode = Doc.DocumentElement.SelectSingleNode(PathToKey);

        XmlNode ValueNode = null;
        if (KeyNode != null)
        {
            ValueNode = KeyNode.NextSibling;
        }

        if (ValueNode == null)
        {
            KeyNode = Doc.CreateNode(XmlNodeType.Element, "key", null);
            KeyNode.InnerText = KeyName;

            ValueNode = ConvertValueToPListFormat(Value);

            DictionaryNode.AppendChild(KeyNode);
            DictionaryNode.AppendChild(ValueNode);
        }
        else
        {
            // Remove the existing value and create a new one
            ValueNode.ParentNode.RemoveChild(ValueNode);
            ValueNode = ConvertValueToPListFormat(Value);

            // Insert the value after the key
            DictionaryNode.InsertAfter(ValueNode, KeyNode);
        }
    }

    public void SetString(string Key, string Value)
    {
        SetValueForKey(Key, Value);
    }

    public string SaveToString()
    {
        // Convert the XML back to text in the same style as the original .plist
        StringBuilder TextOut = new StringBuilder();

        // Work around the fact it outputs the wrong encoding by default (and set some other settings to get something similar to the input file)
        TextOut.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        XmlWriterSettings Settings = new XmlWriterSettings();
        Settings.Indent = true;
        Settings.IndentChars = "\t";
        Settings.NewLineChars = "\n";
        Settings.NewLineHandling = NewLineHandling.Replace;
        Settings.OmitXmlDeclaration = true;
        Settings.Encoding = new UTF8Encoding(false);

        // Work around the fact that it embeds an empty declaration list to the document type which codesign dislikes...
        // Replacing InternalSubset with null if it's empty.  The property is readonly, so we have to reconstruct it entirely
        Doc.ReplaceChild(Doc.CreateDocumentType(
            Doc.DocumentType.Name,
            Doc.DocumentType.PublicId,
            Doc.DocumentType.SystemId,
            String.IsNullOrEmpty(Doc.DocumentType.InternalSubset) ? null : Doc.DocumentType.InternalSubset),
            Doc.DocumentType);

        XmlWriter Writer = XmlWriter.Create(TextOut, Settings);

        Doc.Save(Writer);

        // Remove the space from any standalone XML elements because the iOS parser does not handle them
        return Regex.Replace(TextOut.ToString(), @"<(?<tag>\S+) />", "<${tag}/>");
    }
}