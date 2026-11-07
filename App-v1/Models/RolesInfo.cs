using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models
{
    public class RolesInfo : INotifyPropertyChanged
    {
        private string role_name = string.Empty;
        private int role_code;
        private string role_abrv = string.Empty;
        private List<string> permissions = new List<string>();
        private string status = "active";

        [JsonPropertyName("role_name")]
        public string RoleName
        {
            get => role_name;
            set
            {
                role_name = value; OnPropertyChanged();
            }
        }

        [JsonPropertyName("role_code")]
        public int RoleCode
        {
            get => role_code;
            set
            {
                role_code = value; OnPropertyChanged();
            }
        }

        [JsonPropertyName("role_abbrev")]
        public string RoleAbbreviation
        {
            get => role_abrv;
            set
            {
                role_abrv = value; OnPropertyChanged();
            }
        }

        [JsonPropertyName("permissions")]
        public List<string> Permissions
        {
            get => permissions;
            set
            {
                permissions = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionsString));
            }
        }

        [JsonIgnore]
        public string PermissionsString => string.Join(", ", Permissions);

        [JsonPropertyName("status")]
        public string Status
        {
            get => status;
            set
            {
                status = value; OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}