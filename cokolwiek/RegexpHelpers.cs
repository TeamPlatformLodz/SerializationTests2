using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cokolwiek
{
    public static class RH
    {
        public static readonly char[] name_value_del = new char[] { '=' };
        public static readonly char[] metadata_del = new char[] { ' ' };
        public static readonly string[] col_elemt_del = new string[] { "[[", "," };
        public static readonly string metadataPattern = @"(?<=\{)[^}]*(?=\})";

        public static Metadata ReadMetadata(string line)
        {
            var metadata = new Metadata();
            var meta = LookForMetadata(line);
            metadata.Type = LookForType(meta);
            metadata.Id = LookForId(meta);
            metadata.IsRefId = CheckIsRefId(meta);
            metadata.Name = LookForName(meta);
            return metadata;
        }
        public static string[] LookForMetadata(string line)
        {
            var result = Regex.Match(line, metadataPattern).Value.Split(metadata_del);
            return result;
        }
        public static string LookForType(string[] classMetadata)
        {
            string result = "";
            string typePattern = @"Type=.*";
            Match current;
            for (int i = 0; i < classMetadata.Length; i++)
            {
                current = Regex.Match(classMetadata[i], typePattern);
                if (current.Success)
                {
                    result = current.Value.Split(name_value_del)[1];
                    break;
                }
            }
            return result;
        }
        public static string LookForName(string[] classMetadata)
        {
            string result = "";
            string typePattern = @"Name=.*";
            Match current;
            for (int i = 0; i < classMetadata.Length; i++)
            {
                current = Regex.Match(classMetadata[i], typePattern);
                if (current.Success)
                {
                    result = current.Value.Split(name_value_del)[1];
                    break;
                }
            }
            return result;
        }
        public static string LookForId(string[] classMetadata)
        {
            string result = "";
            string typePattern = @"ID=.*";
            Match current;
            for (int i = 0; i < classMetadata.Length; i++)
            {
                current = Regex.Match(classMetadata[i], typePattern);
                if (current.Success)
                {
                    result = current.Value.Split(name_value_del)[1];
                    break;
                }
            }
            return result;
        }
        public static string LookForValue(string line)
        {
            var values = line.Split(name_value_del);
            var result = values[values.Length - 1];
            return result;
        }
        public static string LookForListElementType(string line)
        {
            var pieces = line.Split(col_elemt_del, StringSplitOptions.None);
            var name = pieces[1];
            return name;
        }
        public static bool CheckIsRefId(string[] classMetadata)
        {
            bool isFound = false;
            string typePattern = @"REF_ID=.*";
            Match current;
            for (int i = 0; i < classMetadata.Length; i++)
            {
                current = Regex.Match(classMetadata[i], typePattern);
                if (current.Success)
                {
                    isFound = true;
                    break;
                }
            }
            return isFound;
        }
        public static bool ContainsCollection(string line)
        {
            if (line.Contains("System.Collections.Generic.List"))
                return true;
            else if (line.Contains("System.Collections.ObjectModel.ObservableCollection"))
                return true;
            else if (line.Contains("System.Collections.Generic.Dictionary"))
                return true;
            else
                return false;
        }
    }

}
