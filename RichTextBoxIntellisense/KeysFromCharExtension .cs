using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Markup;
//using System.Globalization;
namespace Behaviorlibrary
{

    /// <summary>
    /// Extension for xaml. Specially written for RichTextBoxIntellisense.
    /// </summary>
    public sealed class KeysFromCharExtension : MarkupExtension
    {
        // 
        //IDictionary<string, Key> dictionaryKey = KeysFromCharExtension.GetDictionaryKeys();
        Func<string, Key> _comparer;
        IEnumerable<Key> _keys;
        private string[] _keysChar;
        public KeysFromCharExtension(String keysChar)
        {
            if (String.IsNullOrEmpty(keysChar)) throw new ArgumentNullException("keysChar");

            // hard code char ' ' and ',' only
            if (keysChar.Any(c => c == ',')) _keysChar = keysChar.Trim(' ').Split(',');
            else _keysChar = keysChar.Trim(' ').Split();

            _keys = new List<Key>();

            _comparer = ConvertKeyFromString;
            _keys = _keysChar.Select(k => ConvertKeyFromString(k.Trim(' '))).ToList();
        }
        // Importent
        //Key ConvertKeyFromString(string stringKey, CultureInfo culture)
        Key ConvertKeyFromString(string stringKey)
        {
            Key m_key;
            KeyConverter cov = new KeyConverter();
            m_key = KeysFromCharExtension.GetDictionaryCodeKeys().FirstOrDefault(k => k.Key == stringKey).Value;
            if (m_key == Key.None)
                try
                {
                    m_key = (Key)cov.ConvertFromInvariantString(stringKey.ToUpper());
                }
                catch
                {
                    // if not know set to Key.None
                    m_key = Key.None;
                }
            return m_key;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _keys;
        }
        public static IDictionary<string, Key> GetDictionaryCodeKeys()
        {
            IDictionary<string, Key> m_dictionaryKeys = new Dictionary<string, Key>();
            m_dictionaryKeys.Add(";", Key.OemSemicolon);
            m_dictionaryKeys.Add("[", Key.Oem4);
            m_dictionaryKeys.Add("]", Key.Oem6);
            // add all these what are not converted or all 
            // how to add a converter for converting all is no longer needed
            return m_dictionaryKeys;
        }
        public static IDictionary<Key, string> GetDictionaryStringKeys()
        {
            IDictionary<Key, string> m_dictionaryKeys = new Dictionary<Key, string>();
            m_dictionaryKeys.Add(Key.OemSemicolon, ";");
            m_dictionaryKeys.Add(Key.Oem4, "[");
            m_dictionaryKeys.Add(Key.Oem6, "]");
            // add all these what are not converted or all 
            // how to add a converter for converting all is no longer needed
            return m_dictionaryKeys;
        }
    }
}


