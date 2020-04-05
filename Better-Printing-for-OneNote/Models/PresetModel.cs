using Better_Printing_for_OneNote.AdditionalClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Better_Printing_for_OneNote.Models
{
    class PresetModel : NotifyBase
    {
        private string _name { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                if(_name != value)
                {
                    _name = ValidateName(value);
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private List<SignatureAdded> _signatures { get; set; }
        public List<SignatureAdded> Signatures
        {
            get => _signatures;
            set
            {
                if (_signatures != value)
                {
                    _signatures = value;
                    OnPropertyChanged(nameof(Signatures));
                }
            }
        }

        private string ValidateName(string name)
        {
            var charArray = name.ToCharArray();
            var output = "";
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in charArray)
                if (!Array.Exists(invalidChars, _c => _c == c))
                    output += c;
            return output;
        }
    }
}
